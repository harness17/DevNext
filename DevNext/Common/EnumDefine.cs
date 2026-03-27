using Dev.CommonLibrary.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Site.Common
{
    public enum LanguageMin
    {
        ja = 1,
        en = 2
    }

    public enum LanguageCode
    {
        [Display(Name = "日本語", Order = 1)]
        jaJP = 1,
        [Display(Name = "English", Order = 2)]
        enUS = 2,
    }

    public enum ErrorType
    {
        [Display(Name = "システムエラー")]
        syserror = 1,
        [Display(Name = "不正なURLエラー")]
        urlerror = 2,
        [Display(Name = "不正な操作")]
        usererror = 3,
        [Display(Name = "セッションタイムアウト")]
        sessionerror = 4,
        [Display(Name = "使用できない機能")]
        cannotuseerror = 5,
    }

    public enum PageRead
    {
        Resarch,
        Paging,
        Sorting,
        ChangeRecordNum
    }

    public enum CustomRouteData
    {
        RouteSampleID,
    }

    public enum ApplicationRoleType
    {
        [SubValue("1")]
        [Display(Name = "管理者", Order = 1)]
        Admin = 1,
        [SubValue("2")]
        [Display(Name = "運営者", Order = 2)]
        Member = 2,
    }

    public enum SampleEnum
    {
        [Display(Name = "選択肢1")]
        select1 = 0,
        [Display(Name = "選択肢2")]
        select2 = 2,
        [Display(Name = "選択肢3")]
        select3 = 3
    }

    public enum SampleEnum2
    {
        [Display(Name = "選択肢21")]
        select21 = 0,
        [Display(Name = "選択肢22")]
        select22 = 2,
        [Display(Name = "選択肢23")]
        select23 = 3
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

    /// <summary>承認申請の状態</summary>
    public enum ApprovalStatus
    {
        [Display(Name = "下書き")]
        Draft = 1,
        [Display(Name = "申請中")]
        Pending = 2,
        [Display(Name = "承認済み")]
        Approved = 3,
        [Display(Name = "却下")]
        Rejected = 4,
    }
}
