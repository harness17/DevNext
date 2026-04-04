using ExcelSample.Data;
using ExcelSample.Entity;
using Dev.CommonLibrary.Repository;

namespace ExcelSample.Repository
{
    // ポイント: RepositoryBase<TEntity, THistory, TCond> を継承することで
    //           Insert / Update / LogicalDelete / SelectById / BatchInsert 等の共通CRUD操作を継承する
    public class ExcelItemRepository : RepositoryBase<ExcelItemEntity, ExcelItemEntityHistory, ExcelItemCondModel>
    {
        private readonly ExcelSampleDbContext _context;

        public ExcelItemRepository(ExcelSampleDbContext context) : base(context)
        {
            _context = context;
        }

        // ポイント: GetBaseQuery は論理削除フィルタ + 条件を IQueryable として返す
        //           IQueryable のまま返すことで呼び出し側でソート・ページングを追加できる（遅延評価）
        public override IQueryable<ExcelItemEntity> GetBaseQuery(
            ExcelItemCondModel? cond = null, bool includeDelete = false)
        {
            IQueryable<ExcelItemEntity> query = dbSet.Where(x => includeDelete ? true : !x.DelFlag);

            if (cond != null)
            {
                if (cond.Id != null)                        query = query.Where(x => x.Id == cond.Id);
                if (!string.IsNullOrEmpty(cond.Name))       query = query.Where(x => x.Name.Contains(cond.Name));
                if (!string.IsNullOrEmpty(cond.Category))   query = query.Where(x => x.Category == cond.Category);
            }

            return query;
        }
    }

    /// <summary>
    /// Repository 専用の検索条件モデル
    /// </summary>
    public class ExcelItemCondModel : IRepositoryCondModel
    {
        public long? Id { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }

        // ポイント: Pager はページング・ソート情報を保持する共通モデル
        public Dev.CommonLibrary.Common.CommonListPagerModel Pager { get; set; } = new(1, "Id", "ASC", 10);
    }
}
