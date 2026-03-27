using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using Site.Common;
using Site.Entity;
using Site.Models;

namespace Site.Repository
{
    /// <summary>
    /// 承認申請リポジトリ。
    /// ApprovalRequest に履歴テーブルは存在しないため RepositoryBase を使わず直接実装する。
    /// </summary>
    public class ApprovalRequestRepository
    {
        private readonly DBContext _context;

        public ApprovalRequestRepository(DBContext context)
        {
            _context = context;
        }

        // ─── CRUD 基本操作 ─────────────────────────────────────────────────────

        public ApprovalRequestEntity? SelectById(long id)
            => _context.ApprovalRequest.FirstOrDefault(x => x.Id == id && !x.DelFlag);

        public void Insert(ApprovalRequestEntity entity)
        {
            _context.ApprovalRequest.Add(entity);
            _context.SaveChanges();
        }

        public void Update(ApprovalRequestEntity entity)
        {
            _context.ApprovalRequest.Update(entity);
            _context.SaveChanges();
        }

        public void LogicalDelete(ApprovalRequestEntity entity)
        {
            entity.DelFlag = true;
            entity.SetForUpdate();
            _context.ApprovalRequest.Update(entity);
            _context.SaveChanges();
        }

        // ─── 一覧取得 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 一覧取得（ソート・ページング付き）
        /// </summary>
        public ApprovalRequestListData GetList(ApprovalRequestCondModel cond)
        {
            var model = new ApprovalRequestListData();
            IQueryable<ApprovalRequestEntity> query = GetBaseQuery(cond);

            // デフォルトソートは Id 降順
            cond.Pager.sort = string.IsNullOrEmpty(cond.Pager.sort) ? "Id" : cond.Pager.sort;
            cond.Pager.sortdir = string.IsNullOrEmpty(cond.Pager.sortdir) ? "DESC" : cond.Pager.sortdir;

            // ポイント: switch 式でソート列・方向を切り替える
            if (cond.Pager.sortdir.ToLower() == "desc")
            {
                query = cond.Pager.sort switch
                {
                    "Title"         => query.OrderByDescending(x => x.Title),
                    "Status"        => query.OrderByDescending(x => x.Status),
                    "RequestedDate" => query.OrderByDescending(x => x.RequestedDate),
                    _               => query.OrderByDescending(x => x.Id)
                };
            }
            else
            {
                query = cond.Pager.sort switch
                {
                    "Title"         => query.OrderBy(x => x.Title),
                    "Status"        => query.OrderBy(x => x.Status),
                    "RequestedDate" => query.OrderBy(x => x.RequestedDate),
                    _               => query.OrderBy(x => x.Id)
                };
            }

            // ポイント: Count() を先に実行してページング総件数を取得してから Take/Skip を適用する
            int totalRecords = query.Count();
            LocalUtil.SetTakeSkip(ref query, cond);
            model.Rows = query.ToList();
            model.Summary = Util.CreateSummary(cond.Pager, totalRecords, "{0}件中 {1} - {2} を表示");
            return model;
        }

        /// <summary>
        /// 論理削除フィルタ＋検索条件付きクエリを返す（遅延評価）
        /// </summary>
        public IQueryable<ApprovalRequestEntity> GetBaseQuery(ApprovalRequestCondModel? cond = null, bool includeDelete = false)
        {
            IQueryable<ApprovalRequestEntity> query = _context.ApprovalRequest
                .Where(x => includeDelete ? true : !x.DelFlag);

            if (cond != null)
            {
                // 申請者 ID による絞り込み（Member ロールは自分の申請のみ参照できる）
                if (!string.IsNullOrEmpty(cond.RequesterUserId))
                    query = query.Where(x => x.RequesterUserId == cond.RequesterUserId);

                // タイトルの部分一致検索
                if (!string.IsNullOrEmpty(cond.Title))
                    query = query.Where(x => x.Title.Contains(cond.Title));

                // 状態による絞り込み
                if (cond.Status != null)
                    query = query.Where(x => x.Status == cond.Status);
            }

            return query;
        }
    }

    /// <summary>
    /// Repository 用の検索条件モデル（View 用 ViewModel とは分離する）
    /// </summary>
    public class ApprovalRequestCondModel : IRepositoryCondModel
    {
        public string? RequesterUserId { get; set; }
        public string? Title { get; set; }
        public ApprovalStatus? Status { get; set; }

        // ポイント: Pager はページング・ソート情報を保持する共通モデル（共通ライブラリで定義）
        public CommonListPagerModel Pager { get; set; } = new(1, "Id", "DESC", 10);
    }

    /// <summary>一覧取得結果</summary>
    public class ApprovalRequestListData
    {
        public List<ApprovalRequestEntity> Rows { get; set; } = new();
        public Dev.CommonLibrary.Common.CommonListSummaryModel? Summary { get; set; }
    }
}
