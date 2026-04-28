using ApiClientSample.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiClientSample.Controllers;

/// <summary>商品一覧を表示するコントローラー</summary>
public class HomeController(ApiSampleClient apiClient) : Controller
{
    private const string SessionKeyToken = "JwtToken";

    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString(SessionKeyToken);
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Account");

        var items = await apiClient.GetItemsAsync(token);
        if (items is null)
        {
            ViewBag.Error = "商品データの取得に失敗しました。ApiSample（localhost:5042）が起動しているか確認してください。";
            return View(new List<Models.ItemViewModel>());
        }

        return View(items);
    }
}
