using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Site.Models
{
    /// <summary>外部ログイン確認フォーム用 ViewModel。</summary>
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [Display(Name = "電子メール")]
        public string Email { get; set; } = "";
    }

    /// <summary>外部ログインプロバイダー一覧表示用 ViewModel。</summary>
    public class ExternalLoginListViewModel
    {
        public string? ReturnUrl { get; set; }
    }

    /// <summary>二要素認証コード送信先プロバイダー選択用 ViewModel。</summary>
    public class SendCodeViewModel
    {
        public string? SelectedProvider { get; set; }
        public ICollection<SelectListItem>? Providers { get; set; }
        public string? ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
    }

    /// <summary>二要素認証コード検証用 ViewModel。</summary>
    public class VerifyCodeViewModel
    {
        [Required]
        public string Provider { get; set; } = "";

        [Required]
        [Display(Name = "コード")]
        public string Code { get; set; } = "";
        public string? ReturnUrl { get; set; }

        [Display(Name = "認証情報をこのブラウザーに保存しますか?")]
        public bool RememberBrowser { get; set; }
        public bool RememberMe { get; set; }
    }

    /// <summary>ログインフォーム用 ViewModel。</summary>
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "電子メール")]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "パスワード")]
        public string Password { get; set; } = "";

        [Display(Name = "このアカウントを記憶する")]
        public bool RememberMe { get; set; }
    }

    /// <summary>新規ユーザー登録フォーム用 ViewModel。</summary>
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "電子メール")]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "{0} の長さは {2} 文字以上である必要があります。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "パスワード")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "パスワードの確認入力")]
        [Compare("Password", ErrorMessage = "パスワードと確認のパスワードが一致しません。")]
        public string ConfirmPassword { get; set; } = "";
    }

    /// <summary>パスワードリセット実行フォーム用 ViewModel。</summary>
    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "電子メール")]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "{0} の長さは {2} 文字以上である必要があります。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "パスワード")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "パスワードの確認入力")]
        [Compare("Password", ErrorMessage = "パスワードと確認のパスワードが一致しません。")]
        public string ConfirmPassword { get; set; } = "";

        public string? Code { get; set; }
    }

    /// <summary>パスワードリセットメール送信依頼フォーム用 ViewModel。</summary>
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "電子メール")]
        public string Email { get; set; } = "";
    }
}
