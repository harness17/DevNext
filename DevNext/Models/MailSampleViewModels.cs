using System.ComponentModel.DataAnnotations;

namespace Site.Models
{
    /// <summary>
    /// メール送信フォームViewModel
    /// </summary>
    public class MailSampleViewModel
    {
        /// <summary>お名前</summary>
        [Required(ErrorMessage = "お名前を入力してください")]
        [MaxLength(100)]
        [Display(Name = "お名前")]
        public string Name { get; set; } = "";

        /// <summary>メールアドレス（送信先）</summary>
        [Required(ErrorMessage = "メールアドレスを入力してください")]
        [EmailAddress(ErrorMessage = "メールアドレスの形式が正しくありません")]
        [MaxLength(256)]
        [Display(Name = "メールアドレス")]
        public string Email { get; set; } = "";

        /// <summary>件名</summary>
        [Required(ErrorMessage = "件名を入力してください")]
        [MaxLength(200)]
        [Display(Name = "件名")]
        public string Subject { get; set; } = "";

        /// <summary>お問い合わせ内容</summary>
        [Required(ErrorMessage = "お問い合わせ内容を入力してください")]
        [MaxLength(2000)]
        [Display(Name = "お問い合わせ内容")]
        public string Body { get; set; } = "";
    }
}
