using System.ComponentModel.DataAnnotations;

namespace Site.Models
{
    /// <summary>アカウント管理トップ画面用 ViewModel。現在の設定状態を表示する。</summary>
    public class IndexViewModel
    {
        public bool HasPassword { get; set; }
        public string? PhoneNumber { get; set; }
        public bool TwoFactor { get; set; }
        public bool BrowserRemembered { get; set; }
    }

    /// <summary>外部ログイン連携管理画面用 ViewModel。</summary>
    public class ManageLoginsViewModel
    {
        public IList<Microsoft.AspNetCore.Authentication.AuthenticationScheme>? OtherLogins { get; set; }
    }

    /// <summary>パスワード未設定ユーザー向けの初回パスワード設定フォーム用 ViewModel。</summary>
    public class SetPasswordViewModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "{0} の長さは {2} 文字以上である必要があります。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "新しいパスワード")]
        public string NewPassword { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "新しいパスワードの確認入力")]
        [Compare("NewPassword", ErrorMessage = "新しいパスワードと確認のパスワードが一致しません。")]
        public string ConfirmPassword { get; set; } = "";
    }

    /// <summary>パスワード変更フォーム用 ViewModel。現在のパスワードと新しいパスワードを受け取る。</summary>
    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "現在のパスワード")]
        public string OldPassword { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "{0} の長さは {2} 文字以上である必要があります。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "新しいパスワード")]
        public string NewPassword { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "新しいパスワードの確認入力")]
        [Compare("NewPassword", ErrorMessage = "新しいパスワードと確認のパスワードが一致しません。")]
        public string ConfirmPassword { get; set; } = "";
    }
}
