using Dev.CommonLibrary.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Site.Common;
using Site.Entity;
using Site.Models;
using Site.Service;

namespace Site.Controllers
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
        private readonly DBContext _db;
        private readonly DatabaseSampleService _workerService;
        private readonly IWebHostEnvironment _env;

        // ポイント: コンストラクタインジェクション
        //           Program.cs に登録済みのサービスをフレームワークが自動解決して渡す
        public DatabaseSampleController(DBContext db, DatabaseSampleService workerService, IWebHostEnvironment env)
        {
            _db = db;
            _workerService = workerService;
            _env = env;
        }

        // ポイント: GETとPOSTで同名アクション "Index" を使い分ける
        //           GET はURL直打ち・ページング・ソートからの遷移に対応
        //           pageModel にはクエリパラメータで Page/Sort/SortDir/RecordNum/PageRead が渡される
        [HttpGet]
        public IActionResult Index(SearchModelBase? pageModel = null)
        {
            // SearchModelBase のプロパティを DatabaseSampleViewModel へコピーする共通ユーティリティ
            var model = LocalUtil.MapPageModelTo<DatabaseSampleViewModel>(pageModel);

            // ページング・ソート変更時、または Ajax リクエスト時は
            // TempData に保存済みの検索条件を復元して同じ条件で再取得する
            if (model.PageRead != null || IsAjaxRequest())
            {
                // TempData.Peek: 読み出してもキーを消さない（次のリクエストでも保持される）
                // TempData[key] で読み出すと次回以降使えなくなるため Peek を使う
                var sessionCond = TempData.Peek(SessionKey.DatabaseSampleCondViewModel);
                if (sessionCond != null)
                    model.Cond = System.Text.Json.JsonSerializer.Deserialize<DatabaseSampleCondViewModel>(sessionCond.ToString()!)!;

                // Ajax からのページング時は PageRead を上書き
                if (IsAjaxRequest())
                    model.PageRead = PageRead.Paging;
            }

            model = _workerService.GetSampleEntityList(model);
            // 検索条件を JSON シリアライズして TempData に保存
            // ページング・ソート時に検索条件を引き継ぐためのセッション代替手段
            TempData[SessionKey.DatabaseSampleCondViewModel] = System.Text.Json.JsonSerializer.Serialize(model.Cond);

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

            // IsAjaxRequest() で X-Requested-With ヘッダーを確認
            // jQuery の $.ajax() / fetch API は自動的にこのヘッダーを付与する
            if (IsAjaxRequest())
                return PartialView("_IndexPartial", model);  // パーシャルビューのみ返す
            return View(model);
        }

        // ポイント: id が null → 400 BadRequest、DB に存在しない → 404 NotFound を返す
        //           Entity を直接Viewに渡すシンプルな詳細表示パターン
        public IActionResult Details(int? id)
        {
            if (id == null) return BadRequest();
            var sampleEntity = _db.SampleEntity.Find((long)id);
            if (sampleEntity == null) return NotFound();
            return View(sampleEntity);
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
                return RedirectToAction("Index");
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

        // ポイント: [ActionName("Delete")] でアクション名を "Delete" に統一しつつ
        //           メソッド名を DeleteConfirmed にして C# のシグネチャ重複エラーを回避する
        //           GET "Delete"（確認画面）と POST "Delete"（実行）を共存させるパターン
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            // 論理削除: データを物理削除せず DelFlag を立てる（サービス内で実装）
            _workerService.DelSampleEntity(id);
            return RedirectToAction("Index");
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

        // ポイント: Ajax リクエストの判定ヘルパー
        //           jQuery の $.ajax() は X-Requested-With: XMLHttpRequest ヘッダーを自動付与する
        //           このヘッダーを見ることで通常リクエストと Ajax を区別できる
        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}
