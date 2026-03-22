using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Site.Common;
using Site.Entity;
using Site.Models;

namespace Site.Service
{
    /// <summary>
    /// ユーザー・ロール管理サービス
    /// Identity の UserManager / RoleManager を使ってユーザー情報を操作する
    /// </summary>
    public class UserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        /// <summary>
        /// 検索条件・ページング・ソートに基づきユーザー一覧を取得する
        /// </summary>
        public async Task<UserManagementViewModel> GetUserListAsync(UserManagementViewModel model)
        {
            if (model.Cond == null) model.Cond = new UserManagementCondViewModel();
            // ページャー設定（件数・ページ番号・ソート列 を CondViewModel に反映）
            LocalUtil.SetPager(model.Cond, model);

            var query = _userManager.Users.AsQueryable();

            // 検索条件フィルタリング
            if (!string.IsNullOrEmpty(model.Cond.UserName))
                query = query.Where(u => u.UserName != null && u.UserName.Contains(model.Cond.UserName));

            if (!string.IsNullOrEmpty(model.Cond.Email))
                query = query.Where(u => u.Email != null && u.Email.Contains(model.Cond.Email));

            // ソート
            query = model.Sort switch
            {
                "Email"    => model.SortDir == "ASC" ? query.OrderBy(u => u.Email)    : query.OrderByDescending(u => u.Email),
                "UserName" => model.SortDir == "ASC" ? query.OrderBy(u => u.UserName) : query.OrderByDescending(u => u.UserName),
                _          => query.OrderBy(u => u.UserName),
            };

            // 件数取得（ページングより先に全件数を確定する）
            int totalRecords = query.Count();

            // ページング適用
            LocalUtil.SetTakeSkip(ref query, model.Cond);
            var users = query.ToList();

            // ポイント: GetRolesAsync は非同期・1件ずつのため foreach で処理する
            var items = new List<UserManagementListItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                items.Add(new UserManagementListItemViewModel
                {
                    Id             = user.Id,
                    UserName       = user.UserName ?? "",
                    Email          = user.Email ?? "",
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd     = user.LockoutEnd,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles          = roles.ToList(),
                });
            }

            model.RowData = new UserManagementDataViewModel
            {
                rows    = items,
                Summary = Util.CreateSummary(model.Cond.Pager, totalRecords, "{0}件中 {1} - {2} を表示"),
            };

            return model;
        }

        /// <summary>
        /// 指定IDのユーザーを編集画面用 ViewModel に変換して返す
        /// </summary>
        public async Task<UserManagementEditViewModel?> GetUserEditAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return null;

            var currentRoles   = await _userManager.GetRolesAsync(user);
            var availableRoles = GetAvailableRoles();

            return new UserManagementEditViewModel
            {
                Id             = user.Id,
                UserName       = user.UserName ?? "",
                Email          = user.Email ?? "",
                EmailConfirmed = user.EmailConfirmed,
                RoleNames      = currentRoles.ToList(),
                AvailableRoles = availableRoles,
            };
        }

        /// <summary>
        /// ユーザーの基本情報とロールを更新する
        /// </summary>
        public async Task<IdentityResult> UpdateUserAsync(UserManagementEditViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "ユーザーが見つかりません。" });

            user.UserName       = model.UserName;
            user.Email          = model.Email;
            user.EmailConfirmed = model.EmailConfirmed;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded) return updateResult;

            // ポイント: ロールの差分更新（不要なロール削除 → 新規ロール追加）
            var currentRoles  = await _userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(model.RoleNames).ToList();
            var rolesToAdd    = model.RoleNames.Except(currentRoles).ToList();

            // ポイント: 初期 Admin ユーザーは Admin ロールを剥奪できない（システム管理上の制約）
            if (user.Id == Const.SystemAdminUserId)
                rolesToRemove.Remove("Admin");

            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded) return removeResult;
            }

            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded) return addResult;
            }

            return IdentityResult.Success;
        }

        /// <summary>
        /// ユーザーを削除する（初期 Admin ユーザーは削除不可）
        /// </summary>
        public async Task<IdentityResult> DeleteUserAsync(string id)
        {
            // ポイント: 初期 Admin ユーザーはシステム管理上の必須アカウントのため削除を禁止する
            if (id == Const.SystemAdminUserId)
                return IdentityResult.Failed(new IdentityError { Description = "初期管理者ユーザーは削除できません。" });

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "ユーザーが見つかりません。" });

            return await _userManager.DeleteAsync(user);
        }

        /// <summary>
        /// バリデーションエラー後の再表示用にロール一覧を補完する
        /// </summary>
        public void FillAvailableRoles(UserManagementEditViewModel model)
        {
            model.AvailableRoles = GetAvailableRoles();
        }

        // ポイント: ロール選択肢を名前順で返す共通処理
        private List<SelectListItem> GetAvailableRoles()
            => _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToList();
    }
}
