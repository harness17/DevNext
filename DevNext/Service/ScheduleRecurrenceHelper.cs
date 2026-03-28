using Site.Common;
using Site.Entity.Enums;

namespace Site.Service
{
    /// <summary>
    /// 繰り返しスケジュールの発生日展開ロジック。
    /// DB 非依存の純粋関数として実装し、テスト容易性を確保する。
    /// </summary>
    public static class ScheduleRecurrenceHelper
    {
        /// <summary>
        /// 指定条件に基づいて発生日時のリストを返す。
        /// </summary>
        /// <param name="start">イベント開始日時（繰り返しの基準日）</param>
        /// <param name="type">繰り返し種別</param>
        /// <param name="interval">繰り返し間隔</param>
        /// <param name="recEnd">繰り返し終了日（null = 制限なし）</param>
        /// <param name="daysOfWeek">週次時の曜日指定（例: "1,3,5"）。null の場合は start と同じ曜日</param>
        /// <param name="windowStart">取得ウィンドウ開始日（この日以降の発生を返す）</param>
        /// <param name="windowEnd">取得ウィンドウ終了日（この日未満の発生を返す）</param>
        public static List<DateTime> GetOccurrences(
            DateTime start,
            RecurrenceType type,
            int interval,
            DateTime? recEnd,
            string? daysOfWeek,
            DateTime windowStart,
            DateTime windowEnd)
        {
            var results = new List<DateTime>();

            // 繰り返し終了日はウィンドウ終了日とどちらか小さい方で制限。
            // recEnd は「その日を含む」仕様のため、翌日0:00 を上限として扱う。
            var recEndExclusive = recEnd.HasValue ? recEnd.Value.Date.AddDays(1) : (DateTime?)null;
            var effectiveEnd = recEndExclusive.HasValue && recEndExclusive.Value < windowEnd
                ? recEndExclusive.Value
                : windowEnd;

            // interval が 1 以上であることを検証。0 以下は無限ループの原因となる
            if (interval <= 0)
                throw new ArgumentOutOfRangeException(nameof(interval), "interval は 1 以上を指定してください。");

            if (type == RecurrenceType.None)
            {
                // 繰り返しなし: ウィンドウ内に start が含まれれば1件だけ返す
                if (start >= windowStart && start < windowEnd)
                    results.Add(start);
                return results;
            }

            if (type == RecurrenceType.Weekly && !string.IsNullOrEmpty(daysOfWeek))
            {
                // 週次・複数曜日: 各週について指定曜日をすべて展開する
                ExpandWeeklyMultiDay(start, interval, daysOfWeek, windowStart, effectiveEnd, results);
            }
            else
            {
                // 単純繰り返し（Daily / Weekly 同一曜日 / Monthly）
                for (var current = start; current < effectiveEnd; current = Advance(current, type, interval))
                {
                    if (current >= windowStart)
                        results.Add(current);
                }
            }

            return results;
        }

        // ─── 内部ヘルパー ────────────────────────────────────────────────────

        /// <summary>
        /// 週次・複数曜日の展開。
        /// start の週を起点に interval 週ごとに進み、各週で指定曜日すべての発生日を生成する。
        /// </summary>
        private static void ExpandWeeklyMultiDay(
            DateTime start, int interval, string daysOfWeek,
            DateTime windowStart, DateTime effectiveEnd, List<DateTime> results)
        {
            // カンマ区切りの曜日番号（0=日, 1=月, ..., 6=土）をパース
            // Trim で空白を除去し、TryParse で無効なデータを防ぐ
            var days = daysOfWeek.Split(',')
                .Select(s => s.Trim())
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToArray();

            // start の週の日曜日を起点として週単位で進める
            var weekSunday = start.Date.AddDays(-(int)start.DayOfWeek);

            for (var week = weekSunday; week < effectiveEnd; week = week.AddDays(7 * interval))
            {
                foreach (var day in days)
                {
                    // 指定曜日の日付 + start の時刻で発生日時を構築
                    var occurrence = week.AddDays(day).Add(start.TimeOfDay);

                    // start より前・ウィンドウ外・終了日以降は除外
                    if (occurrence >= start && occurrence >= windowStart && occurrence < effectiveEnd)
                        results.Add(occurrence);
                }
            }
        }

        /// <summary>繰り返し種別に応じて日時を進める。</summary>
        private static DateTime Advance(DateTime current, RecurrenceType type, int interval)
            => type switch
            {
                RecurrenceType.Daily => current.AddDays(interval),
                RecurrenceType.Weekly => current.AddDays(7 * interval),
                RecurrenceType.Monthly => current.AddMonths(interval),
                _ => current.AddYears(100) // 未知の種別は安全な停止値で無限ループを防ぐ
            };
    }
}
