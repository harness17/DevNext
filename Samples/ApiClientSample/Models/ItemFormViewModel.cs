using System.ComponentModel.DataAnnotations;

namespace ApiClientSample.Models;

/// <summary>商品登録・更新フォーム用モデル</summary>
public class ItemFormViewModel
{
    [Required(ErrorMessage = "商品名は必須です。")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "価格は必須です。")]
    [Range(0, 9999999, ErrorMessage = "価格は0以上で入力してください。")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "在庫数は必須です。")]
    [Range(0, int.MaxValue, ErrorMessage = "在庫数は0以上で入力してください。")]
    public int Stock { get; set; }
}
