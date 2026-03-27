using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using Site.Common;
using Site.Repository;
using System.ComponentModel.DataAnnotations;

namespace Site.Models
{
    // ─── 一覧ページ用 ─────────────────────────────────────────────────────────

    /// <summary>申請一覧ページの ViewModel</summary>
    public class ApprovalRequestIndexViewModel : SearchModelBase
    {
        public ApprovalRequestCondViewModel? Cond { get; set; }
        public ApprovalRequestListData? RowData { get; set; }
    }

    /// <summary>申請一覧の検索条件（View 用）</summary>
    public class ApprovalRequestCondViewModel
    {
        [Display(Name = "タイトル")]
        public string? Title { get; set; }

        [Display(Name = "状態")]
        public string? Status { get; set; }

        /// <summary>状態ドロップダウン用の選択肢リスト</summary>
        public List<SelectListItem> StatusList { get; set; } = new();
    }

    // ─── 作成・編集フォーム用 ──────────────────────────────────────────────────

    /// <summary>申請の作成・編集フォーム用 ViewModel</summary>
    public class ApprovalRequestFormViewModel
    {
        public long Id { get; set; }

        [Display(Name = "タイトル")]
        [Required(ErrorMessage = "タイトルは必須です")]
        [MaxLength(200, ErrorMessage = "200文字以内で入力してください")]
        public string Title { get; set; } = "";

        [Display(Name = "申請内容")]
        [Required(ErrorMessage = "申請内容は必須です")]
        [MaxLength(2000, ErrorMessage = "2000文字以内で入力してください")]
        public string Content { get; set; } = "";

        /// <summary>申請ボタン押下時 true。下書き保存は false。</summary>
        public bool SubmitRequest { get; set; } = false;
    }

    // ─── 詳細・承認操作用 ──────────────────────────────────────────────────────

    /// <summary>申請詳細・承認/却下操作用 ViewModel</summary>
    public class ApprovalRequestDetailViewModel
    {
        public long Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public ApprovalStatus Status { get; set; }
        public string RequesterUserId { get; set; } = "";
        public string RequesterName { get; set; } = "";
        public string? ApproverComment { get; set; }
        public DateTime? RequestedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime CreateDate { get; set; }

        // 承認・却下フォーム用
        [Display(Name = "コメント")]
        [MaxLength(1000, ErrorMessage = "1000文字以内で入力してください")]
        public string? ActionComment { get; set; }
    }

    // ─── 削除確認用 ────────────────────────────────────────────────────────────

    /// <summary>削除確認ページ用 ViewModel</summary>
    public class ApprovalRequestDeleteViewModel
    {
        public long Id { get; set; }
        public string Title { get; set; } = "";
        public ApprovalStatus Status { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
