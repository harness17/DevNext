using Dev.CommonLibrary.Entity;
using System.ComponentModel.DataAnnotations;

namespace Site.Entity
{
    /// <summary>
    /// 通知エンティティ。承認ワークフローのイベント（申請・承認・却下）に連動して生成される。
    /// </summary>
    public class NotificationEntity : SiteEntityBase
    {
        /// <summary>通知先ユーザーID（ApplicationUser.Id）</summary>
        [Required]
        [MaxLength(450)]
        public string RecipientUserId { get; set; } = "";

        /// <summary>通知メッセージ本文</summary>
        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = "";

        /// <summary>既読フラグ（false: 未読、true: 既読）</summary>
        public bool IsRead { get; set; } = false;

        /// <summary>クリック時の遷移先URL（Detail ページなど）</summary>
        [MaxLength(500)]
        public string? RelatedUrl { get; set; }
    }
}
