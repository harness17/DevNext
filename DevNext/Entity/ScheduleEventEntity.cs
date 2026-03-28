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
