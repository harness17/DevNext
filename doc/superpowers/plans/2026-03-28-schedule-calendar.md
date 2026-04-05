# スケジュール・カレンダー機能 実装プラン

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** FullCalendar.js を使ったスケジュール・カレンダー機能を追加し、個人予定・共有予定・繰り返し予定・参加者招待に対応する。

**Architecture:** Repository（DBContext を直接 new）→ Service（繰り返し展開ロジック）→ Controller（JSON API + MVC）→ View（FullCalendar.js + Bootstrap モーダル）。繰り返し展開は `ScheduleRecurrenceHelper` 静的クラスに分離してテスト可能にする。

**Tech Stack:** ASP.NET Core 10 MVC, Entity Framework Core, FullCalendar.js 6.x（CDN）, Bootstrap 5, xUnit

---

## ファイル構成

| 操作 | パス |
|------|------|
| 新規作成 | `DevNext/Common/ScheduleEnum.cs` |
| 新規作成 | `DevNext/Entity/ScheduleEventEntity.cs` |
| 新規作成 | `DevNext/Entity/ScheduleEventParticipantEntity.cs` |
| 新規作成 | `DevNext/Repository/ScheduleRepository.cs` |
| 新規作成 | `DevNext/Service/ScheduleRecurrenceHelper.cs` |
| 新規作成 | `DevNext/Service/ScheduleService.cs` |
| 新規作成 | `DevNext/Models/ScheduleViewModels.cs` |
| 新規作成 | `DevNext/Controllers/ScheduleController.cs` |
| 新規作成 | `DevNext/Views/Schedule/Index.cshtml` |
| 変更 | `DevNext/Common/DBContext.cs` |
| 変更 | `DevNext/Program.cs` |
| 変更 | `DevNext/Views/Shared/_Layout.cshtml` |
| 変更 | `DevNext/Views/Home/Index.cshtml` |
| 変更 | `DbMigrationRunner/Program.cs` |
| 新規作成 | `Tests/Schedule/ScheduleRecurrenceHelperTests.cs` |

---

## Task 1: Enum 定義

**Files:**
- Create: `DevNext/Common/ScheduleEnum.cs`

- [ ] **Step 1: ScheduleEnum.cs を作成する**

```csharp
namespace Site.Common
{
    /// <summary>
    /// 繰り返し種別
    /// </summary>
    public enum RecurrenceType
    {
        /// <summary>繰り返しなし</summary>
        None = 0,
        /// <summary>毎日</summary>
        Daily = 1,
        /// <summary>毎週（RecurrenceDaysOfWeek で曜日指定）</summary>
        Weekly = 2,
        /// <summary>毎月（開始日と同じ日）</summary>
        Monthly = 3
    }

    /// <summary>
    /// 参加者ステータス
    /// </summary>
    public enum ParticipantStatus
    {
        /// <summary>招待済み（未回答）</summary>
        Invited = 0,
        /// <summary>承諾</summary>
        Accepted = 1,
        /// <summary>辞退</summary>
        Declined = 2
    }
}
```

- [ ] **Step 2: ビルドして確認**

```bash
cd DevNext && dotnet build
```
Expected: Build succeeded, 0 errors

- [ ] **Step 3: コミット**

```bash
git add DevNext/Common/ScheduleEnum.cs
git commit -m "feat: スケジュール機能 Enum を追加（RecurrenceType / ParticipantStatus）"
```

---

## Task 2: エンティティ定義

**Files:**
- Create: `DevNext/Entity/ScheduleEventEntity.cs`
- Create: `DevNext/Entity/ScheduleEventParticipantEntity.cs`

- [ ] **Step 1: ScheduleEventEntity.cs を作成する**

```csharp
using Dev.CommonLibrary.Entity;
using Site.Common;
using System.ComponentModel.DataAnnotations;

namespace Site.Entity
{
    /// <summary>
    /// スケジュールイベントエンティティ（本体）
    /// </summary>
    public class ScheduleEventEntity : ScheduleEventEntityBase { }

    /// <summary>
    /// スケジュールイベント履歴エンティティ
    /// </summary>
    public class ScheduleEventEntityHistory : ScheduleEventEntityBase, IEntityHistory
    {
        [Key]
        public long HistoryId { get; set; }
    }

    /// <summary>
    /// スケジュールイベントエンティティ基底クラス。
    /// SiteEntityBase（Id: long, DelFlag, CreateDate, UpdateDate 等）を継承する。
    /// </summary>
    public abstract class ScheduleEventEntityBase : SiteEntityBase
    {
        /// <summary>件名</summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = "";

        /// <summary>詳細</summary>
        [MaxLength(2000)]
        public string? Description { get; set; }

        /// <summary>開始日時</summary>
        public DateTime StartDate { get; set; }

        /// <summary>終了日時</summary>
        public DateTime EndDate { get; set; }

        /// <summary>終日フラグ</summary>
        public bool IsAllDay { get; set; }

        /// <summary>共有フラグ（false=個人 / true=全体共有）</summary>
        public bool IsShared { get; set; }

        /// <summary>作成者 UserId（ApplicationUser.Id）</summary>
        [Required]
        [MaxLength(450)]
        public string OwnerId { get; set; } = "";

        /// <summary>繰り返し種別</summary>
        public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;

        /// <summary>繰り返し間隔（例: 2週ごとなら2）</summary>
        public int RecurrenceInterval { get; set; } = 1;

        /// <summary>繰り返し終了日</summary>
        public DateTime? RecurrenceEndDate { get; set; }

        /// <summary>
        /// 週次繰り返し時の対象曜日をカンマ区切りで保持する。
        /// DayOfWeek の数値（0=日曜〜6=土曜）を使用する。例: "1,3,5"（月・水・金）
        /// </summary>
        [MaxLength(20)]
        public string? RecurrenceDaysOfWeek { get; set; }
    }
}
```

- [ ] **Step 2: ScheduleEventParticipantEntity.cs を作成する**

```csharp
using Site.Common;
using System.ComponentModel.DataAnnotations;

namespace Site.Entity
{
    /// <summary>
    /// スケジュールイベント参加者エンティティ。
    /// 更新頻度・性質が本体と異なるため履歴テーブルなし。
    /// </summary>
    public class ScheduleEventParticipantEntity : SiteEntityBase
    {
        /// <summary>対象イベント ID（FK → ScheduleEventEntity）</summary>
        public long EventId { get; set; }

        /// <summary>参加者 UserId（FK → ApplicationUser）</summary>
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = "";

        /// <summary>参加ステータス</summary>
        public ParticipantStatus Status { get; set; } = ParticipantStatus.Invited;
    }
}
```

- [ ] **Step 3: ビルドして確認**

```bash
cd DevNext && dotnet build
```
Expected: Build succeeded, 0 errors

- [ ] **Step 4: コミット**

```bash
git add DevNext/Entity/ScheduleEventEntity.cs DevNext/Entity/ScheduleEventParticipantEntity.cs
git commit -m "feat: スケジュール機能 Entity を追加（ScheduleEventEntity / Participant）"
```

---

## Task 3: DBContext 更新

**Files:**
- Modify: `DevNext/Common/DBContext.cs`

- [ ] **Step 1: DBContext に DbSet を追加する**

`DBContext.cs` の `#region DbSet` 末尾（`Notification` の下）に追記する。

```csharp
        // スケジュール
        public DbSet<ScheduleEventEntity> ScheduleEvent { get; set; }
        public DbSet<ScheduleEventEntityHistory> ScheduleEventHistory { get; set; }
        public DbSet<ScheduleEventParticipantEntity> ScheduleEventParticipant { get; set; }
```

- [ ] **Step 2: ビルドして確認**

```bash
cd DevNext && dotnet build
```
Expected: Build succeeded, 0 errors

- [ ] **Step 3: コミット**

```bash
git add DevNext/Common/DBContext.cs
git commit -m "feat: DBContext に ScheduleEvent / ScheduleEventHistory / ScheduleEventParticipant を追加"
```

---

## Task 4: DbMigrationRunner 更新

**Files:**
- Modify: `DbMigrationRunner/Program.cs`

- [ ] **Step 1: ApplyMissingTablesAsync に ScheduleEvent / ScheduleEventHistory / ScheduleEventParticipant を追記する**

`DbMigrationRunner/Program.cs` の `ApplyMissingTablesAsync` メソッド末尾（Notification ブロックの後）に追記する。

```csharp
            // ─── ScheduleEvent ────────────────────────────────────────────────
            Console.WriteLine("  テーブル [ScheduleEvent] を確認しています...");
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ScheduleEvent')
                BEGIN
                    CREATE TABLE [ScheduleEvent] (
                        [Id]                        bigint          NOT NULL IDENTITY(1,1),
                        [Title]                     nvarchar(200)   NOT NULL,
                        [Description]               nvarchar(2000)  NULL,
                        [StartDate]                 datetime2(7)    NOT NULL,
                        [EndDate]                   datetime2(7)    NOT NULL,
                        [IsAllDay]                  bit             NOT NULL,
                        [IsShared]                  bit             NOT NULL,
                        [OwnerId]                   nvarchar(450)   NOT NULL,
                        [RecurrenceType]            int             NOT NULL,
                        [RecurrenceInterval]        int             NOT NULL,
                        [RecurrenceEndDate]         datetime2(7)    NULL,
                        [RecurrenceDaysOfWeek]      nvarchar(20)    NULL,
                        [DelFlag]                   bit             NOT NULL,
                        [UpdateApplicationUserId]   nvarchar(max)   NULL,
                        [CreateApplicationUserId]   nvarchar(max)   NULL,
                        [UpdateDate]                datetime2(7)    NOT NULL,
                        [CreateDate]                datetime2(7)    NOT NULL,
                        CONSTRAINT [PK_ScheduleEvent] PRIMARY KEY ([Id])
                    )
                END");

            // ─── ScheduleEventHistory ─────────────────────────────────────────
            Console.WriteLine("  テーブル [ScheduleEventHistory] を確認しています...");
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ScheduleEventHistory')
                BEGIN
                    CREATE TABLE [ScheduleEventHistory] (
                        [HistoryId]                 bigint          NOT NULL IDENTITY(1,1),
                        [Id]                        bigint          NOT NULL,
                        [Title]                     nvarchar(200)   NOT NULL,
                        [Description]               nvarchar(2000)  NULL,
                        [StartDate]                 datetime2(7)    NOT NULL,
                        [EndDate]                   datetime2(7)    NOT NULL,
                        [IsAllDay]                  bit             NOT NULL,
                        [IsShared]                  bit             NOT NULL,
                        [OwnerId]                   nvarchar(450)   NOT NULL,
                        [RecurrenceType]            int             NOT NULL,
                        [RecurrenceInterval]        int             NOT NULL,
                        [RecurrenceEndDate]         datetime2(7)    NULL,
                        [RecurrenceDaysOfWeek]      nvarchar(20)    NULL,
                        [DelFlag]                   bit             NOT NULL,
                        [UpdateApplicationUserId]   nvarchar(max)   NULL,
                        [CreateApplicationUserId]   nvarchar(max)   NULL,
                        [UpdateDate]                datetime2(7)    NOT NULL,
                        [CreateDate]                datetime2(7)    NOT NULL,
                        CONSTRAINT [PK_ScheduleEventHistory] PRIMARY KEY ([HistoryId])
                    )
                END");

            // ─── ScheduleEventParticipant ─────────────────────────────────────
            Console.WriteLine("  テーブル [ScheduleEventParticipant] を確認しています...");
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ScheduleEventParticipant')
                BEGIN
                    CREATE TABLE [ScheduleEventParticipant] (
                        [Id]                        bigint          NOT NULL IDENTITY(1,1),
                        [EventId]                   bigint          NOT NULL,
                        [UserId]                    nvarchar(450)   NOT NULL,
                        [Status]                    int             NOT NULL,
                        [DelFlag]                   bit             NOT NULL,
                        [UpdateApplicationUserId]   nvarchar(max)   NULL,
                        [CreateApplicationUserId]   nvarchar(max)   NULL,
                        [UpdateDate]                datetime2(7)    NOT NULL,
                        [CreateDate]                datetime2(7)    NOT NULL,
                        CONSTRAINT [PK_ScheduleEventParticipant] PRIMARY KEY ([Id])
                    )
                END");
```

- [ ] **Step 2: DbMigrationRunner をビルドして確認**

```bash
cd DbMigrationRunner && dotnet build
```
Expected: Build succeeded, 0 errors

- [ ] **Step 3: DbMigrationRunner を実行してテーブルを作成**

```bash
cd DbMigrationRunner && dotnet run
```
Expected: `✓ 処理が完了しました。` と表示され、3テーブルが作成される

- [ ] **Step 4: コミット**

```bash
git add DbMigrationRunner/Program.cs
git commit -m "feat: DbMigrationRunner に ScheduleEvent / ScheduleEventHistory / ScheduleEventParticipant テーブル作成を追加"
```

---

## Task 5: 繰り返し展開ヘルパー（TDD）

**Files:**
- Create: `DevNext/Service/ScheduleRecurrenceHelper.cs`
- Create: `Tests/Schedule/ScheduleRecurrenceHelperTests.cs`

繰り返しロジックを DB 非依存の静的クラスに分離し、先にテストを書いてから実装する。

- [ ] **Step 1: テストディレクトリを作成する**

```bash
mkdir -p "H:/ClaudeCode/DevNext/Tests/Schedule"
```

- [ ] **Step 2: 失敗するテストを書く**

`Tests/Schedule/ScheduleRecurrenceHelperTests.cs` を作成する。

```csharp
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
```

- [ ] **Step 3: テストが失敗することを確認**

```bash
cd Tests && dotnet test --filter "ScheduleRecurrenceHelperTests" 2>&1 | head -20
```
Expected: コンパイルエラー（`ScheduleRecurrenceHelper` が未定義）

- [ ] **Step 4: ScheduleRecurrenceHelper.cs を実装する**

```csharp
using Site.Common;

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
            // 繰り返し終了日はウィンドウ終了日とどちらか小さい方
            var effectiveEnd = recEnd.HasValue && recEnd.Value < windowEnd
                ? recEnd.Value
                : windowEnd;

            if (type == RecurrenceType.None)
            {
                // 繰り返しなし: ウィンドウ内に start が含まれれば返す
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

        /// <summary>週次・複数曜日の展開。</summary>
        private static void ExpandWeeklyMultiDay(
            DateTime start, int interval, string daysOfWeek,
            DateTime windowStart, DateTime effectiveEnd, List<DateTime> results)
        {
            var days = daysOfWeek.Split(',').Select(int.Parse).ToArray();
            // start の週の日曜日を起点にして週単位で進める
            var weekSunday = start.Date.AddDays(-(int)start.DayOfWeek);

            for (var week = weekSunday; week < effectiveEnd; week = week.AddDays(7 * interval))
            {
                foreach (var day in days)
                {
                    // 指定曜日の日付 + start の時刻
                    var occurrence = week.AddDays(day).Add(start.TimeOfDay);
                    if (occurrence >= start && occurrence >= windowStart && occurrence < effectiveEnd)
                        results.Add(occurrence);
                }
            }
        }

        /// <summary>繰り返し種別に応じて日時を進める。</summary>
        private static DateTime Advance(DateTime current, RecurrenceType type, int interval)
            => type switch
            {
                RecurrenceType.Daily   => current.AddDays(interval),
                RecurrenceType.Weekly  => current.AddDays(7 * interval),
                RecurrenceType.Monthly => current.AddMonths(interval),
                _ => current.AddYears(100) // 安全な停止値
            };
    }
}
```

- [ ] **Step 5: テストを実行して全件パスすることを確認**

```bash
cd Tests && dotnet test --filter "ScheduleRecurrenceHelperTests" -v
```
Expected: 全テスト PASSED

- [ ] **Step 6: コミット**

```bash
git add DevNext/Service/ScheduleRecurrenceHelper.cs Tests/Schedule/ScheduleRecurrenceHelperTests.cs
git commit -m "feat(TDD): ScheduleRecurrenceHelper を追加・テスト全件パス"
```

---

## Task 6: Repository 実装

**Files:**
- Create: `DevNext/Repository/ScheduleRepository.cs`

- [ ] **Step 1: ScheduleRepository.cs を作成する**

```csharp
using Site.Common;
using Site.Entity;
using Microsoft.EntityFrameworkCore;

namespace Site.Repository
{
    /// <summary>
    /// スケジュールイベントリポジトリ。
    /// 参加者テーブルも含めて管理する。
    /// </summary>
    public class ScheduleRepository
    {
        private readonly DBContext _context;

        public ScheduleRepository(DBContext context)
        {
            _context = context;
        }

        // ─── ScheduleEvent CRUD ───────────────────────────────────────────────

        public ScheduleEventEntity? SelectById(long id)
            => _context.ScheduleEvent.FirstOrDefault(x => x.Id == id && !x.DelFlag);

        public void Insert(ScheduleEventEntity entity)
        {
            _context.ScheduleEvent.Add(entity);
            _context.SaveChanges();
        }

        public void InsertHistory(ScheduleEventEntity entity)
        {
            // 更新前に現在の状態を履歴テーブルへコピーする
            var history = MapToHistory(entity);
            _context.ScheduleEventHistory.Add(history);
            _context.SaveChanges();
        }

        public void Update(ScheduleEventEntity entity)
        {
            _context.ScheduleEvent.Update(entity);
            _context.SaveChanges();
        }

        public void LogicalDelete(ScheduleEventEntity entity)
        {
            entity.SetForLogicalDelete();
            _context.ScheduleEvent.Update(entity);
            _context.SaveChanges();
        }

        // ─── カレンダー範囲取得 ───────────────────────────────────────────────

        /// <summary>
        /// 指定期間に表示すべきイベントを取得する。
        /// - 自分の個人イベント（IsShared=false かつ OwnerId=currentUserId）
        /// - 共有イベント（IsShared=true）
        /// - 自分が参加者として招待されているイベント
        /// 繰り返しイベントは RecurrenceEndDate を考慮してウィンドウ外のものを除外する。
        /// </summary>
        public List<ScheduleEventEntity> GetEventsForRange(
            DateTime rangeStart, DateTime rangeEnd, string currentUserId)
        {
            // 参加者として招待されているイベント ID を取得
            var participantEventIds = _context.ScheduleEventParticipant
                .Where(p => p.UserId == currentUserId && !p.DelFlag)
                .Select(p => p.EventId)
                .ToHashSet();

            return _context.ScheduleEvent
                .Where(x => !x.DelFlag)
                // 閲覧権限: 個人（自分）・共有・招待済み
                .Where(x => (!x.IsShared && x.OwnerId == currentUserId)
                         || x.IsShared
                         || participantEventIds.Contains(x.Id))
                // 期間フィルタ: 繰り返しなしは EndDate, 繰り返しありは RecurrenceEndDate で判断
                .Where(x => x.StartDate < rangeEnd
                         && (x.RecurrenceType != RecurrenceType.None
                             ? (!x.RecurrenceEndDate.HasValue || x.RecurrenceEndDate.Value >= rangeStart)
                             : x.EndDate > rangeStart))
                .ToList();
        }

        // ─── Participant 操作 ─────────────────────────────────────────────────

        public List<ScheduleEventParticipantEntity> GetParticipants(long eventId)
            => _context.ScheduleEventParticipant
                .Where(x => x.EventId == eventId && !x.DelFlag)
                .ToList();

        public ScheduleEventParticipantEntity? GetParticipant(long eventId, string userId)
            => _context.ScheduleEventParticipant
                .FirstOrDefault(x => x.EventId == eventId && x.UserId == userId && !x.DelFlag);

        public void InsertParticipant(ScheduleEventParticipantEntity entity)
        {
            _context.ScheduleEventParticipant.Add(entity);
            _context.SaveChanges();
        }

        public void UpdateParticipant(ScheduleEventParticipantEntity entity)
        {
            _context.ScheduleEventParticipant.Update(entity);
            _context.SaveChanges();
        }

        public void DeleteParticipantsByEventId(long eventId)
        {
            // イベント削除時に参加者も論理削除する
            var participants = _context.ScheduleEventParticipant
                .Where(x => x.EventId == eventId && !x.DelFlag)
                .ToList();
            foreach (var p in participants)
                p.SetForLogicalDelete();
            _context.SaveChanges();
        }

        // ─── 内部ユーティリティ ───────────────────────────────────────────────

        private static ScheduleEventEntityHistory MapToHistory(ScheduleEventEntity src)
            => new()
            {
                Id                    = src.Id,
                Title                 = src.Title,
                Description           = src.Description,
                StartDate             = src.StartDate,
                EndDate               = src.EndDate,
                IsAllDay              = src.IsAllDay,
                IsShared              = src.IsShared,
                OwnerId               = src.OwnerId,
                RecurrenceType        = src.RecurrenceType,
                RecurrenceInterval    = src.RecurrenceInterval,
                RecurrenceEndDate     = src.RecurrenceEndDate,
                RecurrenceDaysOfWeek  = src.RecurrenceDaysOfWeek,
                DelFlag               = src.DelFlag,
                CreateDate            = src.CreateDate,
                UpdateDate            = src.UpdateDate,
                CreateApplicationUserId = src.CreateApplicationUserId,
                UpdateApplicationUserId = src.UpdateApplicationUserId,
            };
    }
}
```

- [ ] **Step 2: ビルドして確認**

```bash
cd DevNext && dotnet build
```
Expected: Build succeeded, 0 errors

- [ ] **Step 3: コミット**

```bash
git add DevNext/Repository/ScheduleRepository.cs
git commit -m "feat: ScheduleRepository を追加"
```

---

## Task 7: ViewModel 定義

**Files:**
- Create: `DevNext/Models/ScheduleViewModels.cs`

- [ ] **Step 1: ScheduleViewModels.cs を作成する**

```csharp
using Microsoft.AspNetCore.Mvc.Rendering;
using Site.Common;
using System.ComponentModel.DataAnnotations;

namespace Site.Models
{
    /// <summary>
    /// FullCalendar に渡す JSON イベント DTO。
    /// FullCalendar の仕様に合わせてプロパティ名をキャメルケースで定義する。
    /// </summary>
    public class ScheduleEventJsonDto
    {
        /// <summary>イベント ID（文字列型で渡す）</summary>
        public string Id { get; set; } = "";
        /// <summary>タイトル</summary>
        public string Title { get; set; } = "";
        /// <summary>開始日時（ISO 8601）</summary>
        public string Start { get; set; } = "";
        /// <summary>終了日時（ISO 8601）</summary>
        public string End { get; set; } = "";
        /// <summary>終日フラグ</summary>
        public bool AllDay { get; set; }
        /// <summary>表示色（個人=青 / 共有自分=緑 / 招待=橙）</summary>
        public string Color { get; set; } = "";
        /// <summary>共有フラグ（モーダル表示用）</summary>
        public bool IsShared { get; set; }
        /// <summary>作成者 ID（編集・削除ボタンの表示判定用）</summary>
        public string OwnerId { get; set; } = "";
    }

    /// <summary>
    /// 予定作成・編集フォーム ViewModel
    /// </summary>
    public class ScheduleEventFormViewModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "件名は必須です")]
        [MaxLength(200, ErrorMessage = "件名は200文字以内で入力してください")]
        [Display(Name = "件名")]
        public string Title { get; set; } = "";

        [MaxLength(2000, ErrorMessage = "詳細は2000文字以内で入力してください")]
        [Display(Name = "詳細")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "開始日時は必須です")]
        [Display(Name = "開始日時")]
        public DateTime StartDate { get; set; } = DateTime.Today.AddHours(9);

        [Required(ErrorMessage = "終了日時は必須です")]
        [Display(Name = "終了日時")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddHours(10);

        [Display(Name = "終日")]
        public bool IsAllDay { get; set; }

        [Display(Name = "全体共有")]
        public bool IsShared { get; set; }

        [Display(Name = "繰り返し")]
        public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;

        [Display(Name = "繰り返し間隔")]
        [Range(1, 99, ErrorMessage = "繰り返し間隔は1〜99を指定してください")]
        public int RecurrenceInterval { get; set; } = 1;

        [Display(Name = "繰り返し終了日")]
        public DateTime? RecurrenceEndDate { get; set; }

        /// <summary>週次繰り返し時の選択曜日（DayOfWeek の数値リスト）</summary>
        public List<int> SelectedDaysOfWeek { get; set; } = new();

        /// <summary>招待する参加者の UserId リスト</summary>
        public List<string> ParticipantUserIds { get; set; } = new();

        // ─── View 用表示リスト ──────────────────────────────────────────────

        /// <summary>参加者候補ユーザー一覧（作成者本人を除く全ユーザー）</summary>
        public List<SelectListItem> UserList { get; set; } = new();
    }

    /// <summary>
    /// 予定詳細表示 DTO（モーダル用 JSON）
    /// </summary>
    public class ScheduleEventDetailDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string StartDate { get; set; } = "";  // "yyyy/MM/dd HH:mm"
        public string EndDate { get; set; } = "";
        public bool IsAllDay { get; set; }
        public bool IsShared { get; set; }
        public string OwnerName { get; set; } = "";
        public string OwnerId { get; set; } = "";
        public RecurrenceType RecurrenceType { get; set; }
        public int RecurrenceInterval { get; set; }
        public string? RecurrenceEndDate { get; set; }  // "yyyy/MM/dd"
        public string? RecurrenceDaysOfWeek { get; set; }
        public List<ParticipantDetailDto> Participants { get; set; } = new();
    }

    /// <summary>
    /// 参加者詳細 DTO
    /// </summary>
    public class ParticipantDetailDto
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public ParticipantStatus Status { get; set; }
        public string StatusLabel { get; set; } = "";
    }
}
```

- [ ] **Step 2: ビルドして確認**

```bash
cd DevNext && dotnet build
```
Expected: Build succeeded, 0 errors

- [ ] **Step 3: コミット**

```bash
git add DevNext/Models/ScheduleViewModels.cs
git commit -m "feat: スケジュール機能 ViewModel を追加"
```

---

## Task 8: Service 実装

**Files:**
- Create: `DevNext/Service/ScheduleService.cs`

- [ ] **Step 1: ScheduleService.cs を作成する**

```csharp
using Microsoft.AspNetCore.Identity;
using Site.Common;
using Site.Entity;
using Site.Models;
using Site.Repository;

namespace Site.Service
{
    /// <summary>
    /// スケジュール機能のビジネスロジック。
    /// 繰り返し展開は ScheduleRecurrenceHelper に委譲する。
    /// </summary>
    public class ScheduleService
    {
        private readonly DBContext _context;
        private readonly ScheduleRepository _repo;
        private readonly UserManager<ApplicationUser> _userManager;

        public ScheduleService(DBContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _repo = new ScheduleRepository(context);
            _userManager = userManager;
        }

        // ─── カレンダーイベント取得 ───────────────────────────────────────────

        /// <summary>
        /// FullCalendar 用のイベント JSON リストを返す。
        /// 繰り返しイベントは rangeStart〜rangeEnd の範囲で展開する。
        /// </summary>
        public List<ScheduleEventJsonDto> GetEventsForRange(
            DateTime rangeStart, DateTime rangeEnd, string currentUserId)
        {
            var entities = _repo.GetEventsForRange(rangeStart, rangeEnd, currentUserId);
            var result = new List<ScheduleEventJsonDto>();

            foreach (var ev in entities)
            {
                // 繰り返し展開ヘルパーで発生日リストを取得
                var occurrences = ScheduleRecurrenceHelper.GetOccurrences(
                    ev.StartDate, ev.RecurrenceType, ev.RecurrenceInterval,
                    ev.RecurrenceEndDate, ev.RecurrenceDaysOfWeek,
                    rangeStart, rangeEnd);

                var duration = ev.EndDate - ev.StartDate;
                var color = GetEventColor(ev, currentUserId);

                foreach (var occStart in occurrences)
                {
                    result.Add(new ScheduleEventJsonDto
                    {
                        Id      = ev.Id.ToString(),
                        Title   = ev.Title,
                        Start   = occStart.ToString("yyyy-MM-ddTHH:mm:ss"),
                        End     = (occStart + duration).ToString("yyyy-MM-ddTHH:mm:ss"),
                        AllDay  = ev.IsAllDay,
                        Color   = color,
                        IsShared = ev.IsShared,
                        OwnerId = ev.OwnerId,
                    });
                }
            }

            return result;
        }

        // ─── 詳細取得 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 予定の詳細を取得する（モーダル表示用 JSON）。
        /// </summary>
        public async Task<ScheduleEventDetailDto?> GetDetailAsync(long id)
        {
            var entity = _repo.SelectById(id);
            if (entity == null) return null;

            var owner = await _userManager.FindByIdAsync(entity.OwnerId);
            var participants = _repo.GetParticipants(id);
            var participantDtos = new List<ParticipantDetailDto>();

            foreach (var p in participants)
            {
                var user = await _userManager.FindByIdAsync(p.UserId);
                participantDtos.Add(new ParticipantDetailDto
                {
                    UserId      = p.UserId,
                    UserName    = user?.UserName ?? p.UserId,
                    Status      = p.Status,
                    StatusLabel = p.Status switch
                    {
                        ParticipantStatus.Accepted => "承諾",
                        ParticipantStatus.Declined => "辞退",
                        _                          => "未回答",
                    },
                });
            }

            return new ScheduleEventDetailDto
            {
                Id                   = entity.Id,
                Title                = entity.Title,
                Description          = entity.Description,
                StartDate            = entity.StartDate.ToString("yyyy/MM/dd HH:mm"),
                EndDate              = entity.EndDate.ToString("yyyy/MM/dd HH:mm"),
                IsAllDay             = entity.IsAllDay,
                IsShared             = entity.IsShared,
                OwnerName            = owner?.UserName ?? entity.OwnerId,
                OwnerId              = entity.OwnerId,
                RecurrenceType       = entity.RecurrenceType,
                RecurrenceInterval   = entity.RecurrenceInterval,
                RecurrenceEndDate    = entity.RecurrenceEndDate?.ToString("yyyy/MM/dd"),
                RecurrenceDaysOfWeek = entity.RecurrenceDaysOfWeek,
                Participants         = participantDtos,
            };
        }

        // ─── フォーム用データ取得 ─────────────────────────────────────────────

        /// <summary>
        /// 作成フォーム用 ViewModel を生成する（ユーザーリストを付与）。
        /// </summary>
        public async Task<ScheduleEventFormViewModel> BuildCreateFormAsync(
            string currentUserId, DateTime? defaultStart = null)
        {
            var start = defaultStart ?? DateTime.Today.AddHours(9);
            return new ScheduleEventFormViewModel
            {
                StartDate = start,
                EndDate   = start.AddHours(1),
                UserList  = await GetUserSelectListAsync(currentUserId),
            };
        }

        /// <summary>
        /// 編集フォーム用 ViewModel を生成する。
        /// 作成者以外が呼び出した場合は null を返す。
        /// </summary>
        public async Task<ScheduleEventFormViewModel?> GetForEditAsync(long id, string currentUserId)
        {
            var entity = _repo.SelectById(id);
            // 存在しないか作成者以外はアクセス不可
            if (entity == null || entity.OwnerId != currentUserId) return null;

            var participants = _repo.GetParticipants(id);

            return new ScheduleEventFormViewModel
            {
                Id                   = entity.Id,
                Title                = entity.Title,
                Description          = entity.Description,
                StartDate            = entity.StartDate,
                EndDate              = entity.EndDate,
                IsAllDay             = entity.IsAllDay,
                IsShared             = entity.IsShared,
                RecurrenceType       = entity.RecurrenceType,
                RecurrenceInterval   = entity.RecurrenceInterval,
                RecurrenceEndDate    = entity.RecurrenceEndDate,
                SelectedDaysOfWeek   = ParseDaysOfWeek(entity.RecurrenceDaysOfWeek),
                ParticipantUserIds   = participants.Select(p => p.UserId).ToList(),
                UserList             = await GetUserSelectListAsync(currentUserId),
            };
        }

        // ─── 作成・更新・削除 ─────────────────────────────────────────────────

        /// <summary>
        /// 予定を新規作成する。
        /// </summary>
        public void Create(ScheduleEventFormViewModel vm, string currentUserId)
        {
            var entity = new ScheduleEventEntity
            {
                Title                = vm.Title,
                Description          = vm.Description,
                StartDate            = vm.StartDate,
                EndDate              = vm.EndDate,
                IsAllDay             = vm.IsAllDay,
                IsShared             = vm.IsShared,
                OwnerId              = currentUserId,
                RecurrenceType       = vm.RecurrenceType,
                RecurrenceInterval   = vm.RecurrenceInterval,
                RecurrenceDaysOfWeek = BuildDaysOfWeekString(vm),
                RecurrenceEndDate    = vm.RecurrenceType != RecurrenceType.None
                                           ? vm.RecurrenceEndDate
                                           : null,
            };
            entity.SetForCreate();
            _repo.Insert(entity);

            // 参加者を登録する
            foreach (var userId in vm.ParticipantUserIds.Distinct())
            {
                if (userId == currentUserId) continue; // 作成者自身は除外
                var participant = new ScheduleEventParticipantEntity
                {
                    EventId = entity.Id,
                    UserId  = userId,
                    Status  = ParticipantStatus.Invited,
                };
                participant.SetForCreate();
                _repo.InsertParticipant(participant);
            }
        }

        /// <summary>
        /// 予定を更新する。
        /// 作成者以外が呼び出した場合は false を返す。
        /// </summary>
        public bool Update(ScheduleEventFormViewModel vm, string currentUserId)
        {
            var entity = _repo.SelectById(vm.Id);
            if (entity == null || entity.OwnerId != currentUserId) return false;

            // 更新前に履歴を保存する
            _repo.InsertHistory(entity);

            entity.Title                = vm.Title;
            entity.Description          = vm.Description;
            entity.StartDate            = vm.StartDate;
            entity.EndDate              = vm.EndDate;
            entity.IsAllDay             = vm.IsAllDay;
            entity.IsShared             = vm.IsShared;
            entity.RecurrenceType       = vm.RecurrenceType;
            entity.RecurrenceInterval   = vm.RecurrenceInterval;
            entity.RecurrenceDaysOfWeek = BuildDaysOfWeekString(vm);
            entity.RecurrenceEndDate    = vm.RecurrenceType != RecurrenceType.None
                                              ? vm.RecurrenceEndDate
                                              : null;
            entity.SetForUpdate();
            _repo.Update(entity);

            // 参加者を洗い替えする（既存を全削除して再登録）
            _repo.DeleteParticipantsByEventId(entity.Id);
            foreach (var userId in vm.ParticipantUserIds.Distinct())
            {
                if (userId == currentUserId) continue;
                var participant = new ScheduleEventParticipantEntity
                {
                    EventId = entity.Id,
                    UserId  = userId,
                    Status  = ParticipantStatus.Invited,
                };
                participant.SetForCreate();
                _repo.InsertParticipant(participant);
            }

            return true;
        }

        /// <summary>
        /// 予定を論理削除する。
        /// 作成者以外が呼び出した場合は false を返す。
        /// </summary>
        public bool Delete(long id, string currentUserId)
        {
            var entity = _repo.SelectById(id);
            if (entity == null || entity.OwnerId != currentUserId) return false;

            _repo.DeleteParticipantsByEventId(id);
            _repo.LogicalDelete(entity);
            return true;
        }

        /// <summary>
        /// 参加ステータスを更新する。
        /// 対象参加者でない場合は false を返す。
        /// </summary>
        public bool UpdateParticipantStatus(long eventId, string currentUserId, ParticipantStatus status)
        {
            var participant = _repo.GetParticipant(eventId, currentUserId);
            if (participant == null) return false;

            participant.Status = status;
            participant.SetForUpdate();
            _repo.UpdateParticipant(participant);
            return true;
        }

        // ─── 内部ユーティリティ ───────────────────────────────────────────────

        /// <summary>
        /// イベントの表示色を決定する。
        /// 個人=青 / 共有かつ自分作成=緑 / 招待された共有=橙
        /// </summary>
        private static string GetEventColor(ScheduleEventEntity ev, string currentUserId)
        {
            if (!ev.IsShared)                   return "#0d6efd"; // 個人: 青
            if (ev.OwnerId == currentUserId)    return "#198754"; // 共有・自分作成: 緑
            return "#fd7e14";                                     // 招待: 橙
        }

        /// <summary>フォームの SelectedDaysOfWeek をカンマ区切り文字列に変換する。</summary>
        private static string? BuildDaysOfWeekString(ScheduleEventFormViewModel vm)
        {
            if (vm.RecurrenceType != RecurrenceType.Weekly || vm.SelectedDaysOfWeek.Count == 0)
                return null;
            return string.Join(",", vm.SelectedDaysOfWeek.Distinct().OrderBy(d => d));
        }

        /// <summary>カンマ区切り文字列を int リストに変換する。</summary>
        private static List<int> ParseDaysOfWeek(string? daysOfWeek)
        {
            if (string.IsNullOrEmpty(daysOfWeek)) return new List<int>();
            return daysOfWeek.Split(',').Select(int.Parse).ToList();
        }

        /// <summary>
        /// 参加者候補のユーザーリストを生成する（現在のユーザー本人を除く）。
        /// </summary>
        private async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetUserSelectListAsync(
            string excludeUserId)
        {
            var users = _userManager.Users
                .Where(u => u.Id != excludeUserId)
                .OrderBy(u => u.UserName)
                .ToList();

            return users.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = u.Id,
                Text  = u.UserName ?? u.Id,
            }).ToList();
        }
    }
}
```

- [ ] **Step 2: ビルドして確認**

```bash
cd DevNext && dotnet build
```
Expected: Build succeeded, 0 errors

- [ ] **Step 3: コミット**

```bash
git add DevNext/Service/ScheduleService.cs
git commit -m "feat: ScheduleService を追加（繰り返し展開・CRUD・参加者管理）"
```

---

## Task 9: Controller 実装

**Files:**
- Create: `DevNext/Controllers/ScheduleController.cs`

- [ ] **Step 1: ScheduleController.cs を作成する**

```csharp
using Dev.CommonLibrary.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Site.Common;
using Site.Models;
using Site.Service;
using System.Security.Claims;

namespace Site.Controllers
{
    /// <summary>
    /// スケジュール・カレンダー Controller。
    /// GetEvents / Detail は JSON API として動作する。
    /// </summary>
    [Authorize]
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class ScheduleController : Controller
    {
        private readonly ScheduleService _service;

        public ScheduleController(ScheduleService service)
        {
            _service = service;
        }

        // ─── カレンダー画面 ───────────────────────────────────────────────────

        /// <summary>
        /// GET: カレンダーメイン画面
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ─── JSON API（FullCalendar 用） ───────────────────────────────────────

        /// <summary>
        /// GET: FullCalendar 用イベント JSON
        /// FullCalendar は /Schedule/GetEvents?start=2026-04-01&amp;end=2026-05-01 の形式でリクエストする。
        /// </summary>
        [HttpGet]
        public IActionResult GetEvents(DateTime start, DateTime end)
        {
            try
            {
                var events = _service.GetEventsForRange(start, end, GetCurrentUserId());
                return Json(events);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET: 予定詳細 JSON（モーダル用）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Detail(long id)
        {
            var detail = await _service.GetDetailAsync(id);
            if (detail == null)
                return Json(new { error = "予定が見つかりませんでした。" });
            return Json(detail);
        }

        // ─── 作成 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// GET: 作成フォーム（モーダル用）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create(DateTime? defaultStart = null)
        {
            var vm = await _service.BuildCreateFormAsync(GetCurrentUserId(), defaultStart);
            return PartialView("_EventFormModal", vm);
        }

        /// <summary>
        /// POST: 予定作成
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ScheduleEventFormViewModel vm)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, errors = GetModelErrors() });

            if (vm.EndDate <= vm.StartDate)
            {
                ModelState.AddModelError("EndDate", "終了日時は開始日時より後に設定してください。");
                return Json(new { success = false, errors = GetModelErrors() });
            }

            _service.Create(vm, GetCurrentUserId());
            return Json(new { success = true });
        }

        // ─── 編集 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// GET: 編集フォーム（モーダル用）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var vm = await _service.GetForEditAsync(id, GetCurrentUserId());
            if (vm == null) return Forbid();
            return PartialView("_EventFormModal", vm);
        }

        /// <summary>
        /// POST: 予定更新
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ScheduleEventFormViewModel vm)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, errors = GetModelErrors() });

            if (vm.EndDate <= vm.StartDate)
            {
                ModelState.AddModelError("EndDate", "終了日時は開始日時より後に設定してください。");
                return Json(new { success = false, errors = GetModelErrors() });
            }

            bool updated = _service.Update(vm, GetCurrentUserId());
            if (!updated) return Json(new { success = false, errors = new[] { "更新権限がありません。" } });
            return Json(new { success = true });
        }

        // ─── 削除 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// POST: 予定削除
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(long id)
        {
            bool deleted = _service.Delete(id, GetCurrentUserId());
            if (!deleted) return Json(new { success = false, error = "削除権限がありません。" });
            return Json(new { success = true });
        }

        // ─── 参加ステータス更新 ───────────────────────────────────────────────

        /// <summary>
        /// POST: 参加ステータス更新（Accepted / Declined）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateParticipantStatus(long eventId, ParticipantStatus status)
        {
            bool updated = _service.UpdateParticipantStatus(eventId, GetCurrentUserId(), status);
            if (!updated) return Json(new { success = false, error = "対象の参加者ではありません。" });
            return Json(new { success = true });
        }

        // ─── 内部ユーティリティ ───────────────────────────────────────────────

        private string GetCurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private List<string> GetModelErrors()
            => ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
    }
}
```

- [ ] **Step 2: ビルドして確認**

```bash
cd DevNext && dotnet build
```
Expected: Build succeeded, 0 errors

- [ ] **Step 3: コミット**

```bash
git add DevNext/Controllers/ScheduleController.cs
git commit -m "feat: ScheduleController を追加"
```

---

## Task 10: View 実装（FullCalendar）

**Files:**
- Create: `DevNext/Views/Schedule/Index.cshtml`
- Create: `DevNext/Views/Schedule/_EventFormModal.cshtml`（部分ビュー）

- [ ] **Step 1: Views/Schedule/ ディレクトリを作成する**

```bash
mkdir -p "H:/ClaudeCode/DevNext/DevNext/Views/Schedule"
```

- [ ] **Step 2: Index.cshtml を作成する**

```html
@{
    ViewData["Title"] = "スケジュール";
}

@* FullCalendar 6.x CDN *@
<link href="https://cdn.jsdelivr.net/npm/fullcalendar@6.1.15/index.global.min.css" rel="stylesheet" />
<script src="https://cdn.jsdelivr.net/npm/fullcalendar@6.1.15/index.global.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/@@fullcalendar/core@6.1.15/locales/ja.global.min.js"></script>

<div class="container-fluid mt-3">
    <div class="d-flex justify-content-between align-items-center mb-3">
        <h4><i class="fas fa-calendar-alt me-2"></i>スケジュール</h4>
        <button type="button" class="btn btn-primary" id="btnNewEvent">
            <i class="fas fa-plus me-1"></i>新規作成
        </button>
    </div>

    <!-- 凡例-->}}
    <div class="d-flex gap-3 mb-2 small">
        <span><span class="badge" style="background:#0d6efd">●</span> 個人予定</span>
        <span><span class="badge" style="background:#198754">●</span> 共有予定（自分）</span>
        <span><span class="badge" style="background:#fd7e14">●</span> 招待された予定</span>
    </div>

    <div id="calendar"></div>
</div>

<!-- 詳細モーダル-->}}
<div class="modal fade" id="detailModal" tabindex="-1" aria-labelledby="detailModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="detailModalLabel">予定詳細</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body" id="detailModalBody">
                <div class="text-center"><div class="spinner-border" role="status"></div></div>
            </div>
            <div class="modal-footer" id="detailModalFooter"></div>
        </div>
    </div>
</div>

<!-- 作成・編集モーダル-->}}
<div class="modal fade" id="formModal" tabindex="-1" aria-labelledby="formModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="formModalLabel">予定</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body" id="formModalBody">
                <div class="text-center"><div class="spinner-border" role="status"></div></div>
            </div>
        </div>
    </div>
</div>

@section Scripts {
<script>
    // ─── FullCalendar 初期化 ────────────────────────────────────────────────
    var calendarEl = document.getElementById('calendar');
    var calendar = new FullCalendar.Calendar(calendarEl, {
        locale: 'ja',
        initialView: 'dayGridMonth',
        headerToolbar: {
            left:   'prev,next today',
            center: 'title',
            right:  'dayGridMonth,timeGridWeek,timeGridDay'
        },
        height: 'auto',
        // FullCalendar が start/end パラメータ付きで呼び出す
        events: '/Schedule/GetEvents',
        // 日付クリック → 作成モーダルを開く（クリックした日付をデフォルト値に設定）
        dateClick: function(info) {
            openFormModal('/Schedule/Create?defaultStart=' + info.dateStr + 'T09:00:00', '新規予定作成');
        },
        // イベントクリック → 詳細モーダルを開く
        eventClick: function(info) {
            openDetailModal(info.event.id, info.event.extendedProps.ownerId);
        }
    });
    calendar.render();

    // ─── 新規作成ボタン ──────────────────────────────────────────────────────
    document.getElementById('btnNewEvent').addEventListener('click', function() {
        openFormModal('/Schedule/Create', '新規予定作成');
    });

    // ─── 詳細モーダル ────────────────────────────────────────────────────────
    function openDetailModal(eventId, ownerId) {
        var modal = new bootstrap.Modal(document.getElementById('detailModal'));
        var body = document.getElementById('detailModalBody');
        var footer = document.getElementById('detailModalFooter');
        body.innerHTML = '<div class="text-center"><div class="spinner-border" role="status"></div></div>';
        footer.innerHTML = '';
        modal.show();

        fetch('/Schedule/Detail/' + eventId)
            .then(function(res) { return res.json(); })
            .then(function(data) {
                if (data.error) { body.innerHTML = '<p class="text-danger">' + data.error + '</p>'; return; }
                renderDetailBody(body, data);
                renderDetailFooter(footer, data, modal);
            });
    }

    function renderDetailBody(container, d) {
        var recLabel = getRecurrenceLabel(d.recurrenceType, d.recurrenceInterval, d.recurrenceDaysOfWeek, d.recurrenceEndDate);
        var participantHtml = d.participants.length === 0 ? '<span class="text-muted">なし</span>' :
            d.participants.map(function(p) {
                var badge = p.status === 1 ? 'bg-success' : p.status === 2 ? 'bg-danger' : 'bg-secondary';
                return '<span class="badge ' + badge + ' me-1">' + escHtml(p.userName) + '（' + p.statusLabel + '）</span>';
            }).join('');

        container.innerHTML =
            '<dl class="row mb-0">' +
            '<dt class="col-sm-3">件名</dt><dd class="col-sm-9">' + escHtml(d.title) + '</dd>' +
            '<dt class="col-sm-3">詳細</dt><dd class="col-sm-9">' + escHtml(d.description || '―') + '</dd>' +
            '<dt class="col-sm-3">開始</dt><dd class="col-sm-9">' + d.startDate + '</dd>' +
            '<dt class="col-sm-3">終了</dt><dd class="col-sm-9">' + d.endDate + '</dd>' +
            '<dt class="col-sm-3">種別</dt><dd class="col-sm-9">' + (d.isShared ? '<span class="badge bg-success">共有</span>' : '<span class="badge bg-primary">個人</span>') + '</dd>' +
            '<dt class="col-sm-3">作成者</dt><dd class="col-sm-9">' + escHtml(d.ownerName) + '</dd>' +
            (recLabel ? '<dt class="col-sm-3">繰り返し</dt><dd class="col-sm-9">' + recLabel + '</dd>' : '') +
            '<dt class="col-sm-3">参加者</dt><dd class="col-sm-9">' + participantHtml + '</dd>' +
            '</dl>';
    }

    function renderDetailFooter(container, d, modal) {
        var currentUserId = '@User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value';
        var btns = '';
        // 参加ステータス変更ボタン（自分が参加者の場合のみ表示）
        var isParticipant = d.participants.some(function(p) { return p.userId === currentUserId; });
        if (isParticipant) {
            btns += '<button class="btn btn-success btn-sm me-1" onclick="updateStatus(' + d.id + ', 1, this)">承諾</button>';
            btns += '<button class="btn btn-danger btn-sm me-1" onclick="updateStatus(' + d.id + ', 2, this)">辞退</button>';
        }
        // 編集・削除ボタン（作成者のみ）
        if (d.ownerId === currentUserId) {
            btns += '<button class="btn btn-outline-primary btn-sm me-1" onclick="openEditModal(' + d.id + ', modal)">編集</button>';
            btns += '<button class="btn btn-outline-danger btn-sm" onclick="deleteEvent(' + d.id + ', modal)">削除</button>';
        }
        btns += '<button type="button" class="btn btn-secondary btn-sm ms-1" data-bs-dismiss="modal">閉じる</button>';
        container.innerHTML = btns;
        // modal 参照を変数にバインドしておく
        container._modal = modal;
    }

    // ─── 作成・編集モーダル ───────────────────────────────────────────────────
    function openFormModal(url, title) {
        document.getElementById('formModalLabel').textContent = title;
        var body = document.getElementById('formModalBody');
        body.innerHTML = '<div class="text-center"><div class="spinner-border" role="status"></div></div>';
        var modal = new bootstrap.Modal(document.getElementById('formModal'));
        modal.show();

        fetch(url)
            .then(function(res) { return res.text(); })
            .then(function(html) { body.innerHTML = html; });
    }

    function openEditModal(eventId, detailModal) {
        detailModal.hide();
        openFormModal('/Schedule/Edit/' + eventId, '予定編集');
    }

    // ─── フォーム送信（モーダル内から呼ばれる） ──────────────────────────────
    window.submitScheduleForm = function(form) {
        var fd = new FormData(form);
        fetch(form.action, { method: 'POST', body: fd })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                if (data.success) {
                    bootstrap.Modal.getInstance(document.getElementById('formModal')).hide();
                    calendar.refetchEvents();
                } else {
                    var errDiv = document.getElementById('formErrors');
                    if (errDiv) errDiv.innerHTML = (data.errors || [data.error]).map(function(e) {
                        return '<p class="mb-0">' + escHtml(e) + '</p>';
                    }).join('');
                }
            });
        return false; // フォームのデフォルト送信を防止
    };

    // ─── 削除 ────────────────────────────────────────────────────────────────
    function deleteEvent(eventId, modal) {
        if (!confirm('この予定を削除しますか？')) return;
        var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        fetch('/Schedule/Delete/' + eventId, {
            method: 'POST',
            headers: { 'RequestVerificationToken': token, 'Content-Type': 'application/x-www-form-urlencoded' },
            body: '__RequestVerificationToken=' + encodeURIComponent(token) + '&id=' + eventId
        })
        .then(function(res) { return res.json(); })
        .then(function(data) {
            if (data.success) { modal.hide(); calendar.refetchEvents(); }
            else { alert(data.error || '削除に失敗しました。'); }
        });
    }

    // ─── 参加ステータス更新 ───────────────────────────────────────────────────
    function updateStatus(eventId, status, btn) {
        var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        fetch('/Schedule/UpdateParticipantStatus', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: '__RequestVerificationToken=' + encodeURIComponent(token) +
                  '&eventId=' + eventId + '&status=' + status
        })
        .then(function(res) { return res.json(); })
        .then(function(data) {
            if (data.success) { btn.textContent += ' ✓'; btn.disabled = true; }
            else { alert(data.error || '更新に失敗しました。'); }
        });
    }

    // ─── ユーティリティ ──────────────────────────────────────────────────────
    function escHtml(str) {
        if (!str) return '';
        return str.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
    }

    function getRecurrenceLabel(type, interval, daysOfWeek, endDate) {
        if (type === 0) return '';
        var labels = { 1: '毎日', 2: '毎週', 3: '毎月' };
        var label = (interval > 1 ? interval + '回ごと' : '') + (labels[type] || '');
        if (type === 2 && daysOfWeek) {
            var dayNames = ['日','月','火','水','木','金','土'];
            label += '（' + daysOfWeek.split(',').map(function(d) { return dayNames[parseInt(d)]; }).join('・') + '）';
        }
        if (endDate) label += '（' + endDate + ' まで）';
        return label;
    }
</script>
@* CSRF トークン（JS からの POST で使用） *@
@Html.AntiForgeryToken()
}
```

- [ ] **Step 3: _EventFormModal.cshtml（部分ビュー）を作成する**

```html
@model Site.Models.ScheduleEventFormViewModel
@using Site.Common

<form id="scheduleForm" action="@(Model.Id == 0 ? "/Schedule/Create" : "/Schedule/Edit/" + Model.Id)"
      method="post" onsubmit="return submitScheduleForm(this)">
    @Html.AntiForgeryToken()
    <input type="hidden" asp-for="Id" />

    <div id="formErrors" class="alert alert-danger d-none"></div>

    <div class="modal-body">
        <!-- 件名-->}}
        <div class="mb-3">
            <label asp-for="Title" class="form-label fw-bold"></label>
            <input asp-for="Title" class="form-control" />
            <span asp-validation-for="Title" class="text-danger"></span>
        </div>

        <!-- 詳細-->}}
        <div class="mb-3">
            <label asp-for="Description" class="form-label"></label>
            <textarea asp-for="Description" class="form-control" rows="3"></textarea>
        </div>

        <!-- 開始・終了-->}}
        <div class="row mb-3">
            <div class="col-md-6">
                <label asp-for="StartDate" class="form-label fw-bold"></label>
                <input asp-for="StartDate" type="datetime-local" class="form-control"
                       value="@Model.StartDate.ToString("yyyy-MM-ddTHH:mm")" />
                <span asp-validation-for="StartDate" class="text-danger"></span>
            </div>
            <div class="col-md-6">
                <label asp-for="EndDate" class="form-label fw-bold"></label>
                <input asp-for="EndDate" type="datetime-local" class="form-control"
                       value="@Model.EndDate.ToString("yyyy-MM-ddTHH:mm")" />
                <span asp-validation-for="EndDate" class="text-danger"></span>
            </div>
        </div>

        <!-- 終日・共有-->}}
        <div class="row mb-3">
            <div class="col-md-6">
                <div class="form-check">
                    <input asp-for="IsAllDay" class="form-check-input" />
                    <label asp-for="IsAllDay" class="form-check-label">終日</label>
                </div>
            </div>
            <div class="col-md-6">
                <div class="form-check">
                    <input asp-for="IsShared" class="form-check-input" />
                    <label asp-for="IsShared" class="form-check-label">全体共有</label>
                </div>
            </div>
        </div>

        <!-- 繰り返し-->}}
        <div class="mb-3">
            <label asp-for="RecurrenceType" class="form-label fw-bold">繰り返し</label>
            <select asp-for="RecurrenceType" class="form-select" id="recurrenceTypeSelect">
                <option value="0">なし</option>
                <option value="1">毎日</option>
                <option value="2">毎週</option>
                <option value="3">毎月</option>
            </select>
        </div>

        <div id="recurrenceOptions" style="display:@(Model.RecurrenceType != RecurrenceType.None ? "block" : "none")">
            <div class="row mb-3">
                <div class="col-md-4">
                    <label asp-for="RecurrenceInterval" class="form-label">間隔</label>
                    <input asp-for="RecurrenceInterval" type="number" class="form-control" min="1" max="99" />
                </div>
                <div class="col-md-8">
                    <label asp-for="RecurrenceEndDate" class="form-label">終了日</label>
                    <input asp-for="RecurrenceEndDate" type="date" class="form-control"
                           value="@Model.RecurrenceEndDate?.ToString("yyyy-MM-dd")" />
                </div>
            </div>

            <!-- 曜日選択（週次のみ表示）-->}}
            <div id="daysOfWeekSection" style="display:@(Model.RecurrenceType == RecurrenceType.Weekly ? "block" : "none")">
                <label class="form-label">曜日</label>
                <div class="d-flex gap-2 flex-wrap">
                    @foreach (var (label, value) in new[] { ("日",0),("月",1),("火",2),("水",3),("木",4),("金",5),("土",6) })
                    {
                        <div class="form-check form-check-inline">
                            <input type="checkbox" name="SelectedDaysOfWeek" value="@value"
                                   class="form-check-input"
                                   @(Model.SelectedDaysOfWeek.Contains(value) ? "checked" : "") />
                            <label class="form-check-label">@label</label>
                        </div>
                    }
                </div>
            </div>
        </div>

        <!-- 参加者-->}}
        @if (Model.UserList.Count > 0)
        {
            <div class="mb-3">
                <label class="form-label">参加者を招待</label>
                <select name="ParticipantUserIds" class="form-select" multiple size="4">
                    @foreach (var u in Model.UserList)
                    {
                        <option value="@u.Value"
                                @(Model.ParticipantUserIds.Contains(u.Value) ? "selected" : "")>
                            @u.Text
                        </option>
                    }
                </select>
                <div class="form-text">Ctrl（Mac: ⌘）を押しながらクリックで複数選択できます。</div>
            </div>
        }
    </div>

    <div class="modal-footer">
        <button type="submit" class="btn btn-primary">保存</button>
        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">キャンセル</button>
    </div>
</form>

<script>
    // 繰り返し種別変更時に関連オプションの表示を切り替える
    document.getElementById('recurrenceTypeSelect')?.addEventListener('change', function() {
        var val = parseInt(this.value);
        document.getElementById('recurrenceOptions').style.display = val !== 0 ? 'block' : 'none';
        document.getElementById('daysOfWeekSection').style.display = val === 2 ? 'block' : 'none';
    });
    // エラーメッセージ div の表示/非表示制御
    document.getElementById('scheduleForm')?.addEventListener('submit', function() {
        document.getElementById('formErrors').classList.add('d-none');
    });
</script>
```

- [ ] **Step 4: ビルドして確認**

```bash
cd DevNext && dotnet build
```
Expected: Build succeeded, 0 errors

- [ ] **Step 5: コミット**

```bash
git add DevNext/Views/Schedule/
git commit -m "feat: スケジュール View を追加（FullCalendar + Bootstrap モーダル）"
```

---

## Task 11: DI 登録・ナビゲーション・ホーム画面

**Files:**
- Modify: `DevNext/Program.cs`
- Modify: `DevNext/Views/Shared/_Layout.cshtml`
- Modify: `DevNext/Views/Home/Index.cshtml`

- [ ] **Step 1: Program.cs に ScheduleService を登録する**

`Program.cs` の `ExportService` 登録行の後に追記する（行番号はファイル末尾付近）：

```csharp
// スケジュール
builder.Services.AddScoped<Site.Service.ScheduleService>();
```

- [ ] **Step 2: _Layout.cshtml にナビリンクを追加する**

`_Layout.cshtml` の承認申請リンク（`ApprovalRequest`）の後に追記する：

```html
                            <a class="nav-link text-white" asp-controller="Schedule" asp-action="Index">スケジュール</a>
```

- [ ] **Step 3: Home/Index.cshtml にカードを追加する**

ホーム画面の承認申請カードの後に追記する：

```html
        <div class="card h-100">
            <div class="card-body">
                <h5 class="card-title"><i class="fas fa-calendar-alt me-2"></i>スケジュール</h5>
                <p class="card-text">個人・共有の予定管理と繰り返しスケジュール、参加者招待機能のサンプルです。</p>
                <a asp-controller="Schedule" asp-action="Index" class="btn btn-primary">カレンダーへ</a>
            </div>
        </div>
```

- [ ] **Step 4: ビルドして確認**

```bash
cd DevNext && dotnet build
```
Expected: Build succeeded, 0 errors

- [ ] **Step 5: コミット**

```bash
git add DevNext/Program.cs DevNext/Views/Shared/_Layout.cshtml DevNext/Views/Home/Index.cshtml
git commit -m "feat: スケジュール機能を DI 登録・ナビゲーション・ホーム画面に追加"
```

---

## Task 12: 動作確認・最終調整

- [ ] **Step 1: 開発サーバーを起動する**

```bash
cd DevNext && dotnet run
```

- [ ] **Step 2: Playwright MCP で動作確認する（全スプリントコントラクト条件を検証）**

確認する項目：
1. `/Schedule/Index` が表示される（FullCalendar が描画される）
2. 日付クリックで作成モーダルが開く
3. 予定を作成できる（個人・共有それぞれ）
4. 作成した予定がカレンダー上に色分けで表示される
5. 予定クリックで詳細モーダルが開く
6. 作成者のみ編集・削除ボタンが表示される
7. 繰り返し予定（Daily / Weekly / Monthly）が正しく展開される
8. 他ユーザーでログインして共有予定が見え、個人予定が見えないことを確認
9. 参加者招待と承諾・辞退ステータス変更が動作する

- [ ] **Step 3: 全テストを実行して確認**

```bash
cd Tests && dotnet test -v
```
Expected: 全テスト PASSED

- [ ] **Step 4: 最終コミット**

```bash
git add -A
git commit -m "feat: スケジュール・カレンダー機能を実装完了"
```

---

## 実行後のチェックリスト

- [ ] `/add-page` スキルを実行してホーム・ナビバー・ドキュメントを確認
- [ ] Evaluator レビューを実施（`superpowers:code-reviewer`）
- [ ] `doc/基本設計書.md` / `doc/詳細設計書.md` / `doc/画面設計書.md` を更新
- [ ] `/export-docs` で Office ファイルを再生成
