# DevNext 開発ガイド

## このドキュメントについて

DevNextのリファクタリング方針と作業指針をまとめたものです。
ClaudeCodeがDevNextで作業する際は、必ずこのドキュメントを参照してください。

---

## 背景・方針転換

### 問題

- サンプル機能が多岐にわたり、コードの全体像が把握しにくくなった
- 「参考になるサンプル」の限界点を超え、複雑化するだけになってきた
- 新案件でテンプレートとして使うには「削除作業」が多すぎる

### 方針

**DevNextはサンプル集ではなく「新案件の出発点となるコアテンプレート」に整理する。**

- サンプル機能は別プロジェクト（`Samples/`配下）に切り出し、本体から独立させる
- コアはシンプルに保ち、新案件時に余計なものを消す手間をなくす
- パターンの参照は `docs/recipes/` のドキュメントで行う

---

## 新しいプロジェクト構造（目標）

```
DevNext/                   ← コアのみ（認証・基本CRUD構造・共通設定）
CommonLibrary/             ← そのまま維持
DbMigrationRunner/         ← そのまま維持（Coreのエンティティに合わせて調整）
BatchSample/               ← そのまま維持
Samples/
  DatabaseSample/          ← 独立した .csproj プロジェクト
  FileSample/              ← 独立した .csproj プロジェクト
  MailSample/              ← 独立した .csproj プロジェクト
  WizardSample/            ← 独立した .csproj プロジェクト
docs/
  recipes/                 ← パターン集（Excel出力・PDF・一括編集 等）
  setup.md                 ← セットアップ手順（現READMEから移動・充実）
  customization.md         ← 新案件向けカスタマイズ指針
```

---

## 作業リスト

### 削除するもの

- [ ] `DevNext/Controllers/DashboardController.cs`
- [ ] `DevNext/Service/DashboardService.cs`
- [ ] `DevNext/Views/Dashboard/` 配下すべて
- [ ] ダッシュボード関連のナビゲーションリンク・DI登録

> **理由**: ダッシュボードはDBサンプル・メール・ファイル・Wizardの全サンプルに依存しており、
> サンプルを分離した後はコア単独では成立しないため削除する。

### コア（DevNext本体）に残すもの

- `AccountController` / `ManageController` ― 認証・アカウント管理
- `UserManagementController` ― ユーザー・ロール管理（Admin限定）
- `HomeController` ― ホーム画面（シンプルな状態に整理）
- `RootErrorController` ― エラーハンドリング
- 基本的なCRUDの骨格（検索・ページング・ソート程度のシンプルな例）
- `CommonLibrary` への参照

### Samples/ に移動するもの

| 移動元 | 移動先プロジェクト | 含む内容 |
|---|---|---|
| `Controllers/DatabaseSampleController.cs` + Service + Repository + Views | `Samples/DatabaseSample/` | CRUD・一括編集・Excel/PDF・親子エンティティ |
| `Controllers/FileManagementController.cs` + Service + Repository + Views | `Samples/FileSample/` | ファイルアップロード・ダウンロード・削除 |
| `Controllers/MailSampleController.cs` + Service + Views | `Samples/MailSample/` | テンプレートメール送信 |
| `Controllers/WizardSampleController.cs` + Service + Repository + Views | `Samples/WizardSample/` | 多段階フォーム |

各Sampleプロジェクトは `CommonLibrary` を参照し、単独でビルド・起動できる独立したWebアプリとする。

### ドキュメント整備

- [ ] `docs/setup.md` ― セットアップ手順（DB作成・初期ユーザー・接続文字列設定）
- [ ] `docs/customization.md` ― 新案件向け変更箇所ガイド（名前空間・DB名・認証設定 等）
- [ ] `docs/recipes/excel-export.md` ― Excelエクスポートのパターン
- [ ] `docs/recipes/pdf-export.md` ― PDF生成のパターン
- [ ] `docs/recipes/bulk-edit.md` ― 一括編集（親＋子エンティティ）のパターン
- [ ] `docs/recipes/file-upload.md` ― ファイルアップロードのパターン
- [ ] `docs/recipes/wizard.md` ― 多段階フォームのパターン
- [ ] `README.md` の更新（新構造に合わせて記載整理）

---

## コアテンプレートの設計指針

### やること
- 認証まわりは完全に動く状態を維持する
- 基本的なCRUD（一覧・詳細・作成・編集・削除）の骨格を1つ残す
  - 複雑な機能（Excel・PDF・一括編集・ファイル添付）は含めない
  - ページング・ソート・検索は含める（どの案件でも必要なため）
- `Program.cs` のDI登録をサンプル依存から解放する
- ナビゲーションをコア機能だけにする

### やらないこと
- これ以上サンプル機能をコア本体に追加しない
- 「あったら便利かも」な機能を先回りして追加しない
- CommonLibraryをむやみに拡張しない（必要になったときだけ追加）

---

## Sampleプロジェクトの設計指針

- 各Sampleは `CommonLibrary` を参照してよい
- Sample同士は依存しない（FileSampleがMailSampleを参照するなど禁止）
- 各Sampleは単独でビルド・起動できる状態にする
- Sample内のエンティティ・DBContextはSample専用とし、コアのDBContextとは分離する
- Seedデータが必要なSampleは、Sample内に簡易なSeederを持つ

---

## 作業時の注意

- 作業は機能単位で1つずつ進める（全部まとめてリファクタリングしない）
- 移動・削除の前に影響範囲（参照・DI登録・ナビゲーション）を確認する
- `DevNext.sln` のプロジェクト参照はSample追加・削除のたびに更新する
- ビルドが通ることを各ステップで確認してからコミットする
