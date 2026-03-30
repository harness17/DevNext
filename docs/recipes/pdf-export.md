# レシピ: PDF 生成（QuestPDF）

`Samples/DatabaseSample` の実装を参考にしてください。

## 依存パッケージ

```xml
<PackageReference Include="QuestPDF" Version="2024.*" />
```

## ライセンス設定（Program.cs）

```csharp
// QuestPDF のライセンス設定（Community ライセンスは無料）
QuestPDF.Settings.License = LicenseType.Community;
```

## 実装パターン

### Document クラス

```csharp
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class SampleDocument : IDocument
{
    private readonly List<SampleEntity> _data;

    public SampleDocument(List<SampleEntity> data)
    {
        _data = data;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);

            page.Header().Text("データ一覧").SemiBold().FontSize(16);

            page.Content().Table(table =>
            {
                // 列定義
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(50);  // ID
                    columns.RelativeColumn();    // 名前
                    columns.ConstantColumn(120); // 登録日時
                });

                // ヘッダー行
                table.Header(header =>
                {
                    header.Cell().Text("ID");
                    header.Cell().Text("名前");
                    header.Cell().Text("登録日時");
                });

                // データ行
                foreach (var item in _data)
                {
                    table.Cell().Text(item.Id.ToString());
                    table.Cell().Text(item.Name);
                    table.Cell().Text(item.CreateDate.ToString("yyyy/MM/dd"));
                }
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Page ");
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
            });
        });
    }
}
```

### Service 層

```csharp
public byte[] ExportPdf(List<SampleEntity> data)
{
    var document = new SampleDocument(data);
    return document.GeneratePdf();
}
```

### Controller 層

```csharp
[HttpGet]
public IActionResult ExportPdf()
{
    var condVm = GetCondFromTempData();
    var data = _service.GetAllForExport(condVm);
    var bytes = _service.ExportPdf(data);
    return File(bytes, "application/pdf", $"データ一覧_{DateTime.Now:yyyyMMdd}.pdf");
}
```

## ポイント

- `IDocument` を実装したクラスでレイアウトを定義する
- `GeneratePdf()` でバイト配列として取得
- Community ライセンスは商用利用不可（確認が必要）
