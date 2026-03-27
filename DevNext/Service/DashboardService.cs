using Site.Common;
using Site.Entity;
using Site.Models;

namespace Site.Service
{
    /// <summary>
    /// ダッシュボード集計サービス
    /// 各エンティティのデータを集計し、グラフ表示用 DTO を返す
    /// </summary>
    public class DashboardService
    {
        private readonly DBContext _context;

        public DashboardService(DBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// ダッシュボード全チャートデータを取得する
        /// Ajax ポーリングで 1 リクエストに纏める
        /// </summary>
        public DashboardChartDataDto GetAllChartData()
        {
            return new DashboardChartDataDto
            {
                Summary = GetSummary(),
                SampleEnumDistribution = GetSampleEnumDistribution(),
                MailSuccessRatio = GetMailSuccessRatio(),
                MailDailyTrend = GetMailDailyTrend(),
                FileTypeDistribution = GetFileTypeDistribution(),
                WizardCategoryDistribution = GetWizardCategoryDistribution(),
            };
        }

        /// <summary>
        /// サマリーカード用の件数を取得する
        /// </summary>
        private DashboardSummaryDto GetSummary()
        {
            return new DashboardSummaryDto
            {
                SampleCount = _context.SampleEntity.Count(e => !e.DelFlag),
                // MailLog は論理削除を使わないため全件カウント
                MailLogCount = _context.MailLog.Count(),
                FileCount = _context.FileEntity.Count(e => !e.DelFlag),
                WizardCount = _context.WizardEntity.Count(e => !e.DelFlag),
            };
        }

        /// <summary>
        /// SampleEntity の EnumData 別件数を取得する（棒グラフ用）
        /// 表示順を固定するため EnumData の定義順にマッピングする
        /// </summary>
        private List<ChartItemDto> GetSampleEnumDistribution()
        {
            // DB から集計（論理削除除外）
            var counts = _context.SampleEntity
                .Where(e => !e.DelFlag)
                .GroupBy(e => e.EnumData)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToList();

            // EnumData の表示名マッピング（定義順で固定表示）
            var labelMap = new List<(SampleEnum Key, string Label)>
            {
                (SampleEnum.select1, "選択肢1"),
                (SampleEnum.select2, "選択肢2"),
                (SampleEnum.select3, "選択肢3"),
            };

            return labelMap
                .Select(m => new ChartItemDto
                {
                    Label = m.Label,
                    Count = counts.FirstOrDefault(c => c.Key == m.Key)?.Count ?? 0,
                })
                .ToList();
        }

        /// <summary>
        /// メール送信の成功/失敗件数を取得する（ドーナツグラフ用）
        /// </summary>
        private MailSuccessRatioDto GetMailSuccessRatio()
        {
            var counts = _context.MailLog
                .GroupBy(e => e.IsSuccess)
                .Select(g => new { IsSuccess = g.Key, Count = g.Count() })
                .ToList();

            return new MailSuccessRatioDto
            {
                SuccessCount = counts.FirstOrDefault(c => c.IsSuccess)?.Count ?? 0,
                FailureCount = counts.FirstOrDefault(c => !c.IsSuccess)?.Count ?? 0,
            };
        }

        /// <summary>
        /// 直近30日の日別メール送信件数を取得する（折れ線グラフ用）
        /// 送信がない日は 0 件で補完する
        /// </summary>
        private List<DailyCountDto> GetMailDailyTrend(int days = 30)
        {
            var from = DateTime.Today.AddDays(-days + 1);

            // DateTime.Date の SQL 変換を避けるためクライアント側でグループ化する
            // Select で CreateDate のみ射影してからメモリに展開し、不要なフィールドのロードを避ける
            var counts = _context.MailLog
                .Where(e => e.CreateDate >= from)
                .Select(e => e.CreateDate)
                .AsEnumerable()
                .GroupBy(d => d.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToList();

            // 指定日数分の日付を生成し、データがない日は 0 で補完する
            return Enumerable.Range(0, days)
                .Select(i => from.AddDays(i))
                .Select(date => new DailyCountDto
                {
                    Date = date.ToString("MM/dd"),
                    Count = counts.FirstOrDefault(c => c.Date == date)?.Count ?? 0,
                })
                .ToList();
        }

        /// <summary>
        /// FileEntity の ContentType 別件数を取得する（棒グラフ用）
        /// MIMEタイプを画像・PDF・Office・テキスト・その他に正規化する
        /// </summary>
        private List<ChartItemDto> GetFileTypeDistribution()
        {
            // ContentType のみ取得してクライアント側で正規化する
            var contentTypes = _context.FileEntity
                .Where(e => !e.DelFlag)
                .Select(e => e.ContentType)
                .ToList();

            return contentTypes
                .GroupBy(NormalizeContentType)
                .Select(g => new ChartItemDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        /// <summary>
        /// MIMEタイプを表示名に正規化する
        /// </summary>
        private static string NormalizeContentType(string contentType) => contentType switch
        {
            var ct when ct.StartsWith("image/") => "画像",
            var ct when ct == "application/pdf" => "PDF",
            var ct when ct.StartsWith("application/vnd.ms") => "Office",
            var ct when ct.StartsWith("application/vnd.openxmlformats") => "Office",
            var ct when ct.StartsWith("text/") => "テキスト",
            _ => "その他",
        };

        /// <summary>
        /// WizardEntity のカテゴリ別件数を取得する（横棒グラフ用）
        /// </summary>
        private List<ChartItemDto> GetWizardCategoryDistribution()
        {
            var counts = _context.WizardEntity
                .Where(e => !e.DelFlag)
                .GroupBy(e => e.Category)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToList();

            // カテゴリの表示名マッピング（定義順で固定表示）
            var labelMap = new List<(WizardCategory Key, string Label)>
            {
                (WizardCategory.Inquiry,   "お問い合わせ"),
                (WizardCategory.Request,   "ご要望"),
                (WizardCategory.BugReport, "不具合報告"),
                (WizardCategory.Other,     "その他"),
            };

            return labelMap
                .Select(m => new ChartItemDto
                {
                    Label = m.Label,
                    Count = counts.FirstOrDefault(c => c.Key == m.Key)?.Count ?? 0,
                })
                .ToList();
        }
    }
}
