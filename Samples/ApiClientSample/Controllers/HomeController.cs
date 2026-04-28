using ApiClientSample.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiClientSample.Controllers;

/// <summary>商品一覧を表示するコントローラー</summary>
public class HomeController(ApiSampleClient apiClient) : Controller
{
    private string? Token => HttpContext.Session.GetString(AccountController.SessionKeyToken);
    private bool IsAdmin => HttpContext.Session.GetString(AccountController.SessionKeyIsAdmin) == "1";

    public async Task<IActionResult> Index()
    {
        if (Token is null) return RedirectToAction("Login", "Account");

        var items = await apiClient.GetItemsAsync(Token);
        if (items is null)
        {
            ViewBag.Error = "商品データの取得に失敗しました。ApiSample（localhost:5042）が起動しているか確認してください。";
            return View(new List<Models.ItemViewModel>());
        }

        ViewBag.IsAdmin = IsAdmin;
        return View(items);
    }
}
