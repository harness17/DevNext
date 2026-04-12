using System.ComponentModel.DataAnnotations.Schema;
using Dev.CommonLibrary.Entity;
using Microsoft.EntityFrameworkCore;

namespace ApiSample.Entity;

/// <summary>
/// API サンプル用の商品エンティティ。
/// SiteEntityBase を継承して Id・監査カラムを統一する。
/// </summary>
public class ApiItem : SiteEntityBase
{
    /// <summary>商品名（必須）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>説明</summary>
    public string? Description { get; set; }

    /// <summary>価格（0以上）。SQL Server の decimal(18,2) に対応</summary>
    [Precision(18, 2)]
    public decimal Price { get; set; }

    /// <summary>在庫数</summary>
    public int Stock { get; set; }
}
