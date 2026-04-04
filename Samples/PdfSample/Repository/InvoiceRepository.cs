using AutoMapper;
using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PdfSample.Common;
using PdfSample.Data;
using PdfSample.Entity;
using PdfSample.Models;

namespace PdfSample.Repository;

public class InvoiceRepository : RepositoryBase<InvoiceEntity, InvoiceEntityHistory, InvoiceCondModel>
{
    public InvoiceRepository(PdfSampleDbContext context) : base(context) { }

    /// <summary>
    /// 検索条件、ソート、ページングを適用した請求書一覧を返す。
    /// </summary>
    public InvoiceDataViewModel GetInvoiceList(InvoiceCondModel cond)
    {
        var model = new InvoiceDataViewModel();
        IQueryable<InvoiceEntity> query = GetBaseQuery(cond);

        cond.Pager.sort = string.IsNullOrEmpty(cond.Pager.sort) ? "Id" : cond.Pager.sort;
        cond.Pager.sortdir = string.IsNullOrEmpty(cond.Pager.sortdir) ? "DESC" : cond.Pager.sortdir;

        if (cond.Pager.sortdir.ToLower() == "desc")
        {
            query = cond.Pager.sort switch
            {
                "InvoiceNumber" => query.OrderByDescending(x => x.InvoiceNumber),
                "ClientName" => query.OrderByDescending(x => x.ClientName),
                "IssueDate" => query.OrderByDescending(x => x.IssueDate),
                "DueDate" => query.OrderByDescending(x => x.DueDate),
                "Status" => query.OrderByDescending(x => x.Status),
                _ => query.OrderByDescending(x => x.Id)
            };
        }
        else
        {
            query = cond.Pager.sort switch
            {
                "InvoiceNumber" => query.OrderBy(x => x.InvoiceNumber),
                "ClientName" => query.OrderBy(x => x.ClientName),
                "IssueDate" => query.OrderBy(x => x.IssueDate),
                "DueDate" => query.OrderBy(x => x.DueDate),
                "Status" => query.OrderBy(x => x.Status),
                _ => query.OrderBy(x => x.Id)
            };
        }

        int totalRecords = query.Count();
        LocalUtil.SetTakeSkip(ref query, cond);
        model.Rows = query.ToList();
        model.Summary = Util.CreateSummary(cond.Pager, totalRecords, "{0}件中 {1} - {2} を表示");
        return model;
    }

    /// <summary>
    /// 明細を含む請求書詳細を取得する。
    /// </summary>
    public InvoiceEntity? GetDetail(long id)
    {
        return dbSet
            .Include(x => x.Items.Where(i => !i.DelFlag))
            .FirstOrDefault(x => x.Id == id && !x.DelFlag);
    }

    /// <summary>
    /// ViewModel の検索条件を Repository 用条件モデルへ変換する。
    /// </summary>
    public override InvoiceCondModel GetCondModel<T>(T viewCondModel, MapperConfiguration? config = null)
    {
        config ??= new MapperConfiguration(cfg => cfg.CreateMap<InvoiceCondViewModel, InvoiceCondModel>(), NullLoggerFactory.Instance);
        return config.CreateMapper().Map<InvoiceCondModel>(viewCondModel);
    }

    /// <summary>
    /// 論理削除を除外した請求書検索クエリを生成する。
    /// </summary>
    public override IQueryable<InvoiceEntity> GetBaseQuery(InvoiceCondModel? cond = null, bool includeDelete = false)
    {
        IQueryable<InvoiceEntity> query = dbSet.Where(x => includeDelete || !x.DelFlag);

        if (cond != null)
        {
            if (cond.Id != null) query = query.Where(x => x.Id == cond.Id);
            if (!string.IsNullOrWhiteSpace(cond.ClientName)) query = query.Where(x => x.ClientName.Contains(cond.ClientName));
            if (cond.Status != null) query = query.Where(x => x.Status == cond.Status);
            if (!string.IsNullOrWhiteSpace(cond.OwnerUserId)) query = query.Where(x => x.CreateApplicationUserId == cond.OwnerUserId);
        }

        return query;
    }
}

public class InvoiceCondModel : IRepositoryCondModel
{
    public long? Id { get; set; }
    public string? ClientName { get; set; }
    public InvoiceStatus? Status { get; set; }
    public string? OwnerUserId { get; set; }
    public CommonListPagerModel Pager { get; set; } = new(1, "Id", "DESC", 10);
}
