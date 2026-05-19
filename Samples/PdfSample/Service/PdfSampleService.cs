using Dev.CommonLibrary.Common;
using PdfSample.Common;
using PdfSample.Data;
using PdfSample.Entity;
using PdfSample.Models;
using PdfSample.Repository;
using System.Security.Claims;

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
