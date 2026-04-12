using System.ComponentModel.DataAnnotations;

namespace ApiSample.Models;

/// <summary>商品登録・更新リクエストモデル</summary>
public class ApiItemRequest
{
    /// <summary>商品名（必須・最大100文字）</summary>
    [Required(ErrorMessage = "商品名は必須です。")]
    [MaxLength(100, ErrorMessage = "商品名は100文字以内で入力してください。")]
    public string Name { get; set; } = string.Empty;

    /// <summary>説明（最大500文字）</summary>
    [MaxLength(500, ErrorMessage = "説明は500文字以内で入力してください。")]
    public string? Description { get; set; }

    /// <summary>価格（0以上）</summary>
    [Range(0, double.MaxValue, ErrorMessage = "価格は0以上で入力してください。")]
    public decimal Price { get; set; }

    /// <summary>在庫数（0以上）</summary>
    [Range(0, int.MaxValue, ErrorMessage = "在庫数は0以上で入力してください。")]
    public int Stock { get; set; }
}
