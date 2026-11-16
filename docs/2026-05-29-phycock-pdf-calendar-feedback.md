# Phycock からのフィードバック移植（PDF基盤 / カレンダー色）

- 日付: 2026-05-29
- 由来: 体調管理アプリ **Phycock**（DevNext テンプレート派生）で実運用しながら育てた汎用コードを、本家 DevNext の `CommonLibrary` へ逆輸入したもの。
- ブランチ: `feature/phycock-pdf-calendar-feedback`
- 検証: `dotnet build DevNext.slnx`（0 警告 / 0 エラー）、`dotnet test DevNext.slnx`（69 passed）

Phycock は DevNext と同じ `CommonLibrary` を共有する兄弟プロジェクト。Phycock 側で発生した「認証付き動的ページの PDF 化」「PDF 出力のセキュリティ強化」「カレンダー色の一元管理」を汎用機能として吸い上げた。

---

## 移植マップ

| 区分 | 内容 | 追加/変更ファイル |
|------|------|-------------------|
| **A1** | `IPlaywrightFactory` + `PlaywrightFactory`（Playwright を Singleton で使い回す） | 新規 `CommonLibrary/Pdf/PlaywrightPdfFactory.cs` |
| **A2** | `PlaywrightPdfService.GenerateFromUrlAsync`（認証 Cookie + 描画完了待機つき URL レンダリング）追加。既存 `GenerateFromHtmlAsync` も Factory 経由に統一 | `CommonLibrary/Pdf/PlaywrightPdfService.cs` |
| **B** | `PdfPrintHelper`（Host ヘッダーを信頼しないループバック URL 構築 + 認証 Cookie 抽出） | 新規 `CommonLibrary/Pdf/PdfPrintHelper.cs` |
| **C** | `CalendarColorAttribute` / `CalendarColorExtensions` / `CalendarColorValue`（enum に色を持たせて複数画面で一元参照） | 新規 `CommonLibrary/Attributes/CalendarColorAttribute.cs`, `CommonLibrary/Extensions/CalendarColorExtensions.cs` |
| DI | `IPlaywrightFactory` の Singleton 登録（`PlaywrightPdfService` が Factory 依存になったため必須） | `Samples/PdfSample/Program.cs`, `Samples/DatabaseSample/Program.cs` |

### なぜ移植したか

- **A1**: 既存の `PlaywrightPdfService` は呼び出しごとに `Playwright.CreateAsync()` していた。Playwright 初期化は重いので Singleton 化して使い回す。Browser は引き続き per-request 起動。HTML 方式・URL 方式どちらにも効く純粋なパフォーマンス改善。
- **A2**: 既存の HTML 文字列方式（`GenerateFromHtmlAsync` + `RazorViewToStringRenderer`）は静的 HTML 向き。ログイン必須ページや Chart.js などの動的描画を含むページは「内部 URL をそのままレンダリングする」方が確実。両方式を `CommonLibrary` に揃えて選べるようにした。
- **B**: A2 の URL 方式を使う際、内部レンダリング先 URL を `Request.Host`（外部から改ざん可能）で組み立てると Host ヘッダー注入のリスクがある。`127.0.0.1` + 同一プロセスの待受ポートに固定し、転送 Cookie も認証/セッション用プレフィックスだけにホワイトリスト化する。Phycock の `StatisticsController` のセキュリティレビュー対応を汎用ヘルパーへ抽出した。
- **C**: enum メンバーに色属性を付け、カレンダー・統計など複数画面で同じ色定義を参照する。DevNext にも Schedule（カレンダー）機能があるため再利用可能。

---

## 不採用（D候補: `ExpressionHelper`）

Phycock の `CommonLibrary/Extensions/Helper/ExpressionHelper.cs`（ラムダ式からメンバーパスをリフレクションで取り出す自前実装）は **移植しなかった**。

理由: DevNext 側の `HtmlExtensions` は既に ASP.NET Core 標準の `ModelExpressionProvider.GetExpressionText`（`HtmlExtensions.cs` の `GetExpressionText`）を使っており、Phycock の自前実装は上位互換に対して劣化コピーになる。移植すると未使用かつ低機能なコードを増やすだけなので見送った。

逆方向（DevNext のみが持つ `HtmlContentExtensions.cs` / `PagerHtmlExtensions.cs` を Phycock へ）は今回のスコープ外。両 `CommonLibrary` の枝分かれを揃えたい場合は別途検討する。

---

## 使い方（A2 + B のセット）

認証付き・動的描画ページを PDF 化する Controller 側の例:

```csharp
// 1) Host を信頼せずループバック URL を組む
var url = PdfPrintHelper.BuildLoopbackUrl(Request, "/Statistics?print=1&weekStart=2026-05-04");

// 2) 認証 Cookie だけを抽出（既定: Identity.Application / Session）
var cookies = PdfPrintHelper.ExtractAuthCookies(Request);

// 3) 描画完了フラグを待ってからレンダリング（Chart.js 等）
var pdf = await _pdfService.GenerateFromUrlAsync(
    url,
    cookies,
    readyFlagJs: "() => window.chartsReady === true",
    landscape: true);

return File(pdf, "application/pdf", "report.pdf");
```

静的 HTML を PDF 化する従来方式は変更なし:

```csharp
var html = await _renderer.RenderViewToStringAsync(...);
var pdf = await _pdfService.GenerateFromHtmlAsync(html);
```

---

## 今後の検討

- PdfSample に URL レンダリング方式の動作サンプル（認証ページ→PDF）を 1 つ足すと、A2 + B が実コードで example 化できる。
- `CommonLibrary利用ガイド.md` の PDF 章に GenerateFromUrlAsync / PdfPrintHelper を追記する。
