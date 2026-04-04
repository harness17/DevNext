using Dev.CommonLibrary.Entity;
using PdfSample.Common;
using System.ComponentModel.DataAnnotations;

namespace PdfSample.Entity;

public class InvoiceEntity : InvoiceEntityBase
{
    public List<InvoiceItemEntity> Items { get; set; } = [];
}

public class InvoiceEntityHistory : InvoiceEntityBase, IEntityHistory
{
    [Key]
    public long HistoryId { get; set; }
}

public abstract class InvoiceEntityBase : SiteEntityBase
{
    [Required]
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = "";

    [Required]
    [MaxLength(200)]
    public string ClientName { get; set; } = "";

    public DateTime IssueDate { get; set; }

    public DateTime DueDate { get; set; }

    public InvoiceStatus Status { get; set; }

    public string? Notes { get; set; }
}

public class InvoiceItemEntity : InvoiceItemEntityBase { }

public class InvoiceItemEntityHistory : InvoiceItemEntityBase, IEntityHistory
{
    [Key]
    public long HistoryId { get; set; }
}

public abstract class InvoiceItemEntityBase : SiteEntityBase
{
    public long InvoiceId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = "";

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}
