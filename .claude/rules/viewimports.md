# _ViewImports.cshtml チェックリスト

## 問題

Razor ビューで型が解決できずに 500 エラーが発生する場合、`_ViewImports.cshtml` の `@using` 漏れが原因のことが多い。

## チェックタイミング

以下のいずれかを実施したとき、`DevNext/Views/_ViewImports.cshtml` を確認すること。

| 変更内容 | 確認事項 |
|---------|---------|
| 新しいエンティティクラスを追加した | エンティティの名前空間が `@using` に含まれているか |
| 共通ライブラリから新しい型をビューで使うようにした | `Dev.CommonLibrary.XXX` の `@using` が含まれているか |
| 新しいビューモデルを追加した | `Site.Models` / `Site.Entity` 等が含まれているか |
| ビルドは通るのにブラウザで 500 が出る | `_ViewImports.cshtml` を最初に確認する |

## 現在の登録済み名前空間

```razor
@using Site.Models
@using Site.Common
@using Site.Entity
@using Dev.CommonLibrary.Entity
@using Microsoft.AspNetCore.Mvc.Rendering
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

## 追加方法

新しい名前空間が必要になった場合は末尾（`@addTagHelper` の前）に追加する。

```razor
@using Site.Models
@using Site.Common
@using Site.Entity
@using Dev.CommonLibrary.Entity
@using [追加する名前空間]          ← ここに追加
@using Microsoft.AspNetCore.Mvc.Rendering
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```
