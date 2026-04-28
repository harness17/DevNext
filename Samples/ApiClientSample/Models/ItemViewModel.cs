namespace ApiClientSample.Models;

/// <summary>ApiSample の /api/items レスポンスに対応するモデル</summary>
public class ItemViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
