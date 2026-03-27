using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Site.Models;
using Site.Service;

namespace Site.Controllers
{
    /// <summary>
    /// ダッシュボードコントローラー
    /// グラフ・統計情報の表示と Ajax データ提供を担当する
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly DashboardService _dashboardService;

        public DashboardController(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// ダッシュボードページ
        /// グラフデータは JS の Ajax ポーリングで取得するため、ViewModel には API URL のみ渡す
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            var model = new DashboardViewModel
            {
                ChartDataUrl = Url.Action(nameof(GetChartData))!,
            };
            ViewData["Title"] = "ダッシュボード";
            return View(model);
        }

        /// <summary>
        /// グラフデータ取得 API（Ajax ポーリングのエンドポイント）
        /// 全チャートのデータを 1 リクエストでまとめて返す
        /// ポーリング頻度が高いためアクセスログ記録は行わない
        /// </summary>
        [HttpGet]
        public IActionResult GetChartData()
        {
            var data = _dashboardService.GetAllChartData();
            return Json(data);
        }
    }
}
