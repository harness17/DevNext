using ExcelSample.Common;
using ExcelSample.Data;
using ExcelSample.Models;
using ExcelSample.Service;
using Dev.CommonLibrary.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExcelSample.Controllers
{
    // ポイント: [Authorize] をコントローラークラスに付けることで全アクションにログイン必須を適用
    [Authorize]
    // ポイント: [ServiceFilter] はDIコンテナ経由でフィルターインスタンスを生成する
    //           使用するには Program.cs で AddScoped<AccessLogAttribute>() 登録が必要
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class ExcelSampleController : Controller
    {
        private readonly ExcelSampleDbContext _db;
        private readonly ExcelSampleService _service;

        public ExcelSampleController(ExcelSampleDbContext db, ExcelSampleService service)
        {
            _db = db;
            _service = service;
        }

        // ─────────────────────────────────────────────
        // 一覧
        // ─────────────────────────────────────────────

        [HttpGet]
        public IActionResult Index()
        {
            var model = new ExcelSampleViewModel();
            model = _service.GetItemList(model);
            return View(model);
        }

        // ─────────────────────────────────────────────
        // 削除
        // ─────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(long id)
        {
            _service.DeleteItem(id, User.Identity?.Name);
            TempData["Message"] = "削除しました。";
            // ポイント: PRG パターン（Post-Redirect-Get）で二重送信を防ぐ
            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────
        // エクスポート（Excel ダウンロード）
        // ─────────────────────────────────────────────

        [HttpGet]
        public IActionResult Export()
        {
            var stream = _service.ExportExcel();
            var fileName = $"商品一覧_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            // ポイント: FileStreamResult で Excel バイナリを直接レスポンスとして返す
            //           ContentType は xlsx の MIME タイプを指定する
            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // ─────────────────────────────────────────────
        // エクスポート（CSV ダウンロード）
        // ─────────────────────────────────────────────

        [HttpGet]
        public IActionResult ExportCsv()
        {
            var stream = _service.ExportCsv();
            var fileName = $"商品一覧_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            // ポイント: ContentType は text/csv を指定する。BOM 付き UTF-8 なので Excel でも文字化けしない
            return File(stream, "text/csv", fileName);
        }

        // ─────────────────────────────────────────────
        // インポート（CSV アップロード）
        // ─────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ImportCsv(ExcelSampleViewModel model)
        {
            if (model.CsvImportFile == null || model.CsvImportFile.Length == 0)
            {
                ModelState.AddModelError(nameof(model.CsvImportFile), "ファイルを選択してください。");
                model = _service.GetItemList(model);
                return View("Index", model);
            }

            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (model.CsvImportFile.Length > maxFileSize)
            {
                ModelState.AddModelError(nameof(model.CsvImportFile), "ファイルサイズは 5MB 以内にしてください。");
                model = _service.GetItemList(model);
                return View("Index", model);
            }

            // ポイント: ブラウザが送るCSVのMIMEタイプはOSによって異なるため、
            //           text/csv・application/csv・text/plain・application/octet-stream を許容する
            var allowedMimes = new[] { "text/csv", "application/csv", "text/plain", "application/octet-stream" };
            if (!allowedMimes.Contains(model.CsvImportFile.ContentType))
            {
                ModelState.AddModelError(nameof(model.CsvImportFile), "csv ファイルを選択してください。");
                model = _service.GetItemList(model);
                return View("Index", model);
            }

            var (successCount, errors) = _service.ImportCsv(
                model.CsvImportFile.OpenReadStream(), User.Identity?.Name);

            model.CsvImportErrors = errors;
            model.CsvImportSuccessCount = successCount;
            model = _service.GetItemList(model);

            if (errors.Count == 0)
                TempData["Message"] = $"{successCount}件のデータを登録しました。";

            return View("Index", model);
        }

        // ─────────────────────────────────────────────
        // インポート（Excel アップロード）
        // ─────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Import(ExcelSampleViewModel model)
        {
            if (model.ImportFile == null || model.ImportFile.Length == 0)
            {
                ModelState.AddModelError(nameof(model.ImportFile), "ファイルを選択してください。");
                model = _service.GetItemList(model);
                return View("Index", model);
            }

            // ポイント: ファイルサイズ・MIME タイプを検証してサーバーへの悪意ある入力を防ぐ
            //           5MB 超のファイルや xlsx 以外の MIME タイプは即エラーとする
            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (model.ImportFile.Length > maxFileSize)
            {
                ModelState.AddModelError(nameof(model.ImportFile), "ファイルサイズは 5MB 以内にしてください。");
                model = _service.GetItemList(model);
                return View("Index", model);
            }
            const string allowedMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            if (model.ImportFile.ContentType != allowedMime)
            {
                ModelState.AddModelError(nameof(model.ImportFile), "xlsx ファイルを選択してください。");
                model = _service.GetItemList(model);
                return View("Index", model);
            }

            // ポイント: OpenReadStream() でファイルストリームをサービスに渡す
            //           using はサービス内で管理するため呼び出し側では閉じない
            var (successCount, errors) = _service.ImportExcel(
                model.ImportFile.OpenReadStream(), User.Identity?.Name);

            model.ImportErrors = errors;
            model.ImportSuccessCount = successCount;
            model = _service.GetItemList(model);

            if (errors.Count == 0)
                TempData["Message"] = $"{successCount}件のデータを登録しました。";

            return View("Index", model);
        }
    }
}
