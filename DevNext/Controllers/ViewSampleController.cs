using Site.Models;
using Microsoft.AspNetCore.Mvc;

namespace Site.Controllers
{
    public class ViewSampleController : Controller
    {
        // ポイント: DB を使わないサンプルのため、コントローラー内でダミーデータを直接セットする
        //           実際の画面ではサービス層からデータを取得してモデルに詰める

        // GET: 初期表示（サーバー側でサンプルデータを生成して渡す）
        public IActionResult PartialSample()
        {
            var model = new PartialSampleViewModel();
            model.ChildList.Add(new PartialSampleChildModel { Text = "サンプル1", Number = 1, Order = 0 });
            model.ChildList.Add(new PartialSampleChildModel { Text = "サンプル2", Number = 2, Order = 1 });
            return View(model);
        }

        // POST: フォーム送信後の処理
        // ポイント: パーシャルビューのサンプルなので、送信後は同じビューをそのまま返している
        //           実際のアプリでは保存処理後に RedirectToAction (PRGパターン) を使う
        [HttpPost]
        public IActionResult PartialSample(PartialSampleViewModel model)
        {
            return View(model);
        }

        // GET: ソートサンプル初期表示
        // ポイント: Order プロパティを Order フィールドで持つことで並び順を管理する
        //           Sortable.js によるドラッグ＆ドロップ後、JavaScript 側で Order 値を更新して送信する
        public IActionResult SortSample()
        {
            var model = new PartialSampleViewModel();
            model.ChildList.Add(new PartialSampleChildModel { Text = "アイテム1", Number = 1, Order = 1 });
            model.ChildList.Add(new PartialSampleChildModel { Text = "アイテム2", Number = 2, Order = 2 });
            return View(model);
        }

        // POST: ソート・追加・削除後の保存処理
        // ポイント: ChildList[0].Text, ChildList[1].Text ... のような name 属性を持つフォームデータを
        //           MVC のモデルバインディングが自動的に List<PartialSampleChildModel> に変換する
        [HttpPost]
        public IActionResult SortSample(PartialSampleViewModel model)
        {
            return View(model);
        }
    }
}
