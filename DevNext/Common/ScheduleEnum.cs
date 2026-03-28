using System.ComponentModel.DataAnnotations;

namespace Site.Common
{
    /// <summary>
    /// 繰り返し種別
    /// </summary>
    public enum RecurrenceType
    {
        /// <summary>繰り返しなし</summary>
        [Display(Name = "繰り返しなし")]
        None = 0,
        /// <summary>毎日</summary>
        [Display(Name = "毎日")]
        Daily = 1,
        /// <summary>毎週（RecurrenceDaysOfWeek で曜日指定）</summary>
        [Display(Name = "毎週")]
        Weekly = 2,
        /// <summary>毎月（開始日と同じ日）</summary>
        [Display(Name = "毎月")]
        Monthly = 3
    }

    /// <summary>
    /// 参加者ステータス
    /// </summary>
    public enum ParticipantStatus
    {
        /// <summary>招待済み（未回答）</summary>
        [Display(Name = "未回答")]
        Invited = 0,
        /// <summary>承諾</summary>
        [Display(Name = "承諾")]
        Accepted = 1,
        /// <summary>辞退</summary>
        [Display(Name = "辞退")]
        Declined = 2
    }
}
