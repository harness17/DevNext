using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using WizardSample.Common;
using WizardSample.Entity;
using System.ComponentModel.DataAnnotations;

namespace WizardSample.Models
{
    // ─────────────────────────────────────────────────────────────────
    // TempData で保持するウィザードセッション全体モデル
    // 各ステップ POST 後に JSON シリアライズして TempData に保存する
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// ウィザード全ステップのデータを保持するセッションモデル
    /// </summary>
    public class WizardSessionModel
    {
        // Step 1
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }

        // Step 2
        public string Subject { get; set; } = "";
        public string Content { get; set; } = "";
        public WizardCategory Category { get; set; }
        public DateTime? DesiredDate { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────
    // 各ステップの入力 ViewModel（バリデーションを各ステップに限定する）
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Step 1: 基本情報 ViewModel
    /// </summary>
    public class WizardStep1ViewModel
    {
        [Required(ErrorMessage = "氏名を入力してください")]
        [MaxLength(100)]
        [Display(Name = "氏名")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "メールアドレスを入力してください")]
        [EmailAddress(ErrorMessage = "メールアドレスの形式が正しくありません")]
        [MaxLength(256)]
        [Display(Name = "メールアドレス")]
        public string Email { get; set; } = "";

        [MaxLength(20)]
        [Phone(ErrorMessage = "電話番号の形式が正しくありません")]
        [Display(Name = "電話番号")]
        public string? Phone { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────
    // 一覧ページ用 ViewModel
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// ウィザード登録データ一覧 ViewModel（ページング・ソート対応）
    /// </summary>
    public class WizardSampleListViewModel : SearchModelBase
    {
        public WizardSampleDataViewModel RowData { get; set; } = new();
        public WizardSampleCondViewModel Cond { get; set; } = new();
        /// <summary>表示件数ドロップダウン用リスト</summary>
        public IEnumerable<SelectListItem> RecordNumberList { get; } = LocalUtil.SetRecoedNumberList();
    }

    /// <summary>
    /// ウィザード一覧の検索条件 ViewModel
    /// </summary>
    public class WizardSampleCondViewModel : SearchCondModelBase
    {
        [Display(Name = "氏名")]
        public string? Name { get; set; }

        [Display(Name = "メールアドレス")]
        public string? Email { get; set; }

        [Display(Name = "カテゴリ")]
        public WizardCategory? Category { get; set; }

        /// <summary>カテゴリドロップダウン用リスト（View でのみ使用）</summary>
        public IEnumerable<SelectListItem> CategoryList { get; set; } =
            SelectListUtility.GetEnumSelectListItem<WizardCategory>();
    }

    /// <summary>
    /// ウィザード一覧データ ViewModel
    /// </summary>
    public class WizardSampleDataViewModel
    {
        public List<WizardEntity> rows { get; set; } = new();
        public CommonListSummaryModel? Summary { get; set; }
    }

    /// <summary>
    /// Step 2: 詳細情報 ViewModel
    /// </summary>
    public class WizardStep2ViewModel
    {
        [Required(ErrorMessage = "件名を入力してください")]
        [MaxLength(200)]
        [Display(Name = "件名")]
        public string Subject { get; set; } = "";

        [Required(ErrorMessage = "内容を入力してください")]
        [MaxLength(2000)]
        [Display(Name = "内容")]
        public string Content { get; set; } = "";

        [Display(Name = "カテゴリ")]
        public WizardCategory Category { get; set; }

        /// <summary>SelectList は View でのみ使用する（Bind 対象外）</summary>
        public IEnumerable<SelectListItem> CategoryList { get; set; } =
            SelectListUtility.GetEnumSelectListItem<WizardCategory>();

        [Display(Name = "希望対応日")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? DesiredDate { get; set; }
    }
}
