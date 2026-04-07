# DevNext 開発ガイド

## プロジェクト概要

- **種別**: ASP.NET Core 10 MVC Webアプリ（ポートフォリオ兼テンプレート）
- **目的**: 新案件の出発点となるコアテンプレート（認証・ユーザー管理・基本CRUD）
- **言語**: C# / Razor Views / JavaScript (jQuery)

```
DevNext/          ← メインWebアプリ（RootNamespace: Site）
CommonLibrary/    ← 共通ライブラリ（RootNamespace: Dev.CommonLibrary）
BatchSample/      ← バッチ処理サンプル（RootNamespace: BatchSample）
Tests/            ← xUnit テストプロジェクト
Samples/          ← 独立したサンプルプロジェクト群
docs/             ← 設計書・実装計画
scripts/          ← 開発補助スクリプト
```

---

## 方針

**DevNextはサンプル集ではなく「新案件の出発点となるコアテンプレート」。**

- サンプル機能は `Samples/` 配下の独立プロジェクトに分離済み
- コアはシンプルに保つ。新機能を先回りして追加しない
- パターンの参照は `docs/recipes/` のドキュメントで行う

---

## コアテンプレートの設計指針

### やること
- 認証まわりは完全に動く状態を維持する
- 基本的なCRUD骨格（ページング・ソート・検索を含む）を維持する
- `Program.cs` のDI登録をサンプル依存から解放した状態を保つ

### やらないこと
- これ以上サンプル機能をコア本体に追加しない
- 「あったら便利かも」な機能を先回りして追加しない
- CommonLibraryをむやみに拡張しない（必要になったときだけ追加）

---

## Sampleプロジェクトの設計指針

- 各Sampleは `CommonLibrary` を参照してよい
- **Sample同士は依存しない**（FileSampleがMailSampleを参照するなど禁止）
- 各Sampleは単独でビルド・起動できる状態にする
- Sample内のエンティティ・DBContextはSample専用とし、コアのDBContextとは分離する

---

## 作業時の注意

- 作業は機能単位で1つずつ進める（全部まとめてリファクタリングしない）
- 移動・削除の前に影響範囲（参照・DI登録・ナビゲーション）を確認する
- `DevNext.sln` のプロジェクト参照はSample追加・削除のたびに更新する
- ビルドが通ることを各ステップで確認してからコミットする

---

## ルール参照

@rules/coding-policy.md
@rules/namespaces.md
@rules/di-and-password.md
@rules/database.md
@rules/commands.md
@rules/testing.md
@rules/sprint-contract.md
@rules/evaluator.md
@rules/add-page-trigger.md
@rules/document-output.md
@rules/context-reset.md
@rules/my-skill-graph.md
@rules/branching-and-merge.md
@rules/viewimports.md
@rules/git-ops.md
@rules/commonlibrary-boundary.md
