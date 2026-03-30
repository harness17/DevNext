using System.ComponentModel.DataAnnotations;

namespace WizardSample.Common
{
    /// <summary>ページ読み込み種別（ページング・ソート・件数変更・再検索の判定に使用）</summary>
    public enum PageRead
    {
        Paging,
        Sorting,
        ChangeRecordNum,
        Resarch,
    }

    /// <summary>多段階フォームサンプルのカテゴリ</summary>
    public enum WizardCategory
    {
        [Display(Name = "お問い合わせ")]
        Inquiry = 1,
        [Display(Name = "ご要望")]
        Request = 2,
        [Display(Name = "不具合報告")]
        BugReport = 3,
        [Display(Name = "その他")]
        Other = 4,
    }
}
