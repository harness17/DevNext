using Dev.CommonLibrary.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Site.Common;
using Site.Entity;
using Site.Models;
using Site.Service;

namespace Site.Controllers
{
    [Authorize]
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class DatabaseSampleController : Controller
    {
        private readonly DBContext _db;
        private readonly DatabaseSampleService _workerService;
        private readonly IWebHostEnvironment _env;

        public DatabaseSampleController(DBContext db, DatabaseSampleService workerService, IWebHostEnvironment env)
        {
            _db = db;
            _workerService = workerService;
            _env = env;
        }

        [HttpGet]
        public IActionResult Index(SearchModelBase? pageModel = null)
        {
            var model = LocalUtil.MapPageModelTo<DatabaseSampleViewModel>(pageModel);

            if (model.PageRead != null || IsAjaxRequest())
            {
                var sessionCond = TempData.Peek(SessionKey.DatabaseSampleCondViewModel);
                if (sessionCond != null)
                    model.Cond = System.Text.Json.JsonSerializer.Deserialize<DatabaseSampleCondViewModel>(sessionCond.ToString()!)!;

                if (IsAjaxRequest())
                    model.PageRead = PageRead.Paging;
            }

            model = _workerService.GetSampleEntityList(model);
            TempData[SessionKey.DatabaseSampleCondViewModel] = System.Text.Json.JsonSerializer.Serialize(model.Cond);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(DatabaseSampleViewModel model)
        {
            model = _workerService.GetSampleEntityList(model);
            TempData[SessionKey.DatabaseSampleCondViewModel] = System.Text.Json.JsonSerializer.Serialize(model.Cond);

            if (IsAjaxRequest())
                return PartialView("_IndexPartial", model);
            return View(model);
        }

        public IActionResult Details(int? id)
        {
            if (id == null) return BadRequest();
            var sampleEntity = _db.SampleEntity.Find((long)id);
            if (sampleEntity == null) return NotFound();
            return View(sampleEntity);
        }

        public IActionResult Create()
        {
            return View("Edit", new DatabaseSampleDetailViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(DatabaseSampleDetailViewModel sampleEntity)
        {
            if (ModelState.IsValid)
            {
                _workerService.InsSampleEntity(sampleEntity, User.Identity?.Name);
                return RedirectToAction("Index");
            }
            return View(sampleEntity);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null) return BadRequest();
            var sampleEntity = _workerService.GetSampleEntity(id.Value);
            if (sampleEntity == null) return NotFound();
            return View(sampleEntity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(DatabaseSampleDetailViewModel sampleEntity)
        {
            if (ModelState.IsValid)
            {
                _workerService.UpdSampleEntity(sampleEntity, _env);
                return RedirectToAction("Index");
            }
            return View(sampleEntity);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null) return BadRequest();
            var sampleEntity = _db.SampleEntity.Find((long)id);
            if (sampleEntity == null) return NotFound();
            return View(sampleEntity);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _workerService.DelSampleEntity(id);
            return RedirectToAction("Index");
        }

        public IActionResult MapperUsage()
        {
            var model = _workerService.GetMapperUsage();
            return View("MapperUsage", model);
        }

        [HttpGet]
        public IActionResult ImportFile()
        {
            return View("ImportFile", new DatabaseSampleImportViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ImportFile(DatabaseSampleImportViewModel model)
        {
            if (ModelState.IsValid)
            {
                _workerService.InsertFile(ref model);
                if (model.ImportErrList.Count == 0)
                    TempData[SessionKey.Message] = LocalUtil.GetUpdateAlertMessage("サンプルエンティティ");
            }
            return View("ImportFile", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExportExcelFile(DatabaseSampleViewModel model)
        {
            var fromdate = DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = $"ExportFile_{fromdate}.xlsx";
            var memorystream = _workerService.ExportFile(model, _env);
            return File(memorystream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// 単体データをPDFとしてダウンロードする
        /// </summary>
        public IActionResult PdfOutput(int? id)
        {
            if (id == null) return BadRequest();
            var memorystream = _workerService.ExportPdfSingle(id.Value, _env);
            if (memorystream == null) return NotFound();
            string fileName = $"SampleEntity_{id}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            return File(memorystream, "application/pdf", fileName);
        }

        /// <summary>
        /// 検索結果をPDFとしてダウンロードする
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PdfDownload(DatabaseSampleViewModel model)
        {
            var fromdate = DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = $"ExportFile_{fromdate}.pdf";
            var memorystream = _workerService.ExportPdf(model);
            return File(memorystream, "application/pdf", fileName);
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}
