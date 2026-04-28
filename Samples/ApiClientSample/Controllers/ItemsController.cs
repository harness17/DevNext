using ApiClientSample.Models;
using ApiClientSample.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiClientSample.Controllers;

/// <summary>商品 CRUD を担当するコントローラー</summary>
public class ItemsController(ApiSampleClient apiClient) : Controller
{
    private string? Token => HttpContext.Session.GetString(AccountController.SessionKeyToken);
    private bool IsAdmin => HttpContext.Session.GetString(AccountController.SessionKeyIsAdmin) == "1";

    // Admin 以外はホームへリダイレクト（Forbid() は認証スキーム未設定で 500 になるため使わない）
    private IActionResult ForbidRedirect() => RedirectToAction("Index", "Home");

    // ─────────────────────────────────────────────────────────────
    // GET /Items/Detail/{id}
    // ─────────────────────────────────────────────────────────────
    public async Task<IActionResult> Detail(long id)
    {
        if (Token is null) return RedirectToAction("Login", "Account");

        var item = await apiClient.GetItemAsync(id, Token);
        if (item is null) return NotFound();

        ViewBag.IsAdmin = IsAdmin;
        return View(item);
    }

    // ─────────────────────────────────────────────────────────────
    // GET /Items/Create
    // ─────────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Create()
    {
        if (Token is null) return RedirectToAction("Login", "Account");
        if (!IsAdmin) return ForbidRedirect();
        return View(new ItemFormViewModel());
    }

    // ─────────────────────────────────────────────────────────────
    // POST /Items/Create
    // ─────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ItemFormViewModel form)
    {
        if (Token is null) return RedirectToAction("Login", "Account");
        if (!IsAdmin) return ForbidRedirect();
        if (!ModelState.IsValid) return View(form);

        var created = await apiClient.CreateItemAsync(form, Token);
        if (created is null)
        {
            ModelState.AddModelError(string.Empty, "登録に失敗しました。ApiSample が起動しているか確認してください。");
            return View(form);
        }

        return RedirectToAction("Index", "Home");
    }

    // ─────────────────────────────────────────────────────────────
    // GET /Items/Edit/{id}
    // ─────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        if (Token is null) return RedirectToAction("Login", "Account");
        if (!IsAdmin) return ForbidRedirect();

        var item = await apiClient.GetItemAsync(id, Token);
        if (item is null) return NotFound();

        var form = new ItemFormViewModel
        {
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            Stock = item.Stock,
        };
        ViewBag.ItemId = id;
        return View(form);
    }

    // ─────────────────────────────────────────────────────────────
    // POST /Items/Edit/{id}
    // ─────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, ItemFormViewModel form)
    {
        if (Token is null) return RedirectToAction("Login", "Account");
        if (!IsAdmin) return ForbidRedirect();
        if (!ModelState.IsValid) { ViewBag.ItemId = id; return View(form); }

        var updated = await apiClient.UpdateItemAsync(id, form, Token);
        if (updated is null)
        {
            ModelState.AddModelError(string.Empty, "更新に失敗しました。");
            ViewBag.ItemId = id;
            return View(form);
        }

        return RedirectToAction("Index", "Home");
    }

    // ─────────────────────────────────────────────────────────────
    // POST /Items/Delete/{id}
    // ─────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        if (Token is null) return RedirectToAction("Login", "Account");
        if (!IsAdmin) return ForbidRedirect();

        await apiClient.DeleteItemAsync(id, Token);
        return RedirectToAction("Index", "Home");
    }
}
