using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Site.Models;
using System.Diagnostics;

namespace Site.Controllers
{
    /// <summary>
    /// エラーハンドリング専用コントローラー
    /// UseExceptionHandler / UseStatusCodePagesWithReExecute から呼び出される
    /// </summary>
    [AllowAnonymous]
    public class RootErrorController : Controller
    {
        private readonly Logger _logger = Logger.GetLogger();

        /// <summary>
        /// 500系エラー（未処理例外）のハンドラー
        /// UseExceptionHandler("/RootError/Error") から呼び出される
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // 例外情報を取得してログに記録する
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionFeature?.Error != null)
            {
                _logger.Error(new LogModel($"未処理例外が発生しました。Path={exceptionFeature.Path}"), exceptionFeature.Error);
            }

            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = 500,
                ErrorTitle = "サーバーエラーが発生しました",
                ErrorMessage = "申し訳ありません。サーバー内部でエラーが発生しました。\nしばらく時間をおいてから再度お試しください。"
            };

            return View("Error", model);
        }

        /// <summary>
        /// 404/403 などのHTTPステータスコードエラーのハンドラー
        /// UseStatusCodePagesWithReExecute("/RootError/StatusCode/{0}") から呼び出される
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("RootError/StatusCode/{statusCode}")]
        public new IActionResult StatusCode(int statusCode)
        {
            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = statusCode
            };

            // ステータスコード別にメッセージを設定する
            switch (statusCode)
            {
                case 404:
                    model.ErrorTitle = "ページが見つかりません";
                    model.ErrorMessage = "お探しのページは移動・削除されたか、URLが間違っている可能性があります。";
                    break;
                case 403:
                    model.ErrorTitle = "アクセスが拒否されました";
                    model.ErrorMessage = "このページへのアクセス権限がありません。";
                    break;
                default:
                    model.ErrorTitle = $"エラーが発生しました（{statusCode}）";
                    model.ErrorMessage = "しばらく時間をおいてから再度お試しください。";
                    break;
            }

            return View("Error", model);
        }
    }
}
