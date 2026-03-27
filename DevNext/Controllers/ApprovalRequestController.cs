using Dev.CommonLibrary.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Site.Common;
using Site.Models;
using Site.Service;
using System.Security.Claims;

namespace Site.Controllers
{
    /// <summary>
    /// 承認ワークフロー Controller。
    /// [Authorize] でログイン必須。ロール別制御は Service 層・View 層で行う。
    /// </summary>
    [Authorize]
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class ApprovalRequestController : Controller
    {
        private readonly ApprovalWorkflowService _service;

        public ApprovalRequestController(ApprovalWorkflowService service)
        {
            _service = service;
        }

        // ─── 一覧 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// GET: 一覧（URL直打ち・ページング・ソート・一覧復帰）
        /// </summary>
        [HttpGet]
        public IActionResult Index(SearchModelBase? pageModel = null, bool returnList = false)
        {
            var model = LocalUtil.MapPageModelTo<ApprovalRequestIndexViewModel>(pageModel);

            // ページング・ソート変更時、Ajax リクエスト時、または一覧復帰時は TempData から検索条件を復元する
            if (model.PageRead != null || IsAjaxRequest() || returnList)
            {
                var sessionCond = TempData.Peek(SessionKey.ApprovalRequestCondViewModel);
                if (sessionCond != null)
                    model.Cond = System.Text.Json.JsonSerializer.Deserialize<ApprovalRequestCondViewModel>(sessionCond.ToString()!)!;

                if (returnList)
                {
                    var sessionPage = TempData.Peek(SessionKey.ApprovalRequestPageModel);
                    if (sessionPage != null)
                    {
                        var savedPage = System.Text.Json.JsonSerializer.Deserialize<SearchModelBase>(sessionPage.ToString()!)!;
                        model.Page      = savedPage.Page;
                        model.Sort      = savedPage.Sort;
                        model.SortDir   = savedPage.SortDir;
                        model.RecordNum = savedPage.RecordNum;
                    }
                }

                if (IsAjaxRequest()) model.PageRead = PageRead.Paging;
            }

            model = _service.GetList(model, GetCurrentUserId(), User.IsInRole("Admin"));

            // 検索条件・ページ状態を TempData に保存（一覧復帰・ページング用）
            TempData[SessionKey.ApprovalRequestCondViewModel] = System.Text.Json.JsonSerializer.Serialize(model.Cond);
            TempData[SessionKey.ApprovalRequestPageModel] = System.Text.Json.JsonSerializer.Serialize(
                new SearchModelBase { Page = model.Page, Sort = model.Sort, SortDir = model.SortDir, RecordNum = model.RecordNum });

            if (IsAjaxRequest()) return PartialView("_IndexPartial", model);
            return View(model);
        }

        /// <summary>
        /// POST: 検索（検索フォーム送信）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(ApprovalRequestIndexViewModel model)
        {
            model = _service.GetList(model, GetCurrentUserId(), User.IsInRole("Admin"));
            TempData[SessionKey.ApprovalRequestCondViewModel] = System.Text.Json.JsonSerializer.Serialize(model.Cond);
            TempData[SessionKey.ApprovalRequestPageModel] = System.Text.Json.JsonSerializer.Serialize(
                new SearchModelBase { Page = model.Page, Sort = model.Sort, SortDir = model.SortDir, RecordNum = model.RecordNum });
            return View(model);
        }

        // ─── 新規作成 ──────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Create()
        {
            return View(new ApprovalRequestFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ApprovalRequestFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            _service.Create(model, GetCurrentUserId());
            TempData[SessionKey.Message] = model.SubmitRequest ? "申請しました。" : "下書きを保存しました。";
            return RedirectToAction(nameof(Index), new { returnList = true });
        }

        // ─── 編集 ──────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Edit(long id)
        {
            var model = _service.GetForEdit(id, GetCurrentUserId());
            if (model == null) return RedirectToAction(nameof(Index));
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ApprovalRequestFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            bool updated = _service.Update(model, GetCurrentUserId());
            if (!updated) return RedirectToAction(nameof(Index));

            TempData[SessionKey.Message] = model.SubmitRequest ? "申請しました。" : "下書きを保存しました。";
            return RedirectToAction(nameof(Index), new { returnList = true });
        }

        // ─── 詳細・承認/却下 ────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Detail(long id)
        {
            var model = await _service.GetDetailAsync(id);
            if (model == null) return RedirectToAction(nameof(Index));
            return View(model);
        }

        /// <summary>承認（Pending → Approved）。Admin 専用。</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Approve(long id, string? actionComment)
        {
            _service.Approve(id, actionComment);
            TempData[SessionKey.Message] = "申請を承認しました。";
            return RedirectToAction(nameof(Detail), new { id });
        }

        /// <summary>却下（Pending → Rejected）。Admin 専用。</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Reject(long id, string? actionComment)
        {
            _service.Reject(id, actionComment);
            TempData[SessionKey.Message] = "申請を却下しました。";
            return RedirectToAction(nameof(Detail), new { id });
        }

        // ─── 削除 ──────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Delete(long id)
        {
            var model = _service.GetForDelete(id, GetCurrentUserId());
            if (model == null) return RedirectToAction(nameof(Index));
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(long id)
        {
            _service.Delete(id, GetCurrentUserId());
            TempData[SessionKey.Message] = "申請を削除しました。";
            return RedirectToAction(nameof(Index), new { returnList = true });
        }

        // ─── 内部ユーティリティ ───────────────────────────────────────────────

        private string GetCurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private bool IsAjaxRequest()
            => Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}
