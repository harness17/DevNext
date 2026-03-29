using AutoMapper;
using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Site.Common;
using Dev.CommonLibrary.Entity;
using Site.Entity;
using Site.Models;
using Site.Repository;

namespace Site.Service
{
    /// <summary>
    /// 承認ワークフローのビジネスロジックを担うサービス。
    /// 状態遷移・権限チェック・申請者名の解決をここで行う。
    /// </summary>
    public class ApprovalWorkflowService
    {
        private readonly DBContext _context;
        private readonly ApprovalRequestRepository _repo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public ApprovalWorkflowService(DBContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
        {
            _context = context;
            // ポイント: Repository はサービス内で new して使う（DIせず）
            //           Repository 自体が DBContext に依存するため、DI 済みの context を渡して初期化する
            _repo = new ApprovalRequestRepository(context);
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // ─── 一覧取得 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 申請一覧を取得する。
        /// Admin は全件、Member は自分の申請のみ。
        /// </summary>
        public ApprovalRequestIndexViewModel GetList(ApprovalRequestIndexViewModel model, string currentUserId, bool isAdmin)
        {
            if (model.Cond == null) model.Cond = new ApprovalRequestCondViewModel();

            var condModel = BuildCondModel(model.Cond, model, isAdmin ? null : currentUserId);
            model.RowData = _repo.GetList(condModel);

            // ポイント: SelectListUtility.GetEnumSelectListItem<T>() で Enum からドロップダウン用リストを自動生成
            model.Cond.StatusList = Dev.CommonLibrary.Common.SelectListUtility
                .GetEnumSelectListItem<ApprovalStatus>().ToList();

            return model;
        }

        // ─── 詳細取得 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 指定 ID の申請詳細を取得する。申請者名は UserManager から解決する。
        /// </summary>
        public async Task<ApprovalRequestDetailViewModel?> GetDetailAsync(long id)
        {
            var entity = _repo.SelectById(id);
            if (entity == null) return null;

            var mapper = new MapperConfiguration(cfg =>
                cfg.CreateMap<ApprovalRequestEntity, ApprovalRequestDetailViewModel>(),
                NullLoggerFactory.Instance).CreateMapper();

            var vm = mapper.Map<ApprovalRequestDetailViewModel>(entity);

            // 申請者名を UserManager から解決する
            var requester = await _userManager.FindByIdAsync(entity.RequesterUserId);
            vm.RequesterName = requester?.UserName ?? entity.RequesterUserId;

            return vm;
        }

        // ─── 作成 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 申請を新規作成する。SubmitRequest=true の場合は即座に申請中（Pending）にする。
        /// </summary>
        public async Task CreateAsync(ApprovalRequestFormViewModel vm, string currentUserId)
        {
            var entity = new ApprovalRequestEntity
            {
                Title = vm.Title,
                Content = vm.Content,
                RequesterUserId = currentUserId,
                Status = vm.SubmitRequest ? ApprovalStatus.Pending : ApprovalStatus.Draft,
            };

            if (vm.SubmitRequest)
                entity.RequestedDate = DateTime.Now;

            entity.SetForCreate();
            _repo.Insert(entity);

            // ポイント: 申請時（Draft → Pending）は Admin ロール全ユーザーに通知する
            if (vm.SubmitRequest)
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                var requester = await _userManager.FindByIdAsync(currentUserId);
                var requesterName = requester?.UserName ?? currentUserId;
                var message = $"{requesterName} さんから申請「{vm.Title}」が届きました";
                var relatedUrl = $"/ApprovalRequest/Detail/{entity.Id}";
                await _notificationService.CreateForMultipleAsync(admins.Select(a => a.Id), message, relatedUrl);
            }
        }

        // ─── 編集用データ取得 ─────────────────────────────────────────────────

        /// <summary>
        /// 編集フォーム用のデータを取得する。申請者本人かつ Draft のみ編集可能。
        /// </summary>
        public ApprovalRequestFormViewModel? GetForEdit(long id, string currentUserId)
        {
            var entity = _repo.SelectById(id);
            if (entity == null) return null;

            // 編集できるのは申請者本人かつ Draft のみ
            if (entity.RequesterUserId != currentUserId || entity.Status != ApprovalStatus.Draft)
                return null;

            return new ApprovalRequestFormViewModel
            {
                Id = entity.Id,
                Title = entity.Title,
                Content = entity.Content,
            };
        }

        // ─── 更新 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 申請を更新する（Draft → Draft / Draft → Pending）。
        /// 申請者本人かつ Draft のみ更新可能。
        /// </summary>
        public async Task<bool> UpdateAsync(ApprovalRequestFormViewModel vm, string currentUserId)
        {
            var entity = _repo.SelectById(vm.Id);
            if (entity == null) return false;
            if (entity.RequesterUserId != currentUserId || entity.Status != ApprovalStatus.Draft)
                return false;

            entity.Title = vm.Title;
            entity.Content = vm.Content;

            if (vm.SubmitRequest)
            {
                entity.Status = ApprovalStatus.Pending;
                entity.RequestedDate = DateTime.Now;
            }

            entity.SetForUpdate();
            _repo.Update(entity);

            // ポイント: 申請時（Draft → Pending）は Admin ロール全ユーザーに通知する
            if (vm.SubmitRequest)
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                var requester = await _userManager.FindByIdAsync(currentUserId);
                var requesterName = requester?.UserName ?? currentUserId;
                var message = $"{requesterName} さんから申請「{vm.Title}」が届きました";
                var relatedUrl = $"/ApprovalRequest/Detail/{entity.Id}";
                await _notificationService.CreateForMultipleAsync(admins.Select(a => a.Id), message, relatedUrl);
            }

            return true;
        }

        // ─── 承認・却下 ────────────────────────────────────────────────────────

        /// <summary>
        /// 申請を承認する（Pending → Approved）。Admin のみ実行可能。
        /// </summary>
        public async Task<bool> ApproveAsync(long id, string? comment)
        {
            var entity = _repo.SelectById(id);
            if (entity == null || entity.Status != ApprovalStatus.Pending) return false;

            entity.Status = ApprovalStatus.Approved;
            entity.ApproverComment = comment;
            entity.ApprovedDate = DateTime.Now;
            entity.SetForUpdate();
            _repo.Update(entity);

            // ポイント: 承認時（Pending → Approved）は申請者に通知する
            var message = $"申請「{entity.Title}」が承認されました";
            var relatedUrl = $"/ApprovalRequest/Detail/{entity.Id}";
            await _notificationService.CreateAsync(entity.RequesterUserId, message, relatedUrl);

            return true;
        }

        /// <summary>
        /// 申請を却下する（Pending → Rejected）。Admin のみ実行可能。
        /// </summary>
        public async Task<bool> RejectAsync(long id, string? comment)
        {
            var entity = _repo.SelectById(id);
            if (entity == null || entity.Status != ApprovalStatus.Pending) return false;

            entity.Status = ApprovalStatus.Rejected;
            entity.ApproverComment = comment;
            entity.ApprovedDate = DateTime.Now;
            entity.SetForUpdate();
            _repo.Update(entity);

            // ポイント: 却下時（Pending → Rejected）は申請者に通知する
            var message = $"申請「{entity.Title}」が却下されました";
            var relatedUrl = $"/ApprovalRequest/Detail/{entity.Id}";
            await _notificationService.CreateAsync(entity.RequesterUserId, message, relatedUrl);

            return true;
        }

        // ─── 削除確認データ取得 ───────────────────────────────────────────────

        /// <summary>
        /// 削除確認ページ用データを取得する。申請者本人かつ Draft のみ削除可能。
        /// </summary>
        public ApprovalRequestDeleteViewModel? GetForDelete(long id, string currentUserId)
        {
            var entity = _repo.SelectById(id);
            if (entity == null) return null;
            if (entity.RequesterUserId != currentUserId || entity.Status != ApprovalStatus.Draft)
                return null;

            return new ApprovalRequestDeleteViewModel
            {
                Id = entity.Id,
                Title = entity.Title,
                Status = entity.Status,
                CreateDate = entity.CreateDate,
            };
        }

        // ─── 削除 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 申請を論理削除する（Draft のみ）。
        /// </summary>
        public bool Delete(long id, string currentUserId)
        {
            var entity = _repo.SelectById(id);
            if (entity == null) return false;
            if (entity.RequesterUserId != currentUserId || entity.Status != ApprovalStatus.Draft)
                return false;

            _repo.LogicalDelete(entity);
            return true;
        }

        // ─── 内部ユーティリティ ───────────────────────────────────────────────

        private ApprovalRequestCondModel BuildCondModel(ApprovalRequestCondViewModel vm, ApprovalRequestIndexViewModel indexModel, string? requesterUserId)
        {
            var cond = new ApprovalRequestCondModel
            {
                Title = vm.Title,
                RequesterUserId = requesterUserId,
                // SearchModelBase のプロパティから直接 Pager を組み立てる
                Pager = new CommonListPagerModel(
                    indexModel.Page,
                    string.IsNullOrEmpty(indexModel.Sort) ? "Id" : indexModel.Sort,
                    string.IsNullOrEmpty(indexModel.SortDir) ? "DESC" : indexModel.SortDir,
                    indexModel.RecordNum)
            };

            if (!string.IsNullOrEmpty(vm.Status) && int.TryParse(vm.Status, out int statusVal))
                cond.Status = (ApprovalStatus)statusVal;

            return cond;
        }
    }
}
