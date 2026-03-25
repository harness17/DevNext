using Dev.CommonLibrary.Common;
using Site.Common;
using Site.Models;
using Site.Repository;

namespace Site.Service
{
    /// <summary>
    /// メール送信ログ一覧サービス
    /// </summary>
    public class MailLogService
    {
        private readonly MailLogRepository _mailLogRepository;

        public MailLogService(DBContext context)
        {
            _mailLogRepository = new MailLogRepository(context);
        }

        /// <summary>
        /// メール送信ログ一覧を取得する
        /// </summary>
        /// <param name="model">一覧 ViewModel（検索条件・ページング情報を含む）</param>
        /// <returns>一覧データをセットした ViewModel</returns>
        public MailLogViewModel GetMailLogList(MailLogViewModel model)
        {
            if (model.Cond == null) model.Cond = new MailLogCondSearchViewModel();

            // ページャー設定（件数・ページ番号・ソート列を CondViewModel に反映）
            LocalUtil.SetPager(model.Cond, model);

            // View 用 CondViewModel → Repository 用 CondModel へ変換
            var cond = new MailLogCondViewModel
            {
                SenderName = model.Cond.SenderName,
                SenderEmail = model.Cond.SenderEmail,
                IsSuccess = model.Cond.IsSuccess,
                SentFrom = model.Cond.SentFrom,
                SentTo = model.Cond.SentTo,
                Pager = model.Cond.Pager
            };

            model.RowData = _mailLogRepository.GetMailLogList(cond);
            return model;
        }
    }
}
