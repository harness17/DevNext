using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Site.Service;
using System.Security.Claims;

namespace Site.Controllers
{
    /// <summary>
    /// 通知 Controller。ナビバーの Ajax ポーリング用エンドポイントを提供する。
    /// ポイント: POST 系アクションは [ValidateAntiForgeryToken] で保護する。
    ///           Ajax 側は X-CSRF-TOKEN ヘッダーでトークンを送信する（AddAntiforgery の HeaderName 設定と対応）。
    /// </summary>
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly NotificationService _service;

        public NotificationController(NotificationService service)
        {
            _service = service;
        }

        // ─── Ajax エンドポイント ────────────────────────────────────────────────

        /// <summary>
        /// GET: 未読件数を返す。ナビバーのバッジ表示用にポーリングされる。
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _service.GetUnreadCountAsync(GetCurrentUserId());
            return Json(new { count });
        }

        /// <summary>
        /// GET: 最新10件の通知を返す。ドロップダウン表示用。
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var notifications = await _service.GetRecentAsync(GetCurrentUserId());
            var result = notifications.Select(n => new
            {
                id = n.Id,
                message = n.Message,
                relatedUrl = n.RelatedUrl ?? "",
                isRead = n.IsRead,
                // ポイント: サーバー側でフォーマットしてクライアント側の処理を簡略化
                createDate = n.CreateDate.ToString("MM/dd HH:mm"),
            });
            return Json(result);
        }

        /// <summary>
        /// POST: 指定 ID の通知を既読にする。
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadRequest request)
        {
            await _service.MarkAsReadAsync(request.Id, GetCurrentUserId());
            return Ok();
        }

        /// <summary>
        /// POST: すべての未読通知を既読にする。
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _service.MarkAllAsReadAsync(GetCurrentUserId());
            return Ok();
        }

        // ─── 内部ユーティリティ ───────────────────────────────────────────────

        private string GetCurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    }

    /// <summary>MarkAsRead リクエストボディ</summary>
    public class MarkAsReadRequest
    {
        public long Id { get; set; }
    }
}
