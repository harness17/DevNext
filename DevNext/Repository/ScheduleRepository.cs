using Site.Common;
using Site.Entity;
using Site.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace Site.Repository
{
    /// <summary>
    /// スケジュールイベントリポジトリ。
    /// 参加者テーブルも含めて管理する。
    /// </summary>
    public class ScheduleRepository
    {
        private readonly DBContext _context;

        public ScheduleRepository(DBContext context)
        {
            _context = context;
        }

        // ─── ScheduleEvent CRUD ───────────────────────────────────────────────

        public ScheduleEventEntity? SelectById(long id)
            => _context.ScheduleEvent.FirstOrDefault(x => x.Id == id && !x.DelFlag);

        public void Insert(ScheduleEventEntity entity)
        {
            _context.ScheduleEvent.Add(entity);
            _context.SaveChanges();
        }

        public void InsertHistory(ScheduleEventEntity entity)
        {
            // 更新前に現在の状態を履歴テーブルへコピーする
            var history = MapToHistory(entity);
            _context.ScheduleEventHistory.Add(history);
            _context.SaveChanges();
        }

        public void Update(ScheduleEventEntity entity)
        {
            _context.ScheduleEvent.Update(entity);
            _context.SaveChanges();
        }

        public void LogicalDelete(ScheduleEventEntity entity)
        {
            entity.SetForLogicalDelete();
            _context.ScheduleEvent.Update(entity);
            _context.SaveChanges();
        }

        // ─── カレンダー範囲取得 ───────────────────────────────────────────────

        /// <summary>
        /// 指定期間に表示すべきイベントを取得する。
        /// - 自分の個人イベント（IsShared=false かつ OwnerId=currentUserId）
        /// - 共有イベント（IsShared=true）
        /// - 自分が参加者として招待されているイベント
        /// 繰り返しイベントは RecurrenceEndDate を考慮してウィンドウ外のものを除外する。
        /// </summary>
        public List<ScheduleEventEntity> GetEventsForRange(
            DateTime rangeStart, DateTime rangeEnd, string currentUserId)
        {
            // 参加者として招待されているイベント ID を取得
            var participantEventIds = _context.ScheduleEventParticipant
                .AsNoTracking()
                .Where(p => p.UserId == currentUserId && !p.DelFlag)
                .Select(p => p.EventId)
                .ToHashSet();

            return _context.ScheduleEvent
                .AsNoTracking()
                .Where(x => !x.DelFlag)
                // 閲覧権限: 個人（自分）・共有・招待済み
                .Where(x => (!x.IsShared && x.OwnerId == currentUserId)
                         || x.IsShared
                         || participantEventIds.Contains(x.Id))
                // 期間フィルタ: 繰り返しなしは EndDate, 繰り返しありは RecurrenceEndDate で判断
                .Where(x => x.StartDate < rangeEnd
                         && (x.RecurrenceType != RecurrenceType.None
                             ? (!x.RecurrenceEndDate.HasValue || x.RecurrenceEndDate.Value >= rangeStart)
                             : x.EndDate > rangeStart))
                .ToList();
        }

        // ─── Participant 操作 ─────────────────────────────────────────────────

        public List<ScheduleEventParticipantEntity> GetParticipants(long eventId)
            => _context.ScheduleEventParticipant
                .AsNoTracking()
                .Where(x => x.EventId == eventId && !x.DelFlag)
                .ToList();

        public ScheduleEventParticipantEntity? GetParticipant(long eventId, string userId)
            => _context.ScheduleEventParticipant
                .FirstOrDefault(x => x.EventId == eventId && x.UserId == userId && !x.DelFlag);

        public void InsertParticipant(ScheduleEventParticipantEntity entity)
        {
            _context.ScheduleEventParticipant.Add(entity);
            _context.SaveChanges();
        }

        public void UpdateParticipant(ScheduleEventParticipantEntity entity)
        {
            _context.ScheduleEventParticipant.Update(entity);
            _context.SaveChanges();
        }

        public void DeleteParticipantsByEventId(long eventId)
        {
            // イベント削除時に参加者も論理削除する
            var participants = _context.ScheduleEventParticipant
                .Where(x => x.EventId == eventId && !x.DelFlag)
                .ToList();
            foreach (var p in participants)
                p.SetForLogicalDelete();
            _context.SaveChanges();
        }

        // ─── 内部ユーティリティ ───────────────────────────────────────────────

        private static ScheduleEventEntityHistory MapToHistory(ScheduleEventEntity src)
            => new()
            {
                Id = src.Id,
                Title = src.Title,
                Description = src.Description,
                StartDate = src.StartDate,
                EndDate = src.EndDate,
                IsAllDay = src.IsAllDay,
                IsShared = src.IsShared,
                OwnerId = src.OwnerId,
                RecurrenceType = src.RecurrenceType,
                RecurrenceInterval = src.RecurrenceInterval,
                RecurrenceEndDate = src.RecurrenceEndDate,
                RecurrenceDaysOfWeek = src.RecurrenceDaysOfWeek,
                DelFlag = src.DelFlag,
                CreateDate = src.CreateDate,
                UpdateDate = src.UpdateDate,
                CreateApplicationUserId = src.CreateApplicationUserId,
                UpdateApplicationUserId = src.UpdateApplicationUserId,
            };
    }
}
