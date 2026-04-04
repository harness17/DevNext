---
name: add-page
description: 新しいページ・機能を追加した後のチェックリスト。ホーム画面リンク・ナビバー追加、ドキュメント更新、README更新、コミットまでを確認する。
---

新しいページ・機能を追加した後、以下を順番に確認・実施してください。

## 1. ホーム画面にカードリンクを追加

`Views/Home/Index.cshtml` に対応するカードを追加する。

```html
<div class="col-md-6 mb-3">
    <div class="card h-100">
        <div class="card-body">
            <h5 class="card-title"><i class="fas fa-[アイコン名] me-2"></i>[機能名]</h5>
            <p class="card-text">[機能の説明文]</p>
            <a asp-controller="[Controller]" asp-action="Index" class="btn btn-primary">一覧へ</a>
        </div>
    </div>
</div>
```

**Admin 限定の場合:**

```html
@if (User.IsInRole("Admin"))
{
    <div class="col-md-6 mb-3">
        <div class="card h-100 border-warning">
            <div class="card-body">
                <h5 class="card-title"><i class="fas fa-[アイコン名] me-2"></i>[機能名]</h5>
                <p class="card-text">[機能の説明文]（Admin限定）</p>
                <a asp-controller="[Controller]" asp-action="Index" class="btn btn-warning">一覧へ</a>
            </div>
        </div>
    </div>
}
```

## 2. ナビバーにリンクを追加

`Views/Shared/_Layout.cshtml` のナビバーにも対応するリンクを追加する。
ロール制限がある場合は同様に `@if (User.IsInRole("ロール名"))` で囲む。

## 3. ドキュメントを更新

変更内容に応じて `doc/` 内の該当ドキュメントを更新する。**Markdown と Office ファイルの両方を更新すること。**

| Markdown ファイル | 更新が必要なケース |
|---|---|
| `doc/基本設計書.md` | 機能追加・削除、画面追加・削除 |
| `doc/詳細設計書.md` | コントローラー・サービス・リポジトリ・エンティティ・ViewModel の追加・変更 |
| `doc/DB設計書.md` | テーブル・カラムの追加・変更・削除 |

Markdown を更新したら、対応する Office ファイルも `/export-docs` で再生成する。

| 変更内容 | 再生成が必要なファイル |
|---|---|
| 機能追加・画面追加 | 基本設計書.docx・詳細設計書.docx・画面設計書.docx |
| テーブル・カラム変更 | DB設計書.xlsx |
| テスト追加・変更 | テスト設計書.docx・テストケース一覧.xlsx |
| API エンドポイント変更 | API仕様書.docx |

## 4. README を更新

以下のケースでは `README.md` も更新する。

- 新機能・新ページを追加したとき（機能一覧への追記）
- 既存機能を削除・統合したとき
- 技術スタックや依存ライブラリを変更したとき
- プロジェクト構成（フォルダ・名前空間）が変わったとき

## 5. コミット

README・ドキュメントの更新も同じコミットに含めてコミットする。
