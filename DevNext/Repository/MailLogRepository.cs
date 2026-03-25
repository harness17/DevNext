using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using Site.Common;
using Site.Entity;
using Site.Models;

namespace Site.Repository
{
    /// <summary>
    /// メール送信ログリポジトリ
    /// MailLogEntity の挿入・一覧取得を担当する。
    /// ログデータのため履歴テーブル・更新・削除操作は持たない。
    /// </summary>
    public class MailLogRepository : RepositoryBase<MailLogEntity, MailLogCondViewModel>
    {
        private readonly DBContext _context;

        public MailLogRepository(DBContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// メール送信ログ一覧をページング・ソート付きで取得する
        /// </summary>
        /// <param name="cond">検索条件</param>
        /// <returns>一覧データ＋ページング情報</returns>
        public MailLogListDataViewModel GetMailLogList(MailLogCondViewModel cond)
        {
            var model = new MailLogListDataViewModel();
            IQueryable<MailLogEntity> query = GetBaseQuery(cond);

            // デフォルトソートは送信日時（CreateDate）降順
            cond.Pager.sort = string.IsNullOrEmpty(cond.Pager.sort) ? "CreateDate" : cond.Pager.sort;
            cond.Pager.sortdir = string.IsNullOrEmpty(cond.Pager.sortdir) ? "DESC" : cond.Pager.sortdir;

            if (cond.Pager.sortdir.ToLower() == "desc")
            {
                query = cond.Pager.sort switch
                {
                    "SenderName" => query.OrderByDescending(x => x.SenderName),
                    "SenderEmail" => query.OrderByDescending(x => x.SenderEmail),
                    "Subject" => query.OrderByDescending(x => x.Subject),
                    "IsSuccess" => query.OrderByDescending(x => x.IsSuccess),
                    _ => query.OrderByDescending(x => x.CreateDate)
                };
            }
            else
            {
                query = cond.Pager.sort switch
                {
                    "SenderName" => query.OrderBy(x => x.SenderName),
                    "SenderEmail" => query.OrderBy(x => x.SenderEmail),
                    "Subject" => query.OrderBy(x => x.Subject),
                    "IsSuccess" => query.OrderBy(x => x.IsSuccess),
                    _ => query.OrderBy(x => x.CreateDate)
                };
            }

            int totalRecords = query.Count();
            LocalUtil.SetTakeSkip(ref query, cond);
            model.Rows = query.ToList();
            model.Summary = Util.CreateSummary(cond.Pager, totalRecords, "{0}件中 {1} - {2} を表示");
            return model;
        }

        /// <summary>
        /// 検索条件を適用したクエリを返す
        /// </summary>
        public override IQueryable<MailLogEntity> GetBaseQuery(MailLogCondViewModel? cond = null, bool includeDelete = false)
        {
            // ログテーブルのため論理削除は使用しないが、基底クラスの DelFlag フィルタを通す
            IQueryable<MailLogEntity> query = dbSet.Where(x => !x.DelFlag);

            if (cond != null)
            {
                if (!string.IsNullOrEmpty(cond.SenderName))
                    query = query.Where(x => x.SenderName.Contains(cond.SenderName));

                if (!string.IsNullOrEmpty(cond.SenderEmail))
                    query = query.Where(x => x.SenderEmail.Contains(cond.SenderEmail));

                if (cond.IsSuccess != null)
                    query = query.Where(x => x.IsSuccess == cond.IsSuccess);

                if (cond.SentFrom != null)
                    query = query.Where(x => cond.SentFrom <= x.CreateDate);

                if (cond.SentTo != null)
                    // 終了日は当日23:59:59まで含める
                    query = query.Where(x => x.CreateDate < cond.SentTo.Value.AddDays(1));
            }

            return query;
        }
    }

    /// <summary>
    /// メール送信ログ検索条件モデル（リポジトリ用）
    /// </summary>
    public class MailLogCondViewModel : IRepositoryCondModel
    {
        /// <summary>送信者名（部分一致）</summary>
        public string? SenderName { get; set; }

        /// <summary>送信者メールアドレス（部分一致）</summary>
        public string? SenderEmail { get; set; }

        /// <summary>成功/失敗フィルタ（null=全て）</summary>
        public bool? IsSuccess { get; set; }

        /// <summary>送信日付（開始）</summary>
        public DateTime? SentFrom { get; set; }

        /// <summary>送信日付（終了）</summary>
        public DateTime? SentTo { get; set; }

        /// <summary>ページング・ソート情報</summary>
        public CommonListPagerModel Pager { get; set; } = new(1, "CreateDate", "DESC", 20);
    }
}
