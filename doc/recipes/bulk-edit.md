# レシピ: 一括編集（親子エンティティ）

`Samples/DatabaseSample` の `SampleEntity` + `SampleEntityChild` の実装を参考にしてください。

## エンティティ設計

```
SampleEntityBase (abstract) : SiteEntityBase
  ├── SampleEntity          // 本体テーブル
  └── SampleEntityHistory   // 履歴テーブル

SampleEntityChildBase (abstract) : SiteEntityBase
  ├── SampleEntityChild          // 子テーブル
  └── SampleEntityChildHistory   // 子履歴テーブル
```

## 画面フロー

1. 親エンティティの編集画面で子エンティティ一覧を表示
2. 子の追加・編集・削除を JavaScript で行い、フォーム送信で一括確定
3. Service 層でトランザクション内に親・子を保存

## ViewModel

```csharp
public class BulkEditViewModel
{
    public SampleEntity Parent { get; set; } = new();

    /// <summary>子エンティティ一覧（追加・編集・削除を含む）</summary>
    public List<SampleEntityChild> Children { get; set; } = new();

    /// <summary>削除対象の子エンティティIDリスト</summary>
    public List<long> DeleteChildIds { get; set; } = new();
}
```

## Service 層（トランザクション）

```csharp
public void SaveBulkEdit(BulkEditViewModel model)
{
    // 親エンティティを更新
    _parentRepository.Update(model.Parent);

    // 削除対象の子を論理削除
    foreach (var id in model.DeleteChildIds)
        _childRepository.Delete(id);

    // 子の追加・更新
    foreach (var child in model.Children)
    {
        child.ParentId = model.Parent.Id;
        if (child.Id == 0)
            _childRepository.Insert(child);
        else
            _childRepository.Update(child);
    }
}
```

## View（動的行追加）

```html
<div id="childList">
    @foreach (var child in Model.Children)
    {
        <partial name="_ChildRow" model="child" />
    }
</div>

<button type="button" id="addChild">行を追加</button>
```

```javascript
// 行追加
$('#addChild').on('click', function () {
    $.get('/DatabaseSample/NewChildRow', function (html) {
        $('#childList').append(html);
    });
});

// 行削除
$(document).on('click', '.deleteChild', function () {
    const id = $(this).data('id');
    if (id > 0) {
        // 既存行は削除IDリストに追加
        $('<input>').attr({ type: 'hidden', name: 'DeleteChildIds', value: id })
            .appendTo('#mainForm');
    }
    $(this).closest('.child-row').remove();
});
```

## ポイント

- 既存子の削除は `DeleteChildIds` に ID を収集し、後から一括論理削除
- 新規追加行は `Id = 0` で識別
- パーシャルビューで行テンプレートを切り出すと管理しやすい
