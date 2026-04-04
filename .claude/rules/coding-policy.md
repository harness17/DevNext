# コーディング方針

- **メンテナンス性を最優先**とする
- **可能な限りコメントを生成**する

## エンティティ設計ルール

- すべてのエンティティは **`SiteEntityBase` を継承**すること（`Id: long` + 共通監査カラムを統一するため）
- 更新履歴が必要なエンティティは **`SampleEntity` と同じパターン**（`Base` 抽象クラス → 本体クラス + `History` クラス）で実装すること
  ```
  XxxEntityBase (abstract) : SiteEntityBase
    ├── XxxEntity          // 本体テーブル
    └── XxxEntityHistory   // 履歴テーブル（HistoryId: long [Key], IEntityHistory）
  ```
