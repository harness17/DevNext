using Site.Common;
using Site.Service;
using Xunit;

namespace Tests.Schedule
{
    public class ScheduleRecurrenceHelperTests
    {
        // ─── None（繰り返しなし） ─────────────────────────────────────────────

        [Fact]
        public void GetOccurrences_None_ReturnsSingleOccurrence()
        {
            // 繰り返しなしは開始日時を1件だけ返す
            var start = new DateTime(2026, 4, 1, 9, 0, 0);
            var results = ScheduleRecurrenceHelper.GetOccurrences(
                start, RecurrenceType.None, interval: 1,
                recEnd: null, daysOfWeek: null,
                windowStart: new DateTime(2026, 4, 1),
                windowEnd: new DateTime(2026, 4, 30));

            Assert.Single(results);
            Assert.Equal(start, results[0]);
        }

        [Fact]
        public void GetOccurrences_None_OutsideWindow_ReturnsEmpty()
        {
            // ウィンドウ外のイベントは返さない
            var start = new DateTime(2026, 3, 1, 9, 0, 0);
            var results = ScheduleRecurrenceHelper.GetOccurrences(
                start, RecurrenceType.None, interval: 1,
                recEnd: null, daysOfWeek: null,
                windowStart: new DateTime(2026, 4, 1),
                windowEnd: new DateTime(2026, 4, 30));

            Assert.Empty(results);
        }

        // ─── Daily ───────────────────────────────────────────────────────────

        [Fact]
        public void GetOccurrences_Daily_Interval1_GeneratesCorrectCount()
        {
            // 4/1 から毎日、4/1〜4/3 の3日間 → 3件
            var start = new DateTime(2026, 4, 1, 9, 0, 0);
            var results = ScheduleRecurrenceHelper.GetOccurrences(
                start, RecurrenceType.Daily, interval: 1,
                recEnd: null, daysOfWeek: null,
                windowStart: new DateTime(2026, 4, 1),
                windowEnd: new DateTime(2026, 4, 4)); // 4/4 は含まない

            Assert.Equal(3, results.Count);
            Assert.Equal(new DateTime(2026, 4, 1, 9, 0, 0), results[0]);
            Assert.Equal(new DateTime(2026, 4, 2, 9, 0, 0), results[1]);
            Assert.Equal(new DateTime(2026, 4, 3, 9, 0, 0), results[2]);
        }

        [Fact]
        public void GetOccurrences_Daily_Interval2_SkipsOddDays()
        {
            // 4/1 から2日おき → 4/1, 4/3, 4/5
            var start = new DateTime(2026, 4, 1, 9, 0, 0);
            var results = ScheduleRecurrenceHelper.GetOccurrences(
                start, RecurrenceType.Daily, interval: 2,
                recEnd: null, daysOfWeek: null,
                windowStart: new DateTime(2026, 4, 1),
                windowEnd: new DateTime(2026, 4, 6));

            Assert.Equal(3, results.Count);
            Assert.Equal(new DateTime(2026, 4, 1, 9, 0, 0), results[0]);
            Assert.Equal(new DateTime(2026, 4, 3, 9, 0, 0), results[1]);
            Assert.Equal(new DateTime(2026, 4, 5, 9, 0, 0), results[2]);
        }

        [Fact]
        public void GetOccurrences_Daily_RecEndLimitsOccurrences()
        {
            // 繰り返し終了日が 4/2 → 4/1, 4/2 の2件だけ
            var start = new DateTime(2026, 4, 1, 9, 0, 0);
            var results = ScheduleRecurrenceHelper.GetOccurrences(
                start, RecurrenceType.Daily, interval: 1,
                recEnd: new DateTime(2026, 4, 2), daysOfWeek: null,
                windowStart: new DateTime(2026, 4, 1),
                windowEnd: new DateTime(2026, 4, 30));

            Assert.Equal(2, results.Count);
        }

        // ─── Weekly ──────────────────────────────────────────────────────────

        [Fact]
        public void GetOccurrences_Weekly_Interval1_RepeatsSameWeekday()
        {
            // 4/7（火）から毎週火曜 → 4/7, 4/14, 4/21
            var start = new DateTime(2026, 4, 7, 9, 0, 0); // 火曜
            var results = ScheduleRecurrenceHelper.GetOccurrences(
                start, RecurrenceType.Weekly, interval: 1,
                recEnd: null, daysOfWeek: null,
                windowStart: new DateTime(2026, 4, 1),
                windowEnd: new DateTime(2026, 4, 28));

            Assert.Equal(3, results.Count);
            Assert.All(results, r => Assert.Equal(DayOfWeek.Tuesday, r.DayOfWeek));
        }

        [Fact]
        public void GetOccurrences_Weekly_MultipleDays_GeneratesAllMatchingDays()
        {
            // 4/6 の週から「月・水・金」毎週 → 4/6(月), 4/8(水), 4/10(金), 4/13(月), ...
            var start = new DateTime(2026, 4, 6, 9, 0, 0); // 月曜
            var results = ScheduleRecurrenceHelper.GetOccurrences(
                start, RecurrenceType.Weekly, interval: 1,
                recEnd: null, daysOfWeek: "1,3,5", // 月・水・金
                windowStart: new DateTime(2026, 4, 6),
                windowEnd: new DateTime(2026, 4, 13)); // 1週分のみ

            // 4/6(月), 4/8(水), 4/10(金) の3件
            Assert.Equal(3, results.Count);
            Assert.Contains(results, r => r.Date == new DateTime(2026, 4, 6));
            Assert.Contains(results, r => r.Date == new DateTime(2026, 4, 8));
            Assert.Contains(results, r => r.Date == new DateTime(2026, 4, 10));
        }

        // ─── Monthly ─────────────────────────────────────────────────────────

        [Fact]
        public void GetOccurrences_Monthly_RepeatsSameDayOfMonth()
        {
            // 4/15 から毎月15日 → 4/15, 5/15, 6/15
            var start = new DateTime(2026, 4, 15, 9, 0, 0);
            var results = ScheduleRecurrenceHelper.GetOccurrences(
                start, RecurrenceType.Monthly, interval: 1,
                recEnd: null, daysOfWeek: null,
                windowStart: new DateTime(2026, 4, 1),
                windowEnd: new DateTime(2026, 7, 1));

            Assert.Equal(3, results.Count);
            Assert.All(results, r => Assert.Equal(15, r.Day));
        }
    }
}
