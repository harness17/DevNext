using Dev.CommonLibrary.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Site.Common;
using Site.Models;
using Site.Service;
using System.Text.Json;

namespace Site.Controllers
{
    /// <summary>
    /// 多段階フォーム（ウィザード）サンプルコントローラー
    ///
    /// 【フロー】
    ///   Step1（基本情報）→ Step2（詳細情報）→ Step3（確認）→ Complete（完了）
    ///
    /// 【状態管理】
    ///   TempData["WizardSession"] に WizardSessionModel を JSON で保存する。
    ///   各 GET では TempData.Peek() で読み取り（データを保持）、
    ///   各 POST では TempData に新しい値を上書きする。
    ///   最終確定 POST（Step3 POST）でのみ DB 保存し、TempData を削除する。
    /// </summary>
    [Authorize]
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class WizardSampleController : Controller
    {
        private readonly WizardSampleService _wizardService;

        public WizardSampleController(WizardSampleService wizardService)
        {
            _wizardService = wizardService;
        }

        // ─── 登録データ一覧 ────────────────────────────────────────

        /// <summary>
        /// 登録データ一覧表示。ページング・ソート変更時は TempData から検索条件を復元する
        /// </summary>
        [HttpGet]
        public IActionResult Index(SearchModelBase? pageModel = null)
        {
            var model = LocalUtil.MapPageModelTo<WizardSampleListViewModel>(pageModel);

            // ページング・ソート変更時、または Ajax 時は保存済み検索条件を復元する
            if (model.PageRead != null || IsAjaxRequest())
            {
                var sessionCond = TempData.Peek(SessionKey.WizardSampleCondViewModel);
                if (sessionCond != null)
                    model.Cond = System.Text.Json.JsonSerializer.Deserialize<WizardSampleCondViewModel>(sessionCond.ToString()!)!;

                if (IsAjaxRequest())
                    model.PageRead = PageRead.Paging;
            }

            model = _wizardService.GetWizardEntityList(model);
            TempData[SessionKey.WizardSampleCondViewModel] = System.Text.Json.JsonSerializer.Serialize(model.Cond);

            return View(model);
        }

        /// <summary>
        /// 登録データ一覧（検索 POST）。Ajax 時はパーシャルビューのみ返す
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(WizardSampleListViewModel model)
        {
            model = _wizardService.GetWizardEntityList(model);
            TempData[SessionKey.WizardSampleCondViewModel] = System.Text.Json.JsonSerializer.Serialize(model.Cond);

            if (IsAjaxRequest())
                return PartialView("_IndexPartial", model);
            return View(model);
        }

        // ─── Step 1: 基本情報 ────────────────────────────────────

        /// <summary>Step 1 表示。既存セッションがあれば値を復元する</summary>
        [HttpGet]
        public IActionResult Step1()
        {
            var session = GetSession();
            var model = new WizardStep1ViewModel
            {
                Name  = session?.Name  ?? "",
                Email = session?.Email ?? "",
                Phone = session?.Phone
            };
            return View(model);
        }

        /// <summary>Step 1 送信。バリデーション後にセッションへ保存して Step 2 へ遷移</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step1(WizardStep1ViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // 既存セッションを引き継ぎ、Step 1 の値だけ更新する
            var session = GetSession() ?? new WizardSessionModel();
            session.Name  = model.Name;
            session.Email = model.Email;
            session.Phone = model.Phone;
            SaveSession(session);

            return RedirectToAction(nameof(Step2));
        }

        // ─── Step 2: 詳細情報 ────────────────────────────────────

        /// <summary>Step 2 表示。セッションがなければ Step 1 へリダイレクト</summary>
        [HttpGet]
        public IActionResult Step2()
        {
            var session = GetSession();
            if (session == null) return RedirectToAction(nameof(Step1));

            var model = new WizardStep2ViewModel
            {
                Subject     = session.Subject,
                Content     = session.Content,
                Category    = session.Category,
                DesiredDate = session.DesiredDate
            };
            return View(model);
        }

        /// <summary>Step 2 送信。バリデーション後にセッションへ保存して Step 3 へ遷移</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step2(WizardStep2ViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var session = GetSession() ?? new WizardSessionModel();
            session.Subject     = model.Subject;
            session.Content     = model.Content;
            session.Category    = model.Category;
            session.DesiredDate = model.DesiredDate;
            SaveSession(session);

            return RedirectToAction(nameof(Step3));
        }

        // ─── Step 3: 確認 ────────────────────────────────────────

        /// <summary>Step 3（確認画面）表示。セッションがなければ Step 1 へリダイレクト</summary>
        [HttpGet]
        public IActionResult Step3()
        {
            var session = GetSession();
            if (session == null) return RedirectToAction(nameof(Step1));
            return View(session);
        }

        /// <summary>Step 3 確定。DB に保存してセッションをクリアし、完了ページへ遷移</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step3(WizardSessionModel model)
        {
            var session = GetSession();
            if (session == null) return RedirectToAction(nameof(Step1));

            _wizardService.SaveWizardData(session);

            // セッションを明示的に削除してウィザードをリセット
            TempData.Remove(SessionKey.WizardSession);

            return RedirectToAction(nameof(Complete));
        }

        // ─── 完了 ─────────────────────────────────────────────────

        /// <summary>完了ページ表示</summary>
        [HttpGet]
        public IActionResult Complete()
        {
            return View();
        }

        // ─── ヘルパー ─────────────────────────────────────────────

        /// <summary>X-Requested-With ヘッダーで Ajax リクエストかどうかを判定する</summary>
        private bool IsAjaxRequest() =>
            Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        /// <summary>TempData からウィザードセッションを取得する（Peek で保持）</summary>
        private WizardSessionModel? GetSession()
        {
            var json = TempData.Peek(SessionKey.WizardSession) as string;
            if (string.IsNullOrEmpty(json)) return null;
            return JsonSerializer.Deserialize<WizardSessionModel>(json);
        }

        /// <summary>ウィザードセッションを TempData に保存する</summary>
        private void SaveSession(WizardSessionModel session)
        {
            TempData[SessionKey.WizardSession] = JsonSerializer.Serialize(session);
        }
    }
}
