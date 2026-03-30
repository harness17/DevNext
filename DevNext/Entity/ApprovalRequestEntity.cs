using Dev.CommonLibrary.Entity;
using Site.Common;
using System.ComponentModel.DataAnnotations;

namespace Site.Entity
{
    /// <summary>
    /// 承認申請エンティティ
    /// 申請者が作成し、承認者（Admin）が承認・却下する。
    /// </summary>
    public class ApprovalRequestEntity : SiteEntityBase
    {
        /// <summary>申請タイトル</summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = "";

        /// <summary>申請内容</summary>
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = "";

        /// <summary>申請状態</summary>
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Draft;

        /// <summary>申請者のユーザーID（ApplicationUser.Id）</summary>
        [Required]
        [MaxLength(450)]
        public string RequesterUserId { get; set; } = "";

        /// <summary>承認者コメント（承認・却下時に入力）</summary>
        [MaxLength(1000)]
        public string? ApproverComment { get; set; }

        /// <summary>申請（Pending 移行）日時</summary>
        public DateTime? RequestedDate { get; set; }

        /// <summary>承認・却下が確定した日時</summary>
        public DateTime? ApprovedDate { get; set; }
    }
}
