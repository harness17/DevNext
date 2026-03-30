using Dev.CommonLibrary.Attributes;
using FileSample.Common;
using FileSample.Models;
using FileSample.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileSample.Controllers
{
    /// <summary>
    /// ファイル管理コントローラー
    /// ファイルのアップロード・一覧表示・ダウンロード・削除のサンプル
    /// </summary>
    [Authorize]
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class FileManagementController : Controller
    {
        private readonly FileManagementService _fileService;
        private readonly IWebHostEnvironment _env;

        public FileManagementController(FileManagementService fileService, IWebHostEnvironment env)
        {
            _fileService = fileService;
            _env = env;
        }

        /// <summary>
        /// ファイル一覧画面（初期表示・ページ遷移時）
        /// </summary>
        [HttpGet]
        public IActionResult Index(SearchModelBase? pageModel = null)
        {
            var model = LocalUtil.MapPageModelTo<FileManagementViewModel>(pageModel);

            if (model.PageRead != null || IsAjaxRequest())
            {
                var sessionCond = TempData.Peek(SessionKey.FileManagementCondViewModel);
                if (sessionCond != null)
                    model.Cond = System.Text.Json.JsonSerializer.Deserialize<FileManagementCondViewModel>(sessionCond.ToString()!)!;

                if (IsAjaxRequest())
                    model.PageRead = PageRead.Paging;
            }

            model = _fileService.GetFileList(model);
            TempData[SessionKey.FileManagementCondViewModel] = System.Text.Json.JsonSerializer.Serialize(model.Cond);

            return View(model);
        }

        /// <summary>
        /// ファイル一覧画面（検索フォーム POST）
        /// Ajax リクエスト時はパーシャルビューを返す
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(FileManagementViewModel model)
        {
            model = _fileService.GetFileList(model);
            TempData[SessionKey.FileManagementCondViewModel] = System.Text.Json.JsonSerializer.Serialize(model.Cond);

            if (IsAjaxRequest())
                return PartialView("_IndexPartial", model);
            return View(model);
        }

        /// <summary>
        /// ファイルアップロード処理
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upload(FileUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData[SessionKey.Message] = "ファイルを選択してください。";
                return RedirectToAction(nameof(Index));
            }

            var error = _fileService.UploadFile(model.UploadFile!, model.Description, _env);
            TempData[SessionKey.Message] = error ?? LocalUtil.GetCreateAlertMessage("ファイル");
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// ファイルダウンロード
        /// </summary>
        public IActionResult Download(long? id)
        {
            if (id == null) return BadRequest();

            var result = _fileService.GetForDownload(id.Value, _env);
            if (result == null) return NotFound();

            var (entity, filePath) = result.Value;
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, entity.ContentType, entity.OriginalFileName);
        }

        /// <summary>
        /// 削除確認画面
        /// </summary>
        public IActionResult Delete(long? id)
        {
            if (id == null) return BadRequest();
            var entity = _fileService.GetById(id.Value);
            if (entity == null) return NotFound();
            return View(entity);
        }

        /// <summary>
        /// 削除実行（論理削除 + 物理ファイル削除）
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(long id)
        {
            _fileService.DeleteFile(id, _env);
            TempData[SessionKey.Message] = LocalUtil.GetDeleteAlertMessage("ファイル");
            return RedirectToAction(nameof(Index));
        }

        private bool IsAjaxRequest() =>
            Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}
