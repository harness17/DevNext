using Microsoft.AspNetCore.Identity;
using Site.Common;
using Site.Entity;
using Site.Entity.Enums;
using Site.Models;
using Site.Repository;

namespace Site.Service
{
    /// <summary>
    /// スケジュール機能のビジネスロジック。
    /// 繰り返し展開は ScheduleRecurrenceHelper に委譲する。
    /// </summary>
    public class ScheduleService
    {
        private readonly DBContext _context;
        private readonly ScheduleRepository _repo;
        private readonly UserManager<ApplicationUser> _userManager;

        public ScheduleService(DBContext context, UserManager<ApplicationUser> userManager, ScheduleRepository repo)
        {
            _context = context;
            _repo = repo;
            _userManager = userManager;
        }

        // ─── カレンダーイベント取得 ───────────────────────────────────────────

        /// <summary>
        /// FullCalendar 用のイベント JSON リストを返す。
        /// 繰り返しイベントは rangeStart〜rangeEnd の範囲で展開する。
        /// </summary>
        public List<ScheduleEventJsonDto> GetEventsForRange(
            DateTime rangeStart, DateTime rangeEnd, string currentUserId)
        {
            var entities = _repo.GetEventsForRange(rangeStart, rangeEnd, currentUserId);
            var result = new List<ScheduleEventJsonDto>();

            foreach (var ev in entities)
            {
                // 繰り返し展開ヘルパーで発生日リストを取得
                var occurrences = ScheduleRecurrenceHelper.GetOccurrences(
                    ev.StartDate, ev.RecurrenceType, ev.RecurrenceInterval,
                    ev.RecurrenceEndDate, ev.RecurrenceDaysOfWeek,
                    rangeStart, rangeEnd);

                var duration = ev.EndDate - ev.StartDate;
                var color = GetEventColor(ev, currentUserId);

                foreach (var occStart in occurrences)
                {
                    result.Add(new ScheduleEventJsonDto
                    {
                        Id      = ev.Id.ToString(),
                        Title   = ev.Title,
                        Start   = occStart.ToString("yyyy-MM-ddTHH:mm:ss"),
                        End     = (occStart + duration).ToString("yyyy-MM-ddTHH:mm:ss"),
                        AllDay  = ev.IsAllDay,
                        Color   = color,
                        IsShared = ev.IsShared,
                        OwnerId = ev.OwnerId,
                    });
                }
            }

            return result;
        }

        // ─── 詳細取得 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 予定の詳細を取得する（モーダル表示用 JSON）。
        /// 閲覧権限がない場合（他人の個人予定）は null を返す。
        /// </summary>
        public async Task<ScheduleEventDetailDto?> GetDetailAsync(long id, string currentUserId)
        {
            var entity = _repo.SelectById(id);
            if (entity == null) return null;

            // 閲覧権限チェック: 共有予定 OR 自分の予定 OR 自分が参加者 であること
            var isParticipant = _repo.GetParticipant(id, currentUserId) != null;
            if (!entity.IsShared && entity.OwnerId != currentUserId && !isParticipant)
                return null;

            var owner = await _userManager.FindByIdAsync(entity.OwnerId);
            var participants = _repo.GetParticipants(id);
            var participantDtos = new List<ParticipantDetailDto>();

            foreach (var p in participants)
            {
                var user = await _userManager.FindByIdAsync(p.UserId);
                participantDtos.Add(new ParticipantDetailDto
                {
                    UserId      = p.UserId,
                    UserName    = user?.UserName ?? p.UserId,
                    Status      = p.Status,
                    StatusLabel = p.Status switch
                    {
                        ParticipantStatus.Accepted => "承諾",
                        ParticipantStatus.Declined => "辞退",
                        _                          => "未回答",
                    },
                });
            }

            return new ScheduleEventDetailDto
            {
                Id                   = entity.Id,
                Title                = entity.Title,
                Description          = entity.Description,
                StartDate            = entity.StartDate.ToString("yyyy/MM/dd HH:mm"),
                EndDate              = entity.EndDate.ToString("yyyy/MM/dd HH:mm"),
                IsAllDay             = entity.IsAllDay,
                IsShared             = entity.IsShared,
                OwnerName            = owner?.UserName ?? entity.OwnerId,
                OwnerId              = entity.OwnerId,
                RecurrenceType       = entity.RecurrenceType,
                RecurrenceInterval   = entity.RecurrenceInterval,
                RecurrenceEndDate    = entity.RecurrenceEndDate?.ToString("yyyy/MM/dd"),
                RecurrenceDaysOfWeek = entity.RecurrenceDaysOfWeek,
                Participants         = participantDtos,
            };
        }

        // ─── フォーム用データ取得 ─────────────────────────────────────────────

        /// <summary>
        /// 作成フォーム用 ViewModel を生成する（ユーザーリストを付与）。
        /// </summary>
        public async Task<ScheduleEventFormViewModel> BuildCreateFormAsync(
            string currentUserId, DateTime? defaultStart = null)
        {
            var start = defaultStart ?? DateTime.Today.AddHours(9);
            return new ScheduleEventFormViewModel
            {
                StartDate = start,
                EndDate   = start.AddHours(1),
                UserList  = await GetUserSelectListAsync(currentUserId),
            };
        }

        /// <summary>
        /// 編集フォーム用 ViewModel を生成する。
        /// 作成者以外が呼び出した場合は null を返す。
        /// </summary>
        public async Task<ScheduleEventFormViewModel?> GetForEditAsync(long id, string currentUserId)
        {
            var entity = _repo.SelectById(id);
            // 存在しないか作成者以外はアクセス不可
            if (entity == null || entity.OwnerId != currentUserId) return null;

            var participants = _repo.GetParticipants(id);

            return new ScheduleEventFormViewModel
            {
                Id                   = entity.Id,
                Title                = entity.Title,
                Description          = entity.Description,
                StartDate            = entity.StartDate,
                EndDate              = entity.EndDate,
                IsAllDay             = entity.IsAllDay,
                IsShared             = entity.IsShared,
                RecurrenceType       = entity.RecurrenceType,
                RecurrenceInterval   = entity.RecurrenceInterval,
                RecurrenceEndDate    = entity.RecurrenceEndDate,
                SelectedDaysOfWeek   = ParseDaysOfWeek(entity.RecurrenceDaysOfWeek),
                ParticipantUserIds   = participants.Select(p => p.UserId).ToList(),
                UserList             = await GetUserSelectListAsync(currentUserId),
            };
        }

        // ─── 作成・更新・削除 ─────────────────────────────────────────────────

        /// <summary>
        /// 予定を新規作成する。
        /// </summary>
        public void Create(ScheduleEventFormViewModel vm, string currentUserId)
        {
            var entity = new ScheduleEventEntity
            {
                Title                = vm.Title,
                Description          = vm.Description,
                StartDate            = vm.StartDate,
                EndDate              = vm.EndDate,
                IsAllDay             = vm.IsAllDay,
                IsShared             = vm.IsShared,
                OwnerId              = currentUserId,
                RecurrenceType       = vm.RecurrenceType,
                RecurrenceInterval   = vm.RecurrenceInterval,
                RecurrenceDaysOfWeek = BuildDaysOfWeekString(vm),
                RecurrenceEndDate    = vm.RecurrenceType != RecurrenceType.None
                                           ? vm.RecurrenceEndDate
                                           : null,
            };
            entity.SetForCreate();
            _repo.Insert(entity);

            // 参加者を登録する
            foreach (var userId in vm.ParticipantUserIds.Distinct())
            {
                if (userId == currentUserId) continue; // 作成者自身は除外
                var participant = new ScheduleEventParticipantEntity
                {
                    EventId = entity.Id,
                    UserId  = userId,
                    Status  = ParticipantStatus.Invited,
                };
                participant.SetForCreate();
                _repo.InsertParticipant(participant);
            }
        }

        /// <summary>
        /// 予定を更新する。
        /// 作成者以外が呼び出した場合は false を返す。
        /// </summary>
        public bool Update(ScheduleEventFormViewModel vm, string currentUserId)
        {
            var entity = _repo.SelectById(vm.Id);
            if (entity == null || entity.OwnerId != currentUserId) return false;

            // 更新前に履歴を保存する
            _repo.InsertHistory(entity);

            entity.Title                = vm.Title;
            entity.Description          = vm.Description;
            entity.StartDate            = vm.StartDate;
            entity.EndDate              = vm.EndDate;
            entity.IsAllDay             = vm.IsAllDay;
            entity.IsShared             = vm.IsShared;
            entity.RecurrenceType       = vm.RecurrenceType;
            entity.RecurrenceInterval   = vm.RecurrenceInterval;
            entity.RecurrenceDaysOfWeek = BuildDaysOfWeekString(vm);
            entity.RecurrenceEndDate    = vm.RecurrenceType != RecurrenceType.None
                                              ? vm.RecurrenceEndDate
                                              : null;
            entity.SetForUpdate();
            _repo.Update(entity);

            // 参加者を洗い替えする。既存のステータスは引き継ぎ、新規追加分のみ Invited とする。
            var existingParticipants = _repo.GetParticipants(entity.Id)
                .ToDictionary(p => p.UserId);
            _repo.DeleteParticipantsByEventId(entity.Id);
            foreach (var userId in vm.ParticipantUserIds.Distinct())
            {
                if (userId == currentUserId) continue;
                // 既存参加者はステータスを引き継ぐ、新規追加は Invited
                var existingStatus = existingParticipants.TryGetValue(userId, out var ep)
                    ? ep.Status
                    : ParticipantStatus.Invited;
                var participant = new ScheduleEventParticipantEntity
                {
                    EventId = entity.Id,
                    UserId  = userId,
                    Status  = existingStatus,
                };
                participant.SetForCreate();
                _repo.InsertParticipant(participant);
            }

            return true;
        }

        /// <summary>
        /// 予定を論理削除する。
        /// 作成者以外が呼び出した場合は false を返す。
        /// </summary>
        public bool Delete(long id, string currentUserId)
        {
            var entity = _repo.SelectById(id);
            if (entity == null || entity.OwnerId != currentUserId) return false;

            _repo.DeleteParticipantsByEventId(id);
            _repo.LogicalDelete(entity);
            return true;
        }

        /// <summary>
        /// 参加ステータスを更新する。
        /// 対象参加者でない場合は false を返す。
        /// </summary>
        public bool UpdateParticipantStatus(long eventId, string currentUserId, ParticipantStatus status)
        {
            var participant = _repo.GetParticipant(eventId, currentUserId);
            if (participant == null) return false;

            participant.Status = status;
            participant.SetForUpdate();
            _repo.UpdateParticipant(participant);
            return true;
        }

        // ─── 内部ユーティリティ ───────────────────────────────────────────────

        /// <summary>
        /// イベントの表示色を決定する。
        /// 個人=青 / 共有かつ自分作成=緑 / 招待された共有=橙
        /// </summary>
        private static string GetEventColor(ScheduleEventEntity ev, string currentUserId)
        {
            if (!ev.IsShared)                   return "#0d6efd"; // 個人: 青
            if (ev.OwnerId == currentUserId)    return "#198754"; // 共有・自分作成: 緑
            return "#fd7e14";                                     // 招待: 橙
        }

        /// <summary>フォームの SelectedDaysOfWeek をカンマ区切り文字列に変換する。</summary>
        private static string? BuildDaysOfWeekString(ScheduleEventFormViewModel vm)
        {
            if (vm.RecurrenceType != RecurrenceType.Weekly || vm.SelectedDaysOfWeek.Count == 0)
                return null;
            return string.Join(",", vm.SelectedDaysOfWeek.Distinct().OrderBy(d => d));
        }

        /// <summary>カンマ区切り文字列を int リストに変換する。無効な値は除外する。</summary>
        private static List<int> ParseDaysOfWeek(string? daysOfWeek)
        {
            if (string.IsNullOrEmpty(daysOfWeek)) return new List<int>();
            // DB の不整合データに備え、int.TryParse で無効値をスキップする
            return daysOfWeek.Split(',')
                .Select(s => s.Trim())
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToList();
        }

        /// <summary>
        /// 参加者候補のユーザーリストを生成する（現在のユーザー本人を除く）。
        /// </summary>
        private async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetUserSelectListAsync(
            string excludeUserId)
        {
            var users = _userManager.Users
                .Where(u => u.Id != excludeUserId)
                .OrderBy(u => u.UserName)
                .ToList();

            return users.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = u.Id,
                Text  = u.UserName ?? u.Id,
            }).ToList();
        }
    }
}
