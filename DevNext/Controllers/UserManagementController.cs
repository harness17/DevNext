using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Site.Common;
using Site.Models;
using Site.Service;

namespace Site.Controllers
{
    // ポイント: [Authorize(Roles = "Admin")] でクラス全体をAdminロール限定に制限する
    //           Adminロール以外でアクセスすると403/ログイン画面にリダイレクトされる
    /// <summary>
    /// ユーザー管理コントローラー。Admin ロール専用。
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManagementService _workerService;
        private readonly Logger _logger = Logger.GetLogger();

        public UserManagementController(UserManagementService workerService)
        {
            _workerService = workerService;
        }

        /// <summary>
        /// GET: ユーザー一覧（URL直打ち・ページング・ソート・一覧復帰）。
        /// ポイント: Identity の非同期 API を使用するため async/await を使用する。
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(SearchModelBase? pageModel = null, bool returnList = false)
        {
            var model = LocalUtil.MapPageModelTo<UserManagementViewModel>(pageModel);

            if (model.PageRead != null || IsAjaxRequest() || returnList)
            {
                var sessionCond = TempData.Peek(SessionKey.UserManagementCondViewModel);
                if (sessionCond != null)
                    model.Cond = System.Text.Json.JsonSerializer.Deserialize<UserManagementCondViewModel>(sessionCond.ToString()!)!;

                // ポイント: 一覧復帰時はページ番号・ソート状態も TempData から復元する
                if (returnList)
                {
                    var sessionPage = TempData.Peek(SessionKey.UserManagementPageModel);
                    if (sessionPage != null)
                    {
                        var saved = System.Text.Json.JsonSerializer.Deserialize<SearchModelBase>(sessionPage.ToString()!)!;
                        model.Page      = saved.Page;
                        model.Sort      = saved.Sort;
                        model.SortDir   = saved.SortDir;
                        model.RecordNum = saved.RecordNum;
                    }
                }

                if (IsAjaxRequest()) model.PageRead = PageRead.Paging;
            }

            model = await _workerService.GetUserListAsync(model);

            TempData[SessionKey.UserManagementCondViewModel] = System.Text.Json.JsonSerializer.Serialize(model.Cond);
            TempData[SessionKey.UserManagementPageModel] = System.Text.Json.JsonSerializer.Serialize(
                new SearchModelBase { Page = model.Page, Sort = model.Sort, SortDir = model.SortDir, RecordNum = model.RecordNum });

            return View(model);
        }

        /// <summary>POST: 検索フォーム送信。</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UserManagementViewModel model)
        {
            model = await _workerService.GetUserListAsync(model);
            TempData[SessionKey.UserManagementCondViewModel] = System.Text.Json.JsonSerializer.Serialize(model.Cond);
            TempData[SessionKey.UserManagementPageModel] = System.Text.Json.JsonSerializer.Serialize(
                new SearchModelBase { Page = model.Page, Sort = model.Sort, SortDir = model.SortDir, RecordNum = model.RecordNum });

            if (IsAjaxRequest()) return PartialView("_IndexPartial", model);
            return View(model);
        }

        /// <summary>ユーザー編集フォーム表示。</summary>
        public async Task<IActionResult> Edit(string? id)
        {
            if (id == null) return BadRequest();
            var model = await _workerService.GetUserEditAsync(id);
            if (model == null)
            {
                _logger.Warn(new LogModel($"ユーザーが見つかりません: id={id}"));
                return NotFound();
            }
            return View(model);
        }

        /// <summary>ユーザー編集処理（POST）。</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserManagementEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _workerService.UpdateUserAsync(model);
                if (result.Succeeded)
                {
                    TempData[SessionKey.Message] = LocalUtil.GetUpdateAlertMessage("ユーザー");
                    return RedirectToAction("Index", new { returnList = true });
                }

                // ポイント: IdentityResult のエラーをモデルに追加して画面に表示する
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }

            // ポイント: バリデーションエラー時はロール選択肢を補完して再表示する
            _workerService.FillAvailableRoles(model);
            return View(model);
        }

        /// <summary>ユーザー削除確認フォーム表示。</summary>
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null) return BadRequest();

            // ポイント: 初期 Admin ユーザーへの削除操作をここで遮断する
            if (id == Const.SystemAdminUserId)
            {
                TempData[SessionKey.Message] = LocalUtil.GetErrorAlertMessage("初期管理者ユーザーは削除できません");
                return RedirectToAction("Index", new { returnList = true });
            }

            var model = await _workerService.GetUserEditAsync(id);
            if (model == null)
            {
                _logger.Warn(new LogModel($"ユーザーが見つかりません: id={id}"));
                return NotFound();
            }
            return View(model);
        }

        /// <summary>ユーザー削除処理（POST）。</summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _workerService.DeleteUserAsync(id);
            TempData[SessionKey.Message] = LocalUtil.GetDeleteAlertMessage("ユーザー");
            return RedirectToAction("Index", new { returnList = true });
        }

        private bool IsAjaxRequest()
            => Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}
