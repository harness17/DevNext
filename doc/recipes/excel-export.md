# レシピ: Excel エクスポート（ClosedXML）

`Samples/DatabaseSample` の実装を参考にしてください。

## 依存パッケージ

```xml
<PackageReference Include="ClosedXML" Version="0.104.*" />
```

## 実装パターン

### Service 層

```csharp
using ClosedXML.Excel;

/// <summary>検索条件に一致するデータを Excel ファイルとして生成する</summary>
public async Task<byte[]> ExportExcelAsync(CondViewModel? condVm)
{
    // 全件取得（ページングなし）
    var cond = _repository.GetCondModel(condVm ?? new CondViewModel());
    cond.Pager.recoedNumber = 0; // 0 = 全件
    var rows = _repository.GetAll(cond);

    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add("データ一覧");

    // ヘッダー行
    ws.Cell(1, 1).Value = "ID";
    ws.Cell(1, 2).Value = "名前";
    ws.Cell(1, 3).Value = "登録日時";

    // データ行
    int row = 2;
    foreach (var item in rows)
    {
        ws.Cell(row, 1).Value = item.Id;
        ws.Cell(row, 2).Value = item.Name;
        ws.Cell(row, 3).Value = item.CreateDate.ToString("yyyy/MM/dd HH:mm");
        row++;
    }

    // 列幅の自動調整
    ws.Columns().AdjustToContents();

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    return stream.ToArray();
}
```

### Controller 層

```csharp
[HttpGet]
public async Task<IActionResult> ExportExcel()
{
    // TempData.Peek で検索条件を保持したままダウンロード
    var condVm = GetCondFromTempData();
    var bytes = await _service.ExportExcelAsync(condVm);
    return File(bytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"データ一覧_{DateTime.Now:yyyyMMdd}.xlsx");
}
```

## ポイント

- `cond.Pager.recoedNumber = 0` で全件取得（ページング無効化）
- `TempData.Peek` で検索条件を消費せずに読み取る
- `ws.Columns().AdjustToContents()` で列幅を自動調整
