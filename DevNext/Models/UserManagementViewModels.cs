using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using Site.Common;
using System.ComponentModel.DataAnnotations;

namespace Site.Models
{
    /// <summary>ユーザー管理一覧ページの ViewModel。</summary>
    // ポイント: SearchModelBase を継承することでページング・ソートに必要な
    //           Page / Sort / SortDir / RecordNum / PageRead プロパティを持つ
    public class UserManagementViewModel : SearchModelBase
    {
        public UserManagementDataViewModel RowData { get; set; } = new();
        public UserManagementCondViewModel Cond { get; set; } = new();
        // ポイント: 表示件数ドロップダウン用リストを共通ユーティリティで生成
        public IEnumerable<SelectListItem> RecoedNumberList { get; } = LocalUtil.SetRecoedNumberList();
    }

    /// <summary>ユーザー管理一覧の検索条件 ViewModel。</summary>
    // ポイント: SearchCondModelBase を継承して検索条件 + Pager 情報を持つ
    //           TempData に JSON シリアライズして保存することでページング時に条件を維持する
    public class UserManagementCondViewModel : SearchCondModelBase
    {
        [Display(Name = "ユーザー名")]
        [MaxLength(256)]
        public string? UserName { get; set; }

        [Display(Name = "メールアドレス")]
        [MaxLength(256)]
        public string? Email { get; set; }
    }

    /// <summary>ユーザー一覧データ（行リスト + ページ概要）を保持する ViewModel。</summary>
    public class UserManagementDataViewModel
    {
        public List<UserManagementListItemViewModel> rows { get; set; } = new();
        public CommonListSummaryModel? Summary { get; set; }
    }

    /// <summary>
    /// ユーザー一覧の1行分データ
    /// </summary>
    public class UserManagementListItemViewModel
    {
        public string Id { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool LockoutEnabled { get; set; }
        // ポイント: LockoutEnd はロックアウト解除日時。現在時刻と比較してロック中かどうかを判定する
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool EmailConfirmed { get; set; }
        // ポイント: ロールは Identity から非同期で取得するためサービス側で詰める
        public List<string> Roles { get; set; } = new();
    }

    /// <summary>
    /// ユーザー編集フォーム用 ViewModel（編集・削除確認画面で共用）
    /// </summary>
    public class UserManagementEditViewModel
    {
        [Required]
        public string Id { get; set; } = "";

        [Required]
        [Display(Name = "ユーザー名")]
        [MaxLength(256)]
        public string UserName { get; set; } = "";

        [Required]
        [EmailAddress]
        [Display(Name = "メールアドレス")]
        [MaxLength(256)]
        public string Email { get; set; } = "";

        [Display(Name = "メール確認済み")]
        public bool EmailConfirmed { get; set; }

        // ポイント: 選択中のロール名一覧（チェックボックスでバインドする）
        [Display(Name = "ロール")]
        public List<string> RoleNames { get; set; } = new();

        // ポイント: 全ロール一覧（チェックボックス選択肢の生成に使用）
        public List<SelectListItem> AvailableRoles { get; set; } = new();
    }
}
