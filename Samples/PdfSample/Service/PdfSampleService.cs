using Dev.CommonLibrary.Common;
using PdfSample.Common;
using PdfSample.Data;
using PdfSample.Entity;
using PdfSample.Models;
using PdfSample.Repository;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Security.Claims;
using System.IO.Compression;

namespace PdfSample.Service;

public class PdfSampleService
{
    private readonly PdfSampleDbContext _context;
    private readonly InvoiceRepository _invoiceRepository;
    private readonly InvoiceItemRepository _invoiceItemRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PdfSampleService(
        PdfSampleDbContext context,
        InvoiceRepository invoiceRepository,
        InvoiceItemRepository invoiceItemRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _invoiceRepository = invoiceRepository;
        _invoiceItemRepository = invoiceItemRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// ログインユーザーの権限に応じて請求書一覧を取得する。
    /// </summary>
    public InvoiceViewModel GetInvoiceList(InvoiceViewModel model)
    {
        model.Cond ??= new InvoiceCondViewModel();
        LocalUtil.SetPager(model.Cond, model);
        var cond = _invoiceRepository.GetCondModel(model.Cond);
        if (!IsAdmin())
            cond.OwnerUserId = GetCurrentUserId();
        model.RowData = _invoiceRepository.GetInvoiceList(cond);
        return model;
    }

    /// <summary>
    /// 参照権限を満たす請求書詳細を ViewModel に変換して返す。
    /// </summary>
    public InvoiceDetailViewModel? GetInvoiceDetail(long id)
    {
        var entity = GetAccessibleInvoice(id);
        if (entity == null) return null;

        return new InvoiceDetailViewModel
        {
            Id = entity.Id,
            InvoiceNumber = entity.InvoiceNumber,
            ClientName = entity.ClientName,
            IssueDate = entity.IssueDate,
            DueDate = entity.DueDate,
            Status = entity.Status,
            Notes = entity.Notes,
            Items = entity.Items
                .Where(x => !x.DelFlag)
                .OrderBy(x => x.Id)
                .Select(x => new InvoiceItemViewModel
                {
                    Id = x.Id,
                    Description = x.Description,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice
                }).ToList()
        };
    }

    /// <summary>
    /// 請求書本体と明細行を新規登録する。
    /// </summary>
    public void InsertInvoice(InvoiceDetailViewModel model, string? userName)
    {
        var entity = new InvoiceEntity
        {
            InvoiceNumber = model.InvoiceNumber,
            ClientName = model.ClientName,
            IssueDate = model.IssueDate,
            DueDate = model.DueDate,
            Status = model.Status,
            Notes = model.Notes,
            CreateApplicationUserId = userName,
            UpdateApplicationUserId = userName,
            Items = model.Items
                .Where(IsValidItemRow)
                .Select(x => new InvoiceItemEntity
                {
                    Description = x.Description.Trim(),
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice
                }).ToList()
        };

        entity.SetForCreate();
        foreach (var item in entity.Items)
            item.SetForCreate();

        _invoiceRepository.InsertSimple(entity);
    }

    /// <summary>
    /// 明細行は全削除後に再登録する方針で請求書を更新する。
    /// </summary>
    public void UpdateInvoice(InvoiceDetailViewModel model)
    {
        var entity = GetAccessibleInvoice(model.Id!.Value);
        if (entity == null) return;

        entity.InvoiceNumber = model.InvoiceNumber;
        entity.ClientName = model.ClientName;
        entity.IssueDate = model.IssueDate;
        entity.DueDate = model.DueDate;
        entity.Status = model.Status;
        entity.Notes = model.Notes;
        _invoiceRepository.Update(entity, false);

        var existingItems = _invoiceItemRepository.GetByInvoiceId(entity.Id);
        if (existingItems.Count != 0)
            _invoiceItemRepository.LogicalDeletes(existingItems, false);

        foreach (var item in model.Items.Where(IsValidItemRow))
        {
            var newItem = new InvoiceItemEntity
            {
                InvoiceId = entity.Id,
                Description = item.Description.Trim(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            };
            _invoiceItemRepository.Insert(newItem, false);
        }

        _context.SaveChanges();
    }

    /// <summary>
    /// 請求書と明細行を論理削除する。
    /// </summary>
    public void DeleteInvoice(long id)
    {
        var entity = GetAccessibleInvoice(id);
        if (entity == null) return;

        var items = _invoiceItemRepository.GetByInvoiceId(id);
        if (items.Count != 0)
            _invoiceItemRepository.LogicalDeletes(items, false);

        _invoiceRepository.LogicalDelete(entity, false);
        _context.SaveChanges();
    }

    /// <summary>
    /// 単一請求書を PDF としてメモリ上に生成する。
    /// </summary>
    public MemoryStream? ExportPdf(long id)
    {
        var model = GetInvoiceDetail(id);
        if (model == null) return null;

        var total = model.Items.Sum(x => x.SubTotal);
        var memoryStream = new MemoryStream();

        // 請求書ヘッダー、明細テーブル、合計、備考を1ページにまとめて出力する。
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Noto Sans CJK JP", "Meiryo", "MS Gothic").FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("請求書").FontSize(20).Bold();
                    col.Item().PaddingTop(8).Text($"請求書番号: {model.InvoiceNumber}");
                    col.Item().Text($"取引先名: {model.ClientName}");
                    col.Item().Text($"発行日: {model.IssueDate:yyyy/MM/dd}");
                    col.Item().Text($"支払期限: {model.DueDate:yyyy/MM/dd}");
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(100);
                        });

                        static IContainer HeaderCellStyle(IContainer c) =>
                            c.Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
                        static IContainer CellStyle(IContainer c) =>
                            c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5);

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("品目").Bold();
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("数量").Bold();
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("単価").Bold();
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("小計").Bold();
                        });

                        foreach (var item in model.Items)
                        {
                            table.Cell().Element(CellStyle).Text(item.Description);
                            table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString());
                            table.Cell().Element(CellStyle).AlignRight().Text(item.UnitPrice.ToString("#,0.##"));
                            table.Cell().Element(CellStyle).AlignRight().Text(item.SubTotal.ToString("#,0.##"));
                        }

                        table.Cell().ColumnSpan(3).Element(CellStyle).AlignRight().Text("合計金額").Bold();
                        table.Cell().Element(CellStyle).AlignRight().Text($"{total:#,0.##} 円").Bold();
                    });

                    col.Item().PaddingTop(12).Text("備考").Bold();
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).MinHeight(70)
                        .Text(string.IsNullOrWhiteSpace(model.Notes) ? "なし" : model.Notes);
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                });
            });
        }).GeneratePdf(memoryStream);

        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <summary>
    /// 複数請求書の PDF を ZIP にまとめて出力する。
    /// </summary>
    public MemoryStream ExportPdfBulk(List<long> ids)
    {
        var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var id in ids.Distinct())
            {
                var detail = GetInvoiceDetail(id);
                if (detail == null) continue;

                using var pdfStream = ExportPdf(id);
                if (pdfStream == null) continue;

                var entry = archive.CreateEntry($"請求書_{detail.InvoiceNumber}_{id}.pdf");
                using var entryStream = entry.Open();
                pdfStream.CopyTo(entryStream);
            }
        }

        zipStream.Position = 0;
        return zipStream;
    }

    private static bool IsValidItemRow(InvoiceItemViewModel item)
    {
        return !string.IsNullOrWhiteSpace(item.Description);
    }

    /// <summary>
    /// Admin か所有者のみ取得できる請求書を返す。
    /// </summary>
    private InvoiceEntity? GetAccessibleInvoice(long id)
    {
        var entity = _invoiceRepository.GetDetail(id);
        if (entity == null) return null;

        if (IsAdmin()) return entity;

        return entity.CreateApplicationUserId == GetCurrentUserId() ? entity : null;
    }

    /// <summary>
    /// ログインユーザーの識別子を取得する。
    /// </summary>
    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    /// <summary>
    /// 現在ユーザーが Admin ロールか判定する。
    /// </summary>
    private bool IsAdmin()
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;
    }
}
