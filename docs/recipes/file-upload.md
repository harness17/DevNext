# レシピ: ファイルアップロード・ダウンロード・削除

`Samples/FileSample` の実装を参考にしてください。

## ファイル保存先

```
wwwroot/uploads/   ← 実ファイル保存ディレクトリ（.gitignore 推奨）
```

DB には `FileEntity` にファイルメタデータ（ファイル名・保存パス・サイズ等）を保存します。

## エンティティ

```csharp
public class FileEntity : SiteEntityBase
{
    [Required, MaxLength(256)]
    public string OriginalFileName { get; set; } = "";  // 元のファイル名

    [Required, MaxLength(512)]
    public string SavedFileName { get; set; } = "";     // 保存時のファイル名（GUID）

    [Required, MaxLength(256)]
    public string ContentType { get; set; } = "";

    public long FileSize { get; set; }
}
```

## アップロード（Controller）

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Upload(IFormFile file)
{
    if (file == null || file.Length == 0)
    {
        ModelState.AddModelError("", "ファイルを選択してください。");
        return View();
    }

    // 保存ファイル名をGUIDで生成（元のファイル名は DB に保存）
    var savedName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
    var savePath  = Path.Combine(_uploadDir, savedName);

    using (var stream = new FileStream(savePath, FileMode.Create))
        await file.CopyToAsync(stream);

    await _service.SaveFileMetaAsync(file.FileName, savedName, file.ContentType, file.Length);

    return RedirectToAction(nameof(Index));
}
```

## ダウンロード（Controller）

```csharp
[HttpGet]
public async Task<IActionResult> Download(long id)
{
    var entity = await _service.GetFileEntityAsync(id);
    if (entity == null) return NotFound();

    var filePath = Path.Combine(_uploadDir, entity.SavedFileName);
    if (!System.IO.File.Exists(filePath)) return NotFound();

    var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
    return File(bytes, entity.ContentType, entity.OriginalFileName);
}
```

## 削除（Controller）

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(long id)
{
    var entity = await _service.GetFileEntityAsync(id);
    if (entity == null) return NotFound();

    // 実ファイル削除
    var filePath = Path.Combine(_uploadDir, entity.SavedFileName);
    if (System.IO.File.Exists(filePath))
        System.IO.File.Delete(filePath);

    // DB から論理削除
    await _service.DeleteAsync(id);

    return RedirectToAction(nameof(Index));
}
```

## View（アップロードフォーム）

```html
<form asp-action="Upload" method="post" enctype="multipart/form-data">
    @Html.AntiForgeryToken()
    <input type="file" name="file" class="form-control" />
    <button type="submit" class="btn btn-primary mt-2">アップロード</button>
</form>
```

## ポイント

- 保存ファイル名は GUID にして衝突を防ぐ
- 元のファイル名は DB に保存しダウンロード時に使用
- `wwwroot/uploads/` は `.gitignore` に追加する
- 本番環境では Azure Blob Storage 等への切り替えを検討
