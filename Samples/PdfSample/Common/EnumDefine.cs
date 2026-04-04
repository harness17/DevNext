using System.ComponentModel.DataAnnotations;

namespace PdfSample.Common;

public enum PageRead
{
    Research,
    Paging,
    Sorting,
    ChangeRecordNum
}

public enum InvoiceStatus
{
    [Display(Name = "下書き")]
    Draft = 0,

    [Display(Name = "発行済")]
    Issued = 1,

    [Display(Name = "支払済")]
    Paid = 2,
}
