# PdfSample 実装計画

- 作成日: 2026-04-04
- ブランチ: develop

## スプリントコントラクト（完成条件）

- [ ] `Samples/PdfSample/` プロジェクトが `DevNext.sln` に追加されビルドが通る
- [ ] 請求書の一覧・作成・編集・削除が動作する
- [ ] 一覧にページング・ソート・検索（取引先名、ステータス）が動作する
- [ ] 請求書詳細から PDF をダウンロードできる
- [ ] PDF に日本語テキスト（取引先名・品目等）が正しく表示される
- [ ] 明細行の合計金額が PDF に表示される
- [ ] 一覧から複数選択して ZIP 一括ダウンロードできる
- [ ] 認証あり（ログイン必須）
- [ ] 起動時に DB 自動作成 + Seed データ投入（admin1@sample.jp / Admin1!、member1@sample.jp / Member1!）

---

## 方針

- ExcelSample の `Program.cs` パターン（Identity・DI・Cookie 設定）を踏襲する
- ページング・ソート・検索は DatabaseSample のパターンを踏襲する
- PDF 生成ライブラリは QuestPDF 2026.2.3（既存 DatabaseSample と同バージョン）
- DB は `PdfSampleDB`（独立）。起動時に `EnsureCreated` + Seeder で自動初期化
- Sample 同士は依存しない
- `AGENTS.md` のルールに従って実装すること

---

## STEP 1: プロジェクトファイルと基本設定

- [ ] `Samples/PdfSample/PdfSample.csproj` を作成する

  ```xml
  <Project Sdk="Microsoft.NET.Sdk.Web">
    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
      <RootNamespace>PdfSample</RootNamespace>
    </PropertyGroup>
    <ItemGroup>
      <ProjectReference Include="..\..\CommonLibrary\CommonLibrary.csproj" />
    </ItemGroup>
    <ItemGroup>
      <PackageReference Include="AutoMapper" Version="16.1.1" />
      <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.0" />
      <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
      <PackageReference Include="Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation" Version="10.0.0" />
      <PackageReference Include="QuestPDF" Version="2026.2.3" />
    </ItemGroup>
  </Project>
  ```

- [ ] `Samples/PdfSample/appsettings.json` を作成する（DB名: `PdfSampleDB`）

  ```json
  {
    "ConnectionStrings": {
      "SiteConnection": "Server=localhost;Database=PdfSampleDB;Trusted_Connection=True;TrustServerCertificate=True;"
    },
    "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
    "AllowedHosts": "*"
  }
  ```

- [ ] `Samples/PdfSample/Program.cs` を作成する

  - ExcelSample の `Program.cs` を基に namespace・DbContext・Service 名を変更
  - `QuestPDF.Settings.License = LicenseType.Community;` を `var app = builder.Build();` の直後に追加
  - 起動時 DB 初期化ブロック（`EnsureCreated` + `PdfSampleSeeder.SeedAsync`）を追加
  - `MapControllerRoute` のデフォルト: `{controller=Invoice}/{action=Index}/{id?}`
  - DI 登録: `PdfSampleService`、`InvoiceRepository`、`InvoiceItemRepository`、`AccessLogAttribute`

- [ ] `DevNext.sln` に PdfSample プロジェクトエントリを追加する

  追加内容（既存サンプルの後に挿入）:
  - `Project` 宣言ブロック（新規 GUID を採番）
  - `ProjectConfigurationPlatforms` に Debug/Release の 6 エントリ
  - `NestedProjects` に Samples フォルダ GUID とのペア

---

## STEP 2: Common

- [ ] `Samples/PdfSample/Common/EnumDefine.cs` を作成する

  ```csharp
  namespace PdfSample.Common;

  public enum PageRead { Resarch, Paging, Sorting, ChangeRecordNum }

  public enum InvoiceStatus
  {
      [Display(Name = "下書き")] Draft = 0,
      [Display(Name = "発行済")] Issued = 1,
      [Display(Name = "支払済")] Paid = 2,
  }
  ```

- [ ] `Samples/PdfSample/Common/SessionKey.cs` を作成する（キー定数: `PdfSampleCondViewModel`、`PdfSamplePageModel`、`Message`）

- [ ] `Samples/PdfSample/Common/LocalUtil.cs` を作成する（DatabaseSample の LocalUtil.cs を `namespace PdfSample.Common` に変えて流用）

- [ ] `Samples/PdfSample/Common/PdfSampleSeeder.cs` を作成する

  - `admin1@sample.jp / Admin1! (Admin)` と `member1@sample.jp / Member1! (Member)` を作成
  - サンプル請求書 3 件（各 2〜3 件の明細行）を Seed する

---

## STEP 3: Entity

- [ ] `Samples/PdfSample/Entity/InvoiceEntity.cs` を作成する

  パターン: `XxxEntityBase (abstract) : SiteEntityBase` → `XxxEntity` + `XxxEntityHistory`

  ```
  InvoiceEntityBase (abstract) : SiteEntityBase
    ├── InvoiceEntity          // Items ナビゲーションプロパティをここだけに定義
    └── InvoiceEntityHistory   // HistoryId: long [Key], IEntityHistory

  InvoiceItemEntityBase (abstract) : SiteEntityBase
    ├── InvoiceItemEntity
    └── InvoiceItemEntityHistory
  ```

  フィールド（InvoiceEntityBase）:
  - `InvoiceNumber: string`（[Required][MaxLength(50)]）
  - `ClientName: string`（[Required][MaxLength(200)]）
  - `IssueDate: DateTime`
  - `DueDate: DateTime`
  - `Status: InvoiceStatus`
  - `Notes: string?`

  フィールド（InvoiceItemEntityBase）:
  - `InvoiceId: long`
  - `Description: string`（[Required][MaxLength(200)]）
  - `Quantity: int`
  - `UnitPrice: decimal`

  **注意**: `Items` ナビゲーションは `InvoiceEntity`（具象クラス）のみに定義する（History クラスに継承させない）

---

## STEP 4: Data（DbContext）

- [ ] `Samples/PdfSample/Data/PdfSampleDbContext.cs` を作成する

  - DatabaseSample の DbContext パターンを踏襲
  - DbSet: `InvoiceEntity`、`InvoiceEntityHistory`、`InvoiceItemEntity`、`InvoiceItemEntityHistory`
  - `OnModelCreating` に FK リレーション設定（`HasMany(e => e.Items).WithOne().HasForeignKey(i => i.InvoiceId).OnDelete(DeleteBehavior.Cascade)`）

---

## STEP 5: Repository

- [ ] `Samples/PdfSample/Repository/InvoiceRepository.cs` を作成する

  - `RepositoryBase<InvoiceEntity, InvoiceEntityHistory, InvoiceCondModel>` 継承
  - `InvoiceCondModel` の検索条件: `ClientName: string?`（部分一致）、`Status: InvoiceStatus?`（完全一致）、`Pager`
  - 詳細取得時は `Include(x => x.Items)` で明細行を含める

- [ ] `Samples/PdfSample/Repository/InvoiceItemRepository.cs` を作成する

  - `RepositoryBase<InvoiceItemEntity, InvoiceItemEntityHistory, InvoiceItemCondModel>` 継承
  - `InvoiceItemCondModel` には `InvoiceId: long?` を持たせる

---

## STEP 6: Models（ViewModel）

- [ ] `Samples/PdfSample/Models/CommonViewModels.cs` を作成する（DatabaseSample の CommonViewModels.cs を `namespace PdfSample.Models` に変えてコピー）

- [ ] `Samples/PdfSample/Models/PdfSampleViewModels.cs` を作成する

  必要なクラス:
  - `LoginViewModel`
  - `InvoiceViewModel : SearchModelBase`（`RowData`、`Cond`、`RecoedNumberList`、`SelectedIds: List<long>`）
  - `InvoiceDataViewModel`（`Rows: List<InvoiceEntity>`、`Summary: CommonListSummaryModel?`）
  - `InvoiceCondViewModel : SearchCondModelBase`（`ClientName`、`Status`、`StatusList`）
  - `InvoiceDetailViewModel`（`Id`、`InvoiceNumber`、`ClientName`、`IssueDate`、`DueDate`、`Status`、`Notes`、`Items: List<InvoiceItemViewModel>`）
  - `InvoiceItemViewModel`（`Id`、`Description`、`Quantity`、`UnitPrice`、`SubTotal` 計算プロパティ）

---

## STEP 7: Service

- [ ] `Samples/PdfSample/Service/PdfSampleService.cs` を作成する

  メソッド:
  - `GetInvoiceList(InvoiceViewModel model) : InvoiceViewModel` — ページング・ソート・件数サマリー
  - `GetInvoiceDetail(long id) : InvoiceDetailViewModel?` — Items を Include して取得
  - `InsertInvoice(InvoiceDetailViewModel model, string? userName) : void`
  - `UpdateInvoice(InvoiceDetailViewModel model) : void` — 明細行は全削除→再 Insert 戦略
  - `DeleteInvoice(long id) : void` — 論理削除
  - `ExportPdf(long id) : MemoryStream?` — QuestPDF で A4 縦の請求書 PDF 生成
  - `ExportPdfBulk(List<long> ids) : MemoryStream` — 複数 PDF を ZIP にまとめる

  PDF レイアウト（QuestPDF）:
  ```
  Page（A4 縦、余白 2cm）
  ├ Header: タイトル「請求書」・請求書番号・取引先名・発行日・支払期限
  ├ Content: 明細テーブル（品目/数量/単価/小計）、合計金額行、備考欄
  └ Footer: ページ番号
  ```
  日本語フォント: `FontFamily("Noto Sans CJK JP", "Meiryo", "MS Gothic")`

  ZIP 生成の注意:
  - `ZipArchive` は `leaveOpen: true` で作成し最後に `zipStream.Position = 0` をセットする
  - エントリ名: `請求書_{InvoiceNumber}_{id}.pdf`

---

## STEP 8: Controller

- [ ] `Samples/PdfSample/Controllers/AccountController.cs` を作成する（ExcelSample の AccountController.cs を namespace・モデル参照を変えてコピー）

- [ ] `Samples/PdfSample/Controllers/InvoiceController.cs` を作成する

  アクション一覧:

  | HTTP | アクション | 説明 |
  |------|-----------|------|
  | GET | `Index` | 一覧（検索状態復元） |
  | POST | `Index` | 検索（Ajax 対応、`_IndexPartial` を返す） |
  | GET | `Details(id)` | 詳細 |
  | GET | `Create` | 新規作成フォーム |
  | POST | `Create` | 登録（PRG パターン） |
  | GET | `Edit(id)` | 編集フォーム |
  | POST | `Edit` | 更新（PRG パターン） |
  | GET | `Delete(id)` | 削除確認画面 |
  | POST | `DeleteConfirmed` | 論理削除（PRG パターン） |
  | GET | `DownloadPdf(id)` | 単件 PDF ダウンロード |
  | POST | `DownloadZip` | 複数選択 ZIP ダウンロード |

  `[Authorize]` をコントローラークラスに付与（全アクション認証必須）

---

## STEP 9: Views

- [ ] `Samples/PdfSample/Views/_ViewImports.cshtml` を作成する

  ```cshtml
  @using PdfSample
  @using PdfSample.Models
  @using PdfSample.Entity
  @using PdfSample.Common
  @using Dev.CommonLibrary.Extensions
  @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
  ```

- [ ] `Samples/PdfSample/Views/_ViewStart.cshtml` を作成する

- [ ] `Samples/PdfSample/Views/Shared/_Layout.cshtml` を作成する（ExcelSample ベースでタイトル・ナビを変更）

- [ ] `Samples/PdfSample/Views/Shared/_LoginPartial.cshtml`・`_ValidationScriptsPartial.cshtml`・`_LayoutModalPartial.cshtml` を ExcelSample からコピー（namespace 調整）

- [ ] `Samples/PdfSample/Views/Account/Login.cshtml` を ExcelSample からコピー（namespace 調整）

- [ ] `Samples/PdfSample/Views/Invoice/Index.cshtml` を作成する

  - 検索フォーム（取引先名テキスト、ステータスドロップダウン）
  - テーブル（チェックボックス / ID / 請求書番号 / 取引先名 / 発行日 / 支払期限 / ステータス / 操作）
  - 「PDF一括ダウンロード」ボタン（POST → `DownloadZip`、1件も選択なしは JS で阻止）
  - ページャー

- [ ] `Samples/PdfSample/Views/Invoice/_IndexPartial.cshtml` を作成する（Ajax 用テーブル部分）

- [ ] `Samples/PdfSample/Views/Invoice/Details.cshtml` を作成する

  - 請求書基本情報表示
  - 明細行テーブル（合計行付き）
  - 「PDF ダウンロード」ボタン

- [ ] `Samples/PdfSample/Views/Invoice/Edit.cshtml` を作成する

  - 新規/編集共用（`Model.Id == null` でタイトル切り替え）
  - 明細行の動的追加・削除（JavaScript、indexed binding `Items[0].Description`）

- [ ] `Samples/PdfSample/Views/Invoice/Delete.cshtml` を作成する

- [ ] `Samples/PdfSample/wwwroot/` を ExcelSample から複製（css・js・lib）

---

## 参照すべき既存ファイル

実装時は以下を参考にすること:

- `Samples/ExcelSample/Program.cs` — Program.cs のパターン
- `Samples/ExcelSample/Controllers/AccountController.cs` — アカウントコントローラー
- `Samples/DatabaseSample/Service/DatabaseSampleService.cs` — サービスのパターン
- `Samples/DatabaseSample/Controllers/DatabaseSampleController.cs` — ページング・ソート・Ajax のパターン
- `Samples/DatabaseSample/Data/DatabaseSampleDbContext.cs` — DbContext のパターン
- `Samples/DatabaseSample/Common/LocalUtil.cs` — LocalUtil のパターン
- `Samples/DatabaseSample/Models/CommonViewModels.cs` — 共通 ViewModel のパターン
- `DevNext.sln` — sln ファイルへのプロジェクト追加方法
