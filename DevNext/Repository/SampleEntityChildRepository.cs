using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using Site.Common;
using Site.Entity;

namespace Site.Repository
{
    // ポイント: RepositoryBase<TEntity, THistory, TCond> を継承することで
    //           Insert / Update / LogicalDelete の際に履歴テーブルへの自動保存が有効になる
    public class SampleEntityChildRepository : RepositoryBase<SampleEntityChild, SampleEntityChildHistory, SampleEntityChildCondViewModel>
    {
        public SampleEntityChildRepository(DBContext context) : base(context) { }

        // ポイント: 論理削除フィルタ + 条件絞り込みを IQueryable で返す
        //           includeDelete=true にすると削除済みも含めて取得できる
        public override IQueryable<SampleEntityChild> GetBaseQuery(SampleEntityChildCondViewModel? cond = null, bool includeDelete = false)
        {
            IQueryable<SampleEntityChild> query = dbSet.Where(x => includeDelete ? true : !x.DelFlag);

            if (cond != null)
            {
                // 親IDで絞り込む（子一覧取得の主な用途）
                if (cond.ParentId != null) query = query.Where(x => x.SumpleEntityID == cond.ParentId);
                if (cond.Id != null) query = query.Where(x => x.Id == cond.Id);
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
        /// 一覧画面で親ごとの子件数を効率的に取得するために使用する
        /// </summary>
        public Dictionary<long, int> GetChildCountsByParentIds(IEnumerable<long> parentIds)
        {
            // ポイント: GroupBy + ToDictionary で1クエリで全親の子件数を取得する
            //           N+1 問題を避けるために SELECT ... WHERE IN を使った一括取得パターン
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

            // ポイント: LogicalDeletes を使って一括論理削除する（BaseClassのメソッド）
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
