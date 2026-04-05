# CommonLibrary 責務境界ルール

## 原則

> **「このライブラリを別プロジェクトに持っていっても使える」ものだけを置く。**

## ホワイトリスト（入れていいカテゴリ）

| カテゴリ | 具体例 |
|---------|-------|
| Entity 基底 | `EntityBase`, `SiteEntityBase`, `IEntity` |
| Identity エンティティ | `ApplicationUser`, `ApplicationRole`, `UserPreviousPassword` |
| Repository 基底 | `RepositoryBase`, `IRepository` |
| ページング | `CommonListPagerModel`, `CommonListSummaryModel` |
| ロギング | `Logger`, `LogModel`, `ILogModel` |
| 属性 | `AccessLogAttribute`, `SubValueAttribute`, `FileAttribute` |
| 拡張メソッド | `StringExtensions`, `EnumExtensions` など |
| バッチ基底 | `IBatch`, `BatchService` |

## ブラックリスト（入れてはいけないもの）

| NG の例 | 理由 |
|--------|------|
| アプリ固有の定数・Enum | プロジェクト依存 |
| Sample 固有のヘルパー | Sample は CommonLibrary を使う側 |
| 特定機能の業務ロジック | 呼び出し側プロジェクトに書く |

## 既存クラスへの追記ルール

> **既存クラスへの追記が許されるのは、そのクラスの責務名で説明できるときだけ。**
> 説明できなければ新クラスを作る。

## グレーゾーンの判断ゲート（3問チェック）

ホワイトリストに当てはまらないものを追加するとき：

```
① 他プロジェクトに持っていっても使えるか？
     NO → CommonLibrary には入れない。呼び出し側に書く。

② 単一の責務に収まるか？
     NO → CommonLibrary には入れない。責務を分解してから再検討。

③ 独立したクラスとして成立するか？
     NO → 既存クラスの責務名で説明できるか確認。できなければ新クラスを作る。
     YES → 新クラスとして追加する。
```
