using Site.Common;
using Site.Entity;
using Site.Repository;

namespace Site.Service
{
    /// <summary>
    /// 通知サービス。通知の作成・取得・既読更新を提供する。
    /// ApprovalWorkflowService から呼び出され、状態遷移イベントに連動して通知を生成する。
    /// </summary>
    public class NotificationService
    {
        private readonly NotificationRepository _repo;

        public NotificationService(DBContext context)
        {
            // ポイント: Repository はサービス内で new して使う（DIせず）
            //           Repository 自体が DBContext に依存するため、DI 済みの context を渡して初期化する
            _repo = new NotificationRepository(context);
        }

        // ─── 作成 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 指定ユーザー1人に通知を作成する。
        /// </summary>
        public async Task CreateAsync(string recipientUserId, string message, string? relatedUrl = null)
        {
            var entity = new NotificationEntity
            {
                RecipientUserId = recipientUserId,
                Message = message,
                RelatedUrl = relatedUrl,
                IsRead = false,
            };
            entity.SetForCreate();
            await _repo.InsertAsync(entity);
        }

        /// <summary>
        /// 複数ユーザーに同じ通知をまとめて作成する（Admin への申請通知などで使用）。
        /// </summary>
        public async Task CreateForMultipleAsync(IEnumerable<string> recipientUserIds, string message, string? relatedUrl = null)
        {
            foreach (var userId in recipientUserIds)
            {
                await CreateAsync(userId, message, relatedUrl);
            }
        }

        // ─── 取得 ──────────────────────────────────────────────────────────────

        /// <summary>指定ユーザーの未読件数を返す。</summary>
        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _repo.GetUnreadCountAsync(userId);
        }

        /// <summary>指定ユーザーの最新通知を返す（新しい順、最大10件）。</summary>
        public async Task<List<NotificationEntity>> GetRecentAsync(string userId, int count = 10)
        {
            return await _repo.GetRecentAsync(userId, count);
        }

        // ─── 既読更新 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 指定 ID の通知を既読にする。
        /// 本人の通知のみ更新可能（他ユーザーの通知は無視）。
        /// </summary>
        public async Task MarkAsReadAsync(long id, string userId)
        {
            var entity = await _repo.SelectByIdAsync(id);
            // ポイント: 本人の通知以外は無視（他人の通知を既読にできないよう保護）
            if (entity == null || entity.RecipientUserId != userId) return;

            entity.IsRead = true;
            entity.SetForUpdate();
            await _repo.UpdateAsync(entity);
        }

        /// <summary>指定ユーザーの未読通知をすべて既読にする。</summary>
        public async Task MarkAllAsReadAsync(string userId)
        {
            await _repo.MarkAllAsReadAsync(userId);
        }
    }
}
