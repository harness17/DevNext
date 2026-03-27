using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Site.Service;
using System.Security.Claims;

namespace Site.Controllers
{
    /// <summary>
    /// 通知 Controller。ナビバーの Ajax ポーリング用エンドポイントを提供する。
    /// ポイント: [ValidateAntiForgeryToken] を付けないことで Ajax からの POST を受け付ける。
    ///           認証必須（[Authorize]）のため CSRF リスクは低い。
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
        public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadRequest request)
        {
            await _service.MarkAsReadAsync(request.Id, GetCurrentUserId());
            return Ok();
        }

        /// <summary>
        /// POST: すべての未読通知を既読にする。
        /// </summary>
        [HttpPost]
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
