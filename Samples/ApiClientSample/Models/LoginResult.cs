namespace ApiClientSample.Models;

/// <summary>ログイン成功時に返るトークンとロール情報</summary>
public record LoginResult(string Token, List<string> Roles)
{
    public bool IsAdmin => Roles.Contains("Admin");
}
