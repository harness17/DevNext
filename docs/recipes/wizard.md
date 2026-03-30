# レシピ: 多段階フォーム（ウィザード）

`Samples/WizardSample` の実装を参考にしてください。

## フロー

```
Step1（基本情報）→ Step2（詳細情報）→ Step3（確認）→ Complete（完了）
```

各ステップ間のデータは `TempData` に JSON シリアライズして保持します。

## 状態管理

```csharp
// セッション保存
private void SaveSession(WizardSessionModel session)
{
    TempData[SessionKey.WizardSession] = JsonSerializer.Serialize(session);
}

// セッション読み取り（Peek = 消費しない）
private WizardSessionModel? GetSession()
{
    var json = TempData.Peek(SessionKey.WizardSession) as string;
    if (string.IsNullOrEmpty(json)) return null;
    return JsonSerializer.Deserialize<WizardSessionModel>(json);
}
```

## ViewModel 設計

```csharp
// 全ステップのデータを保持するセッションモデル
public class WizardSessionModel
{
    // Step 1
    public string Name  { get; set; } = "";
    public string Email { get; set; } = "";

    // Step 2
    public string Subject { get; set; } = "";
    public string Content { get; set; } = "";
}

// 各ステップで必要なプロパティのみバリデーション
public class WizardStep1ViewModel
{
    [Required] public string Name  { get; set; } = "";
    [Required] public string Email { get; set; } = "";
}
```

## Controller パターン

```csharp
// Step 1 表示：既存セッションがあれば値を復元
[HttpGet]
public IActionResult Step1()
{
    var session = GetSession();
    var model = new WizardStep1ViewModel
    {
        Name  = session?.Name  ?? "",
        Email = session?.Email ?? "",
    };
    return View(model);
}

// Step 1 送信：バリデーション → セッション保存 → 次ステップへ
[HttpPost, ValidateAntiForgeryToken]
public IActionResult Step1(WizardStep1ViewModel model)
{
    if (!ModelState.IsValid) return View(model);

    var session = GetSession() ?? new WizardSessionModel();
    session.Name  = model.Name;
    session.Email = model.Email;
    SaveSession(session);

    return RedirectToAction(nameof(Step2));
}

// Step 3 確定：DB 保存 → セッション削除 → 完了へ
[HttpPost, ValidateAntiForgeryToken]
public IActionResult Step3(WizardSessionModel model)
{
    var session = GetSession();
    if (session == null) return RedirectToAction(nameof(Step1));

    _service.SaveWizardData(session);
    TempData.Remove(SessionKey.WizardSession);

    return RedirectToAction(nameof(Complete));
}
```

## 進捗バー

```html
@* パーシャルビューに現在ステップ番号を渡す *@
@await Html.PartialAsync("_ProgressBar", 2)
```

`_ProgressBar.cshtml` は `int` 型のモデルを受け取り、完了・現在・未到達を色分け表示します。

## 戻るボタンの実装

Step の GET アクション冒頭でセッション存在を確認し、なければ Step 1 にリダイレクト:

```csharp
[HttpGet]
public IActionResult Step2()
{
    var session = GetSession();
    if (session == null) return RedirectToAction(nameof(Step1));
    // ...
}
```

## ポイント

- `TempData.Peek` で読み取り（消費しない）、POST 後に上書き
- 各ステップで必要なフィールドのみ ViewModel に含めてバリデーション
- DB 保存は最終ステップの POST のみで行う
- セッションが途切れた場合は Step 1 へフォールバック
