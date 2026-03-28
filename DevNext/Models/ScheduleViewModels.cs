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
