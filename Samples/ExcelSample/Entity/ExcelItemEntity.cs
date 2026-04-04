using Dev.CommonLibrary.Entity;
using System.ComponentModel.DataAnnotations;

namespace ExcelSample.Entity
{
    public class ExcelItemEntity : ExcelItemEntityBase { }

    public class ExcelItemEntityHistory : ExcelItemEntityBase, IEntityHistory
    {
        [Key]
        public long HistoryId { get; set; }
    }

    /// <summary>
    /// Excel インポート/エクスポート サンプル用商品エンティティ
    /// </summary>
    public abstract class ExcelItemEntityBase : SiteEntityBase
    {
        /// <summary>商品名</summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        /// <summary>カテゴリ</summary>
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "";

        /// <summary>単価（円）</summary>
        public int Price { get; set; }

        /// <summary>在庫数</summary>
        public int Quantity { get; set; }
    }
}
