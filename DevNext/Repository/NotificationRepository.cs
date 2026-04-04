using Microsoft.EntityFrameworkCore;
using Site.Common;
using Site.Entity;

namespace Site.Repository
{
    /// <summary>
    /// 通知リポジトリ。CRUD と未読取得・既読更新を提供する。
    /// </summary>
    public class NotificationRepository
    {
        private readonly DBContext _context;

        public NotificationRepository(DBContext context)
        {
            _context = context;
        }

        /// <summary>通知を新規登録する。</summary>
        public async Task InsertAsync(NotificationEntity entity)
        {
            _context.Notification.Add(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>指定ユーザーの未読件数を取得する。</summary>
        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notification
                .AsNoTracking()
                .CountAsync(n => n.RecipientUserId == userId && !n.IsRead && !n.DelFlag);
        }

        /// <summary>指定ユーザーの最新通知を取得する（新しい順）。</summary>
        public async Task<List<NotificationEntity>> GetRecentAsync(string userId, int count = 10)
        {
            return await _context.Notification
                .AsNoTracking()
                .Where(n => n.RecipientUserId == userId && !n.DelFlag)
                .OrderByDescending(n => n.CreateDate)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>複数通知をまとめて登録する。</summary>
        public async Task InsertRangeAsync(IEnumerable<NotificationEntity> entities)
        {
            _context.Notification.AddRange(entities);
            await _context.SaveChangesAsync();
        }

        /// <summary>ID で通知を取得する。</summary>
        public async Task<NotificationEntity?> SelectByIdAsync(long id)
        {
            return await _context.Notification
                .FirstOrDefaultAsync(n => n.Id == id && !n.DelFlag);
        }

        /// <summary>通知を更新する。</summary>
        public async Task UpdateAsync(NotificationEntity entity)
        {
            _context.Notification.Update(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>指定ユーザーの未読通知をすべて既読にする。</summary>
        public async Task MarkAllAsReadAsync(string userId)
        {
            await _context.Notification
                .Where(n => n.RecipientUserId == userId && !n.IsRead && !n.DelFlag)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.UpdateDate, _ => DateTime.Now)
                    .SetProperty(n => n.UpdateApplicationUserId, _ => userId));
        }

        /// <summary>指定ユーザーの通知を1件既読にする。</summary>
        public async Task MarkAsReadAsync(long id, string userId)
        {
            await _context.Notification
                .Where(n => n.Id == id && n.RecipientUserId == userId && !n.IsRead && !n.DelFlag)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.UpdateDate, _ => DateTime.Now)
                    .SetProperty(n => n.UpdateApplicationUserId, _ => userId));
        }
    }
}
