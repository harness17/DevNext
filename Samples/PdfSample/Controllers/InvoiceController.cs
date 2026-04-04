using Dev.CommonLibrary.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdfSample.Common;
using PdfSample.Models;
using PdfSample.Service;

namespace PdfSample.Controllers;

[Authorize]
[ServiceFilter(typeof(AccessLogAttribute))]
public class InvoiceController : Controller
{
    private readonly PdfSampleService _service;

    public InvoiceController(PdfSampleService service)
    {
        _service = service;
    }

    /// <summary>
    /// 一覧の初期表示、および一覧復帰時の状態復元を行う。
    /// </summary>
    [HttpGet]
    public IActionResult Index(SearchModelBase? pageModel = null, bool returnList = false)
    {
        var model = LocalUtil.MapPageModelTo<InvoiceViewModel>(pageModel);

        if (model.PageRead != null || IsAjaxRequest() || returnList)
        {
            var sessionCond = TempData.Peek(SessionKey.PdfSampleCondViewModel);
            if (sessionCond != null)
                model.Cond = System.Text.Json.JsonSerializer.Deserialize<InvoiceCondViewModel>(sessionCond.ToString()!)!;

            if (returnList)
            {
                var sessionPage = TempData.Peek(SessionKey.PdfSamplePageModel);
                if (sessionPage != null)
                {
                    var savedPage = System.Text.Json.JsonSerializer.Deserialize<SearchModelBase>(sessionPage.ToString()!)!;
                    model.Page = savedPage.Page;
                    model.Sort = savedPage.Sort;
                    model.SortDir = savedPage.SortDir;
                    model.RecordNum = savedPage.RecordNum;
                }
            }

            if (IsAjaxRequest())
                model.PageRead = PageRead.Paging;
        }

        model = _service.GetInvoiceList(model);
        SaveSearchState(model);
        return View(model);
    }

    /// <summary>
    /// 検索条件の POST と Ajax 部分更新に対応する。
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(InvoiceViewModel model)
    {
        model = _service.GetInvoiceList(model);
        SaveSearchState(model);

        if (IsAjaxRequest())
            return PartialView("_IndexPartial", model);
        return View(model);
    }

    /// <summary>
    /// 請求書詳細を表示する。
    /// </summary>
    [HttpGet]
    public IActionResult Details(long? id)
    {
        if (id == null) return BadRequest();
        var model = _service.GetInvoiceDetail(id.Value);
        if (model == null) return NotFound();
        return View(model);
    }

    /// <summary>
    /// 新規作成フォームを表示する。
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        return View("Edit", new InvoiceDetailViewModel());
    }

    /// <summary>
    /// 請求書を新規登録する。
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(InvoiceDetailViewModel model)
    {
        NormalizeItems(model);
        ValidateItems(model);

        if (!ModelState.IsValid)
            return View("Edit", model);

        _service.InsertInvoice(model, User.Identity?.Name);
        TempData[SessionKey.Message] = LocalUtil.GetCreateAlertMessage("請求書");
        return RedirectToAction(nameof(Index), new { returnList = true });
    }

    /// <summary>
    /// 編集フォームを表示する。
    /// </summary>
    [HttpGet]
    public IActionResult Edit(long? id)
    {
        if (id == null) return BadRequest();
        var model = _service.GetInvoiceDetail(id.Value);
        if (model == null) return NotFound();
        if (model.Items.Count == 0) model.Items.Add(new InvoiceItemViewModel());
        return View(model);
    }

    /// <summary>
    /// 請求書を更新する。
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(InvoiceDetailViewModel model)
    {
        NormalizeItems(model);
        ValidateItems(model);

        if (!ModelState.IsValid)
            return View(model);

        _service.UpdateInvoice(model);
        TempData[SessionKey.Message] = LocalUtil.GetUpdateAlertMessage("請求書");
        return RedirectToAction(nameof(Index), new { returnList = true });
    }

    /// <summary>
    /// 削除確認画面を表示する。
    /// </summary>
    [HttpGet]
    public IActionResult Delete(long? id)
    {
        if (id == null) return BadRequest();
        var model = _service.GetInvoiceDetail(id.Value);
        if (model == null) return NotFound();
        return View(model);
    }

    /// <summary>
    /// 請求書を論理削除する。
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(long id)
    {
        _service.DeleteInvoice(id);
        TempData[SessionKey.Message] = LocalUtil.GetDeleteAlertMessage("請求書");
        return RedirectToAction(nameof(Index), new { returnList = true });
    }

    /// <summary>
    /// 単一請求書の PDF をダウンロードする。
    /// </summary>
    [HttpGet]
    public IActionResult DownloadPdf(long? id)
    {
        if (id == null) return BadRequest();
        var stream = _service.ExportPdf(id.Value);
        if (stream == null) return NotFound();
        return File(stream, "application/pdf", $"請求書_{id}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
    }

    /// <summary>
    /// 選択済み請求書を ZIP 形式で一括ダウンロードする。
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DownloadZip(InvoiceViewModel model)
    {
        if (model.SelectedIds == null || model.SelectedIds.Count == 0)
        {
            TempData[SessionKey.Message] = "請求書を1件以上選択してください。";
            return RedirectToAction(nameof(Index), new { returnList = true });
        }

        var stream = _service.ExportPdfBulk(model.SelectedIds);
        return File(stream, "application/zip", $"請求書一括_{DateTime.Now:yyyyMMddHHmmss}.zip");
    }

    private static void NormalizeItems(InvoiceDetailViewModel model)
    {
        model.Items ??= [];
        model.Items = model.Items
            .Where(x => !string.IsNullOrWhiteSpace(x.Description) || x.Quantity != 0 || x.UnitPrice != 0)
            .ToList();
        if (model.Items.Count == 0)
            model.Items.Add(new InvoiceItemViewModel());
    }

    private void ValidateItems(InvoiceDetailViewModel model)
    {
        if (!model.Items.Any(x => !string.IsNullOrWhiteSpace(x.Description)))
            ModelState.AddModelError(string.Empty, "明細を1件以上入力してください。");
    }

    private void SaveSearchState(InvoiceViewModel model)
    {
        TempData[SessionKey.PdfSampleCondViewModel] = System.Text.Json.JsonSerializer.Serialize(model.Cond);
        TempData[SessionKey.PdfSamplePageModel] = System.Text.Json.JsonSerializer.Serialize(
            new SearchModelBase { Page = model.Page, Sort = model.Sort, SortDir = model.SortDir, RecordNum = model.RecordNum });
    }

    private bool IsAjaxRequest() => Request.Headers["X-Requested-With"] == "XMLHttpRequest";
}
