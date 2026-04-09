---
name: add-page
description: 新しいページ・機能を追加した後のチェックリスト。ホーム画面リンク・ナビバー追加、ドキュメント更新、README更新、コミットまでを確認する。
---

新しいページ・機能を追加した後、以下を順番に確認・実施してください。

## 1. ホーム画面にカードリンクを追加

`Views/Home/Index.cshtml` に対応するカードを追加する。

カードは既存の `<div class="row mt-3">` の末尾に `col-md-6` として追加するか、新しい row を作る場合は `mt-3` を使う（先頭の row のみ `mt-4`）。

**通常のカード（ボタン1つ）:**

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

**ボタンが複数ある場合（例: 一覧 + 新規登録）:**

```html
<div class="col-md-6 mb-3">
    <div class="card h-100">
        <div class="card-body">
            <h5 class="card-title"><i class="fas fa-[アイコン名] me-2"></i>[機能名]</h5>
            <p class="card-text">[機能の説明文]</p>
            <a asp-controller="[Controller]" asp-action="Index" class="btn btn-primary me-2">一覧へ</a>
            <a asp-controller="[Controller]" asp-action="Create" class="btn btn-secondary">新規登録</a>
        </div>
    </div>
</div>
```

**Admin 限定の場合（`border-warning` + `btn-warning` を使う）:**

```html
@* ポイント: Admin ロールのみ表示する管理機能カード *@
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

`Views/Shared/_Layout.cshtml` の `<ul class="navbar-nav flex-grow-1">` 内に追加する。

**現在のナビバー構成（コア機能のみ）:**

```html
<ul class="navbar-nav flex-grow-1">
    <li class="nav-item">
        <a class="nav-link text-white" asp-controller="Home" asp-action="Index">ホーム</a>
    </li>
    <li class="nav-item">
        <a class="nav-link text-white" asp-controller="ApprovalRequest" asp-action="Index">承認申請</a>
    </li>
    <li class="nav-item">
        <a class="nav-link text-white" asp-controller="Schedule" asp-action="Index">スケジュール</a>
    </li>
    @* Admin 限定メニュー *@
    @if (User.IsInRole("Admin"))
    {
        <li class="nav-item">
            <a class="nav-link text-white" asp-controller="UserManagement" asp-action="Index">ユーザー管理</a>
        </li>
    }
</ul>
```

**追加パターン（通常）:**

```html
<li class="nav-item">
    <a class="nav-link text-white" asp-controller="[Controller]" asp-action="Index">[メニュー名]</a>
</li>
```

**Admin 限定の場合:**

```html
@* ポイント: Admin ロールのみ表示する管理メニュー *@
@if (User.IsInRole("Admin"))
{
    <li class="nav-item">
        <a class="nav-link text-white" asp-controller="[Controller]" asp-action="Index">[メニュー名]</a>
    </li>
}
```

> **注意**: ナビバーはコア機能（全ユーザーが日常的に使う機能）のみ追加する。
> サンプル機能・補助的な機能はホーム画面のカードリンクのみで十分。

## 3. ドキュメントを更新

変更内容に応じて `docs/` 内の該当ドキュメントを更新する。**Markdown と Office ファイルの両方を更新すること。**

| Markdown ファイル | 更新が必要なケース |
|---|---|
| `docs/基本設計書.md` | 機能追加・削除、画面追加・削除 |
| `docs/詳細設計書.md` | コントローラー・サービス・リポジトリ・エンティティ・ViewModel の追加・変更 |
| `docs/DB設計書.md` | テーブル・カラムの追加・変更・削除 |

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
