namespace ApiSample.Models;

/// <summary>ログイン成功レスポンスモデル</summary>
public class LoginResponse
{
    /// <summary>JWT アクセストークン</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>トークン有効期限（UTC）</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>ログインユーザーのメールアドレス</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>ロール一覧</summary>
    public IList<string> Roles { get; set; } = [];
}
