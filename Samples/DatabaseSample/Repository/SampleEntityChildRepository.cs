using DatabaseSample.Data;
using DatabaseSample.Entity;
using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;

namespace DatabaseSample.Repository
{
    // ポイント: RepositoryBase<TEntity, THistory, TCond> を継承することで
    //           Insert / Update / LogicalDelete の際に履歴テーブルへの自動保存が有効になる
    public class SampleEntityChildRepository : RepositoryBase<SampleEntityChild, SampleEntityChildHistory, SampleEntityChildCondViewModel>
    {
        public SampleEntityChildRepository(DatabaseSampleDbContext context) : base(context) { }

        // ポイント: 論理削除フィルタ + 条件絞り込みを IQueryable で返す
        public override IQueryable<SampleEntityChild> GetBaseQuery(
            SampleEntityChildCondViewModel? cond = null, bool includeDelete = false)
        {
            IQueryable<SampleEntityChild> query = dbSet.Where(x => includeDelete ? true : !x.DelFlag);

            if (cond != null)
            {
                if (cond.ParentId != null) query = query.Where(x => x.SumpleEntityID == cond.ParentId);
                if (cond.Id != null)       query = query.Where(x => x.Id == cond.Id);
            }

            return query;
        }

        /// <summary>
        /// 指定した親IDに紐づく子レコードを Id 昇順で取得する
        /// </summary>
        public List<SampleEntityChild> GetChildrenByParentId(long parentId)
        {
            return dbSet
                .Where(x => !x.DelFlag && x.SumpleEntityID == parentId)
                .OrderBy(x => x.Id)
                .ToList();
        }

        /// <summary>
        /// 指定した複数の親IDに対して、子レコードの件数をディクショナリで返す
        /// ポイント: GroupBy + ToDictionary で1クエリで全親の子件数を取得する（N+1 回避）
        /// </summary>
        public Dictionary<long, int> GetChildCountsByParentIds(IEnumerable<long> parentIds)
        {
            return dbSet
                .Where(x => !x.DelFlag && parentIds.Contains(x.SumpleEntityID))
                .GroupBy(x => x.SumpleEntityID)
                .Select(g => new { ParentId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.ParentId, x => x.Count);
        }

        /// <summary>
        /// 指定した親IDに紐づく子レコードをすべて論理削除する（親削除時の連動処理）
        /// </summary>
        public void LogicalDeleteByParentId(long parentId)
        {
            var children = dbSet
                .Where(x => !x.DelFlag && x.SumpleEntityID == parentId)
                .ToList();

            LogicalDeletes(children);
        }
    }

    /// <summary>
    /// SampleEntityChild 検索条件モデル（Repository 専用）
    /// </summary>
    public class SampleEntityChildCondViewModel : IRepositoryCondModel
    {
        public long? Id { get; set; }
        /// <summary>親エンティティのID（SumpleEntityID に対応）</summary>
        public long? ParentId { get; set; }
        // ポイント: Pager はページング・ソート情報を保持する共通モデル（共通ライブラリで定義）
        public CommonListPagerModel Pager { get; set; } = new(1, "Id", "ASC", 100);
    }
}
