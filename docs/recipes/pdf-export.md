# レシピ: PDF 生成（Playwright headless print）

`CommonLibrary/Pdf` と `Samples/DatabaseSample` / `Samples/PdfSample` の実装を参考にしてください。

## 依存パッケージ

```xml
<PackageReference Include="Microsoft.Playwright" Version="1.59.0" />
```

初回セットアップ時は、対象アプリのビルド後に Chromium をインストールします。

```powershell
.\Samples\PdfSample\bin\Debug\net10.0\playwright.ps1 install chromium
.\Samples\DatabaseSample\bin\Debug\net10.0\playwright.ps1 install chromium
```

## 実装パターン

### 印刷専用 View

`Views/Shared/_LayoutPrint.cshtml` を作り、ナビゲーションや通常画面用 CSS を含めずに印刷用 CSS だけを定義します。

```cshtml
<!DOCTYPE html>
<html lang="ja">
<head>
    <meta charset="utf-8" />
    <style>
        @@page {
            size: A4 portrait;
            margin: 16mm;
        }
    </style>
</head>
<body>
    @RenderBody()
</body>
</html>
```

帳票本体は通常の Razor View として作成します。

```cshtml
@model SamplePrintViewModel
@{
    Layout = "_LayoutPrint";
}

<h1>データ一覧</h1>
<table>
    @foreach (var item in Model.Rows)
    {
        <tr>
            <td>@item.Id</td>
            <td>@item.Name</td>
        </tr>
    }
</table>
```

### Controller 層

```csharp
private readonly RazorViewToStringRenderer _razorRenderer;
private readonly PlaywrightPdfService _pdfService;

[HttpGet]
public async Task<IActionResult> ExportPdf()
{
    var model = _service.GetPrintData();
    var html = await _razorRenderer.RenderAsync(ControllerContext, "Sample/Print", model);
    var pdf = await _pdfService.GenerateFromHtmlAsync(html);

    Response.Headers.CacheControl = "no-store, private";
    return File(pdf, "application/pdf", $"データ一覧_{DateTime.Now:yyyyMMdd}.pdf");
}
```

## ポイント

- PDF レイアウトは Razor + CSS で定義する
- `RazorViewToStringRenderer` で HTML 化し、`PlaywrightPdfService` で PDF 化する
- 認証済みページをブラウザで開かず、サーバー側で生成した HTML を `SetContentAsync` に渡す
- PDF レスポンスは個人情報を含む可能性があるため `Cache-Control: no-store, private` を付与する
- CI/CD や初回環境構築では `playwright.ps1 install chromium` が必要
