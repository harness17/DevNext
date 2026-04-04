using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using PdfSample.Data;
using PdfSample.Entity;

namespace PdfSample.Repository;

public class InvoiceItemRepository : RepositoryBase<InvoiceItemEntity, InvoiceItemEntityHistory, InvoiceItemCondModel>
{
    public InvoiceItemRepository(PdfSampleDbContext context) : base(context) { }

    public List<InvoiceItemEntity> GetByInvoiceId(long invoiceId)
    {
        return dbSet.Where(x => !x.DelFlag && x.InvoiceId == invoiceId).OrderBy(x => x.Id).ToList();
    }

    public override IQueryable<InvoiceItemEntity> GetBaseQuery(InvoiceItemCondModel? cond = null, bool includeDelete = false)
    {
        IQueryable<InvoiceItemEntity> query = dbSet.Where(x => includeDelete || !x.DelFlag);

        if (cond != null)
        {
            if (cond.Id != null) query = query.Where(x => x.Id == cond.Id);
            if (cond.InvoiceId != null) query = query.Where(x => x.InvoiceId == cond.InvoiceId);
        }

        return query;
    }
}

public class InvoiceItemCondModel : IRepositoryCondModel
{
    public long? Id { get; set; }
    public long? InvoiceId { get; set; }
    public CommonListPagerModel Pager { get; set; } = new(1, "Id", "ASC", 100);
}
