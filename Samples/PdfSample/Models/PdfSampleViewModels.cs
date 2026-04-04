using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using PdfSample.Common;
using PdfSample.Entity;
using System.ComponentModel.DataAnnotations;

namespace PdfSample.Models;

public class LoginViewModel
{
    [Required]
    [Display(Name = "電子メール")]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "パスワード")]
    public string Password { get; set; } = "";

    [Display(Name = "このアカウントを記憶する")]
    public bool RememberMe { get; set; }
}

public class InvoiceViewModel : SearchModelBase
{
    public InvoiceDataViewModel RowData { get; set; } = new();
    public InvoiceCondViewModel Cond { get; set; } = new();
    public IEnumerable<SelectListItem> RecordNumberList { get; } = LocalUtil.SetRecordNumberList();
    public List<long> SelectedIds { get; set; } = [];
}

public class InvoiceDataViewModel
{
    public List<InvoiceEntity> Rows { get; set; } = [];
    public CommonListSummaryModel? Summary { get; set; }
}

public class InvoiceCondViewModel : SearchCondModelBase
{
    [Display(Name = "取引先名")]
    [MaxLength(200)]
    public string? ClientName { get; set; }

    [Display(Name = "ステータス")]
    public InvoiceStatus? Status { get; set; }

    public List<SelectListItem> StatusList { get; set; } = SelectListUtility.GetEnumSelectListItem<InvoiceStatus>().ToList();
}

public class InvoiceDetailViewModel
{
    public long? Id { get; set; }

    [Required]
    [Display(Name = "請求書番号")]
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = "";

    [Required]
    [Display(Name = "取引先名")]
    [MaxLength(200)]
    public string ClientName { get; set; } = "";

    [Display(Name = "発行日")]
    [DataType(DataType.Date)]
    public DateTime IssueDate { get; set; } = DateTime.Today;

    [Display(Name = "支払期限")]
    [DataType(DataType.Date)]
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

    [Display(Name = "ステータス")]
    public InvoiceStatus Status { get; set; }

    public List<SelectListItem> StatusList { get; set; } = SelectListUtility.GetEnumSelectListItem<InvoiceStatus>().ToList();

    [Display(Name = "備考")]
    public string? Notes { get; set; }

    public List<InvoiceItemViewModel> Items { get; set; } = [new InvoiceItemViewModel()];
}

public class InvoiceItemViewModel
{
    public long? Id { get; set; }

    [Required]
    [Display(Name = "品目")]
    [MaxLength(200)]
    public string Description { get; set; } = "";

    [Display(Name = "数量")]
    [Range(1, int.MaxValue, ErrorMessage = "数量は1以上で入力してください。")]
    public int Quantity { get; set; } = 1;

    [Display(Name = "単価")]
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "単価は0以上で入力してください。")]
    public decimal UnitPrice { get; set; }

    public decimal SubTotal => Quantity * UnitPrice;
}
