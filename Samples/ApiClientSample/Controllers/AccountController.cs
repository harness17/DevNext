using ApiClientSample.Models;
using ApiClientSample.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiClientSample.Controllers;

/// <summary>ログイン・ログアウトを担当するコントローラー</summary>
public class AccountController(ApiSampleClient apiClient) : Controller
{
    internal const string SessionKeyToken = "JwtToken";
    internal const string SessionKeyIsAdmin = "IsAdmin";

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await apiClient.LoginAsync(model.Email, model.Password);
        if (result is null)
        {
            ModelState.AddModelError(string.Empty, "メールアドレスまたはパスワードが正しくありません。ApiSample が起動しているか確認してください。");
            return View(model);
        }

        HttpContext.Session.SetString(SessionKeyToken, result.Token);
        HttpContext.Session.SetString(SessionKeyIsAdmin, result.IsAdmin ? "1" : "0");
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
