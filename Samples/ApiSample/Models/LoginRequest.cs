using System.ComponentModel.DataAnnotations;

namespace ApiSample.Models;

/// <summary>ログイン（JWT 発行）リクエストモデル</summary>
public class LoginRequest
{
    /// <summary>メールアドレス</summary>
    [Required(ErrorMessage = "メールアドレスは必須です。")]
    [EmailAddress(ErrorMessage = "正しいメールアドレス形式で入力してください。")]
    public string Email { get; set; } = string.Empty;

    /// <summary>パスワード</summary>
    [Required(ErrorMessage = "パスワードは必須です。")]
    public string Password { get; set; } = string.Empty;
}
