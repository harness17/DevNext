using System.ComponentModel.DataAnnotations;

namespace WizardSample.Models
{
    /// <summary>ログイン ViewModel</summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "メールアドレスを入力してください")]
        [EmailAddress]
        [Display(Name = "メールアドレス")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "パスワードを入力してください")]
        [DataType(DataType.Password)]
        [Display(Name = "パスワード")]
        public string? Password { get; set; }

        [Display(Name = "ログイン状態を保持する")]
        public bool RememberMe { get; set; }
    }
}
