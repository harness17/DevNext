---
name: search-restore
description: 検索条件・ページ状態の復元パターンを実装する。検索フォーム付き一覧ページを追加するときに使用する。
---

検索フォームを持つ一覧ページに「一覧復帰パターン」を実装します。
編集・詳細などのサブページから戻った際に、直前の検索条件・ページ位置を再現します。

## 手順

### 1. SessionKey に保存キーを追加

`DevNext/Common/SessionKey.cs` に検索条件キーとページ状態キーを追加する。

```csharp
public static string XxxCondViewModel = "XxxCondViewModel";
public static string XxxPageModel     = "XxxPageModel";
```

### 2. GET Index アクション

`returnList` パラメータを追加し、`true` のとき TempData から条件＋ページ状態を復元する。
検索実行後は毎回 TempData に保存する。

```csharp
[HttpGet]
public IActionResult Index(SearchModelBase? pageModel = null, bool returnList = false)
{
    var model = LocalUtil.MapPageModelTo<XxxViewModel>(pageModel);

    if (model.PageRead != null || IsAjaxRequest() || returnList)
    {
        // 検索条件を復元
        var sessionCond = TempData.Peek(SessionKey.XxxCondViewModel);
        if (sessionCond != null)
            model.Cond = JsonSerializer.Deserialize<XxxCondViewModel>(sessionCond.ToString()!)!;

        // 一覧復帰時はページ・ソート状態も復元
        if (returnList)
        {
            var sessionPage = TempData.Peek(SessionKey.XxxPageModel);
            if (sessionPage != null)
            {
                var saved = JsonSerializer.Deserialize<SearchModelBase>(sessionPage.ToString()!)!;
                model.Page      = saved.Page;
                model.Sort      = saved.Sort;
                model.SortDir   = saved.SortDir;
                model.RecordNum = saved.RecordNum;
            }
        }

        if (IsAjaxRequest()) model.PageRead = PageRead.Paging;
    }

    model = _workerService.GetXxxList(model);

    // 検索条件・ページ状態を TempData に保存
    TempData[SessionKey.XxxCondViewModel] = JsonSerializer.Serialize(model.Cond);
    TempData[SessionKey.XxxPageModel]     = JsonSerializer.Serialize(
        new SearchModelBase { Page = model.Page, Sort = model.Sort, SortDir = model.SortDir, RecordNum = model.RecordNum });

    return View(model);
}
```

### 3. POST Index アクション

検索実行後に TempData を更新する（条件＋ページ状態の両方）。

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Index(XxxViewModel model)
{
    model = _workerService.GetXxxList(model);
    TempData[SessionKey.XxxCondViewModel] = JsonSerializer.Serialize(model.Cond);
    TempData[SessionKey.XxxPageModel]     = JsonSerializer.Serialize(
        new SearchModelBase { Page = model.Page, Sort = model.Sort, SortDir = model.SortDir, RecordNum = model.RecordNum });

    if (IsAjaxRequest()) return PartialView("_IndexPartial", model);
    return View(model);
}
```

### 4. 一覧へのリダイレクト（Controller）

CRUD 操作後の `RedirectToAction("Index")` にはすべて `returnList = true` を付与する。

```csharp
return RedirectToAction("Index", new { returnList = true });
```

### 5. 「一覧に戻る」リンク（View）

一覧ページへ戻るリンクにはすべて `asp-route-returnList="true"` を付与する。

```html
<a asp-action="Index" asp-route-returnList="true" class="btn btn-secondary">一覧に戻る</a>
```

## 対象ページの判断基準

- 検索フォーム（条件入力＋検索ボタン）がある一覧ページはすべて対象
- ページング・ソートが実装されている場合はページ状態も必ず保存・復元する
- ナビバーやホームからの直接遷移（`returnList` なし）は条件をリセットして初期表示にする
