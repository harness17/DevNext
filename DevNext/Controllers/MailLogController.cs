using Dev.CommonLibrary.Attributes;
using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Site.Common;
using Site.Models;
using Site.Service;
using System.Text.Json;

namespace Site.Controllers
{
    /// <summary>
    /// メール送信ログ一覧コントローラー
    /// お問い合わせフォームから送信されたメールの送信履歴を表示する。
    /// </summary>
    [Authorize]
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class MailLogController : Controller
    {
        private readonly MailLogService _mailLogService;

        public MailLogController(MailLogService mailLogService)
        {
            _mailLogService = mailLogService;
        }

        /// <summary>
        /// メール送信ログ一覧を表示する（GET）
        /// returnList=true の場合は TempData から検索条件・ページ状態を復元する
        /// </summary>
        [HttpGet]
        public IActionResult Index(SearchModelBase? pageModel = null, bool returnList = false)
        {
            var model = LocalUtil.MapPageModelTo<MailLogViewModel>(pageModel);

            if (model.PageRead != null || IsAjaxRequest() || returnList)
            {
                // TempData から検索条件を復元（Peek で消費しない）
                var sessionCond = TempData.Peek(SessionKey.MailLogCondViewModel);
                if (sessionCond != null)
                    model.Cond = JsonSerializer.Deserialize<MailLogCondSearchViewModel>(sessionCond.ToString()!)!;

                // 一覧復帰時はページ・ソート状態も復元する
                if (returnList)
                {
                    var sessionPage = TempData.Peek(SessionKey.MailLogPageModel);
                    if (sessionPage != null)
                    {
                        var savedPage = JsonSerializer.Deserialize<SearchModelBase>(sessionPage.ToString()!)!;
                        model.Page = savedPage.Page;
                        model.Sort = savedPage.Sort;
                        model.SortDir = savedPage.SortDir;
                        model.RecordNum = savedPage.RecordNum;
                    }
                }

                if (IsAjaxRequest()) model.PageRead = PageRead.Paging;
            }

            model = _mailLogService.GetMailLogList(model);

            // 検索条件・ページ状態を TempData に保存
            TempData[SessionKey.MailLogCondViewModel] = JsonSerializer.Serialize(model.Cond);
            TempData[SessionKey.MailLogPageModel] = JsonSerializer.Serialize(
                new SearchModelBase { Page = model.Page, Sort = model.Sort, SortDir = model.SortDir, RecordNum = model.RecordNum });

            return View(model);
        }

        /// <summary>
        /// 検索フォーム送信を受け取り、ログ一覧を返す（POST）
        /// Ajax リクエストの場合はパーシャルビューを返す
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(MailLogViewModel model)
        {
            model = _mailLogService.GetMailLogList(model);

            // 検索条件・ページ状態を TempData に保存
            TempData[SessionKey.MailLogCondViewModel] = JsonSerializer.Serialize(model.Cond);
            TempData[SessionKey.MailLogPageModel] = JsonSerializer.Serialize(
                new SearchModelBase { Page = model.Page, Sort = model.Sort, SortDir = model.SortDir, RecordNum = model.RecordNum });

            if (IsAjaxRequest()) return PartialView("_IndexPartial", model);
            return View(model);
        }

        /// <summary>
        /// Ajax リクエストかどうかを判定する
        /// </summary>
        private bool IsAjaxRequest()
            => Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}
