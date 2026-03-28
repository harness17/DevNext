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
    /// スケジュール・カレンダー Controller。
    /// GetEvents / Detail は JSON API として動作する。
    /// </summary>
    [Authorize]
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class ScheduleController : Controller
    {
        private readonly ScheduleService _service;

        public ScheduleController(ScheduleService service)
        {
            _service = service;
        }

        // ─── カレンダー画面 ───────────────────────────────────────────────────

        /// <summary>
        /// GET: カレンダーメイン画面
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ─── JSON API（FullCalendar 用） ───────────────────────────────────────

        /// <summary>
        /// GET: FullCalendar 用イベント JSON
        /// FullCalendar は /Schedule/GetEvents?start=2026-04-01&amp;end=2026-05-01 の形式でリクエストする。
        /// </summary>
        [HttpGet]
        public IActionResult GetEvents(DateTime start, DateTime end)
        {
            try
            {
                var events = _service.GetEventsForRange(start, end, GetCurrentUserId());
                return Json(events);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET: 予定詳細 JSON（モーダル用）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Detail(long id)
        {
            var detail = await _service.GetDetailAsync(id);
            if (detail == null)
                return Json(new { error = "予定が見つかりませんでした。" });
            return Json(detail);
        }

        // ─── 作成 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// GET: 作成フォーム（モーダル用）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create(DateTime? defaultStart = null)
        {
            var vm = await _service.BuildCreateFormAsync(GetCurrentUserId(), defaultStart);
            return PartialView("_EventFormModal", vm);
        }

        /// <summary>
        /// POST: 予定作成
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ScheduleEventFormViewModel vm)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, errors = GetModelErrors() });

            if (vm.EndDate <= vm.StartDate)
            {
                ModelState.AddModelError("EndDate", "終了日時は開始日時より後に設定してください。");
                return Json(new { success = false, errors = GetModelErrors() });
            }

            _service.Create(vm, GetCurrentUserId());
            return Json(new { success = true });
        }

        // ─── 編集 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// GET: 編集フォーム（モーダル用）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var vm = await _service.GetForEditAsync(id, GetCurrentUserId());
            if (vm == null) return Forbid();
            return PartialView("_EventFormModal", vm);
        }

        /// <summary>
        /// POST: 予定更新
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ScheduleEventFormViewModel vm)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, errors = GetModelErrors() });

            if (vm.EndDate <= vm.StartDate)
            {
                ModelState.AddModelError("EndDate", "終了日時は開始日時より後に設定してください。");
                return Json(new { success = false, errors = GetModelErrors() });
            }

            bool updated = _service.Update(vm, GetCurrentUserId());
            if (!updated) return Json(new { success = false, errors = new[] { "更新権限がありません。" } });
            return Json(new { success = true });
        }

        // ─── 削除 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// POST: 予定削除
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(long id)
        {
            bool deleted = _service.Delete(id, GetCurrentUserId());
            if (!deleted) return Json(new { success = false, error = "削除権限がありません。" });
            return Json(new { success = true });
        }

        // ─── 参加ステータス更新 ───────────────────────────────────────────────

        /// <summary>
        /// POST: 参加ステータス更新（Accepted / Declined）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateParticipantStatus(long eventId, ParticipantStatus status)
        {
            bool updated = _service.UpdateParticipantStatus(eventId, GetCurrentUserId(), status);
            if (!updated) return Json(new { success = false, error = "対象の参加者ではありません。" });
            return Json(new { success = true });
        }

        // ─── 内部ユーティリティ ───────────────────────────────────────────────

        private string GetCurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private List<string> GetModelErrors()
            => ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
    }
}
