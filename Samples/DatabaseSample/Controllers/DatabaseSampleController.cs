using DatabaseSample.Common;
using DatabaseSample.Data;
using DatabaseSample.Models;
using DatabaseSample.Service;
using Dev.CommonLibrary.Attributes;
using Dev.CommonLibrary.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseSample.Controllers
{
    // ポイント: [Authorize] をコントローラークラスに付けることで全アクションにログイン必須を適用
    //           アクション単位で除外したい場合は [AllowAnonymous] を個別に付ける
    [Authorize]
    // ポイント: [ServiceFilter] はDIコンテナ経由でフィルターインスタンスを生成する
    //           通常の [TypeFilter] と異なり、フィルタークラス自身をDI対象にできる（内部でサービス注入可能）
    //           使用するには Program.cs で AddScoped<AccessLogAttribute>() 登録が必要
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class DatabaseSampleController : Controller
    {
        private readonly DatabaseSampleDbContext _db;
        private readonly DatabaseSampleService _workerService;
        private readonly RazorViewToStringRenderer _razorRenderer;
        private readonly PlaywrightPdfService _pdfService;
        private readonly IWebHostEnvironment _env;

        // ポイント: コンストラクタインジェクション
        //           Program.cs に登録済みのサービスをフレームワークが自動解決して渡す
        public DatabaseSampleController(
            DatabaseSampleDbContext db,
            DatabaseSampleService workerService,
            RazorViewToStringRenderer razorRenderer,
            PlaywrightPdfService pdfService,
            IWebHostEnvironment env)
        {
            _db = db;
            _workerService = workerService;
            _razorRenderer = razorRenderer;
            _pdfService = pdfService;
            _env = env;
        }

        // ポイント: GETとPOSTで同名アクション "Index" を使い分ける
        //           GET はURL直打ち・ページング・ソートからの遷移に対応
        //           pageModel にはクエリパラメータで Page/Sort/SortDir/RecordNum/PageRead が渡される
        //           returnList=true のとき（編集・削除後の一覧復帰）は TempData から検索・ページ状態を復元する
        [HttpGet]
        public IActionResult Index(SearchModelBase? pageModel = null, bool returnList = false)
        {
            // SearchModelBase のプロパティを DatabaseSampleViewModel へコピーする共通ユーティリティ
            var model = LocalUtil.MapPageModelTo<DatabaseSampleViewModel>(pageModel);

            // ページング・ソート変更時、Ajax リクエスト時、または一覧復帰時は
            // TempData に保存済みの検索条件を復元して同じ条件で再取得する
            if (model.PageRead != null || IsAjaxRequest() || returnList)
            {
                // TempData.Peek: 読み出してもキーを消さない（次のリクエストでも保持される）
                // TempData[key] で読み出すと次回以降使えなくなるため Peek を使う
                var sessionCond = TempData.Peek(SessionKey.DatabaseSampleCondViewModel);
                if (sessionCond != null)
                    model.Cond = System.Text.Json.JsonSerializer.Deserialize<DatabaseSampleCondViewModel>(sessionCond.ToString()!)!;

                // ポイント: 一覧復帰時はページ番号・ソート状態も TempData から復元する
                //           ページング・ソート操作時はURLパラメータ側の値を優先するため復元しない
                if (returnList)
                {
                    var sessionPage = TempData.Peek(SessionKey.DatabaseSamplePageModel);
                    if (sessionPage != null)
                    {
                        var savedPage = System.Text.Json.JsonSerializer.Deserialize<SearchModelBase>(sessionPage.ToString()!)!;
                        model.Page      = savedPage.Page;
                        model.Sort      = savedPage.Sort;
                        model.SortDir   = savedPage.SortDir;
                        model.RecordNum = savedPage.RecordNum;
                    }
                }

                // Ajax からのページング時は PageRead を上書き
                if (IsAjaxRequest())
                    model.PageRead = PageRead.Paging;
            }

            model = _workerService.GetSampleEntityList(model);

            // 検索条件を JSON シリアライズして TempData に保存
            // ページング・ソート時に検索条件を引き継ぐためのセッション代替手段
            TempData[SessionKey.DatabaseSampleCondViewModel] = System.Text.Json.JsonSerializer.Serialize(model.Cond);
            // ポイント: ページ・ソート状態も TempData に保存する
            //           一覧復帰時（returnList=true）に同じページ位置を再現するために使用する
            TempData[SessionKey.DatabaseSamplePageModel] = System.Text.Json.JsonSerializer.Serialize(
                new SearchModelBase { Page = model.Page, Sort = model.Sort, SortDir = model.SortDir, RecordNum = model.RecordNum });

            return View(model);
        }

        // ポイント: POST は検索ボタン押下時（Ajax）に対応
        //           Ajax リクエストの場合はパーシャルビューのみ返して #SearchResult div を差し替える
        //           非 Ajax の場合はフルページを返す（JS 無効環境での互換性確保）
        [HttpPost]
        [ValidateAntiForgeryToken]  // ポイント: CSRF 対策。フォームに @Html.AntiForgeryToken() が必要
        public IActionResult Index(DatabaseSampleViewModel model)
        {
            model = _workerService.GetSampleEntityList(model);
            TempData[SessionKey.DatabaseSampleCondViewModel] = System.Text.Json.JsonSerializer.Serialize(model.Cond);
            // ポイント: POST 検索後もページ・ソート状態を更新して一覧復帰時に反映させる
            TempData[SessionKey.DatabaseSamplePageModel] = System.Text.Json.JsonSerializer.Serialize(
                new SearchModelBase { Page = model.Page, Sort = model.Sort, SortDir = model.SortDir, RecordNum = model.RecordNum });

            // IsAjaxRequest() で X-Requested-With ヘッダーを確認
            // jQuery の $.ajax() / fetch API は自動的にこのヘッダーを付与する
            if (IsAjaxRequest())
                return PartialView("_IndexPartial", model);  // パーシャルビューのみ返す
            return View(model);
        }

        // ポイント: 親エンティティの詳細 + 子エンティティ一覧を表示する
        //           サービス層で親+子を一括取得し DatabaseSampleDetailsViewModel に詰めて返す
        public IActionResult Details(int? id)
        {
            if (id == null) return BadRequest();
            var model = _workerService.GetParentDetails((long)id);
            if (model == null) return NotFound();
            return View(model);
        }

        // ポイント: Create と Edit で同一ビュー (Edit.cshtml) を使い回す
        //           Model.Id が null なら新規作成、値があれば編集としてビュー側で判定する
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
                // User.Identity?.Name でログイン中のユーザー名を取得して作成者として記録する
                _workerService.InsSampleEntity(sampleEntity, User.Identity?.Name);
                // ポイント: PRG パターン (Post-Redirect-Get)
                //           POST 後に RedirectToAction でリダイレクトすることで
                //           F5 リロードによる二重送信を防ぐ
                return RedirectToAction("Index", new { returnList = true });
            }
            return View(sampleEntity);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null) return BadRequest();
            // サービス層で Entity → ViewModel へのマッピングを行う
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
                // IWebHostEnvironment はファイルアップロード先の物理パス解決に使用
                _workerService.UpdSampleEntity(sampleEntity, _env);
                return RedirectToAction("Index", new { returnList = true });
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

        // ポイント: [ActionName("Delete")] でアクション名を "Delete" に統一しつつ
        //           メソッド名を DeleteConfirmed にして C# のシグネチャ重複エラーを回避する
        //           GET "Delete"（確認画面）と POST "Delete"（実行）を共存させるパターン
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            // 論理削除: データを物理削除せず DelFlag を立てる（サービス内で実装）
            _workerService.DelSampleEntity(id);
            return RedirectToAction("Index", new { returnList = true });
        }

        // ポイント: AutoMapper の前付け名・後付け名マッピング (RecognizeDestinationPrefixes / Postfixes) のサンプル
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
                // ポイント: ref 引数でモデルを渡すことでサービス内でエラーリストを書き込める
                _workerService.InsertFile(ref model);
                // エラーがなければ成功メッセージを TempData にセット（次リクエストで表示）
                if (model.ImportErrList.Count == 0)
                    TempData[SessionKey.Message] = LocalUtil.GetUpdateAlertMessage("サンプルエンティティ");
            }
            return View("ImportFile", model);
        }

        // ポイント: ファイルダウンロードは MemoryStream で生成して File() で返す
        //           サーバーにファイルを保存せずにストリームを直接レスポンスに流せる
        //           ContentType を application/vnd.openxmlformats... にすることで xlsx として認識される
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
        public async Task<IActionResult> PdfOutput(int? id)
        {
            if (id == null) return BadRequest();
            var pdfModel = _workerService.GetPdfDetail(id.Value, _env);
            if (pdfModel == null) return NotFound();

            var pdf = await GeneratePdfAsync("DatabaseSample/PrintDetail", pdfModel);
            string fileName = $"SampleEntity_{id}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            SetPrivateNoStoreCache();
            return File(pdf, "application/pdf", fileName);
        }

        /// <summary>
        /// 検索結果をPDFとしてダウンロードする
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PdfDownload(DatabaseSampleViewModel model)
        {
            var fromdate = DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = $"ExportFile_{fromdate}.pdf";
            var pdfModel = _workerService.GetPdfList(model);
            var pdf = await GeneratePdfAsync("DatabaseSample/PrintList", pdfModel);
            SetPrivateNoStoreCache();
            return File(pdf, "application/pdf", fileName);
        }

        // ─────────────────────────────────────────────
        // 一括登録・編集
        // ─────────────────────────────────────────────

        // ポイント: Create と BulkCreate で同一ビュー (BulkEdit.cshtml) を使い回す
        //           Model.Id が null なら新規登録として扱う
        public IActionResult BulkCreate()
        {
            return View("BulkEdit", new DatabaseSampleBulkEditViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BulkCreate(DatabaseSampleBulkEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                _workerService.BulkInsert(model, User.Identity?.Name);
                TempData[SessionKey.Message] = LocalUtil.GetCreateAlertMessage("エンティティ（一括）");
                return RedirectToAction("Index", new { returnList = true });
            }
            return View("BulkEdit", model);
        }

        public IActionResult BulkEdit(int? id)
        {
            if (id == null) return BadRequest();
            var model = _workerService.GetBulkEditModel((long)id);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BulkEdit(DatabaseSampleBulkEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                _workerService.BulkUpdate(model);
                TempData[SessionKey.Message] = LocalUtil.GetUpdateAlertMessage("エンティティ（一括）");
                return RedirectToAction("Details", new { id = model.Id });
            }
            return View(model);
        }

        // ─────────────────────────────────────────────
        // 子エンティティ 新規作成
        // ─────────────────────────────────────────────

        // ポイント: 子作成時は必ず親IDをクエリパラメータで渡して FK を設定できるようにする
        public IActionResult ChildCreate(int? parentId)
        {
            if (parentId == null) return BadRequest();
            var model = new DatabaseSampleChildEditViewModel { ParentId = parentId.Value };
            return View("ChildEdit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChildCreate(DatabaseSampleChildEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                _workerService.InsChild(model);
                TempData[SessionKey.Message] = LocalUtil.GetCreateAlertMessage("子エンティティ");
                // ポイント: 子の操作後は親の詳細ページへリダイレクトして子一覧を確認できるようにする
                return RedirectToAction("Details", new { id = model.ParentId });
            }
            return View("ChildEdit", model);
        }

        // ─────────────────────────────────────────────
        // 子エンティティ 編集
        // ─────────────────────────────────────────────

        public IActionResult ChildEdit(int? id)
        {
            if (id == null) return BadRequest();
            var model = _workerService.GetChildEditModel(id.Value);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChildEdit(DatabaseSampleChildEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                _workerService.UpdChild(model);
                TempData[SessionKey.Message] = LocalUtil.GetUpdateAlertMessage("子エンティティ");
                return RedirectToAction("Details", new { id = model.ParentId });
            }
            return View(model);
        }

        // ─────────────────────────────────────────────
        // 子エンティティ 削除
        // ─────────────────────────────────────────────

        public IActionResult ChildDelete(int? id)
        {
            if (id == null) return BadRequest();
            var model = _workerService.GetChildEditModel(id.Value);
            if (model == null) return NotFound();
            return View(model);
        }

        // ポイント: [ActionName("ChildDelete")] でアクション名を統一しつつ
        //           メソッド名を ChildDeleteConfirmed にして C# のシグネチャ重複エラーを回避する
        [HttpPost, ActionName("ChildDelete")]
        [ValidateAntiForgeryToken]
        public IActionResult ChildDeleteConfirmed(int id, long parentId)
        {
            _workerService.DelChild(id);
            TempData[SessionKey.Message] = LocalUtil.GetDeleteAlertMessage("子エンティティ");
            return RedirectToAction("Details", new { id = parentId });
        }

        // ポイント: Ajax リクエストの判定ヘルパー
        //           jQuery の $.ajax() は X-Requested-With: XMLHttpRequest ヘッダーを自動付与する
        //           このヘッダーを見ることで通常リクエストと Ajax を区別できる
        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        private async Task<byte[]> GeneratePdfAsync(string viewName, object model)
        {
            var html = await _razorRenderer.RenderAsync(ControllerContext, viewName, model);
            return await _pdfService.GenerateFromHtmlAsync(html);
        }

        private void SetPrivateNoStoreCache()
        {
            Response.Headers.CacheControl = "no-store, private";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
        }
    }
}
