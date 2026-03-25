# DevNext DB 設計書

| 項目 | 内容 |
|------|------|
| ドキュメント名 | DevNext DB 設計書 |
| バージョン | 1.0 |
| 作成日 | 2026-03-23 |
| 対象システム | DevNext Web アプリケーション |
| データベース | SQL Server |
| データベース名 | DevNextDB |

---

## 目次

1. [テーブル一覧](#1-テーブル一覧)
2. [共通カラム定義](#2-共通カラム定義)
3. [テーブル定義](#3-テーブル定義)
   - [ApplicationUser](#31-applicationuser)
   - [ApplicationRole](#32-applicationrole)
   - [ApplicationUserRole](#33-applicationuserrole)
   - [ApplicationUserClaim](#34-applicationuserclaim)
   - [ApplicationUserLogin](#35-applicationuserlogin)
   - [ApplicationUserToken](#36-applicationusertoken)
   - [ApplicationRoleClaim](#37-applicationroleclaim)
   - [UserPreviousPassword](#38-userpreviouspassword)
   - [SampleEntity](#39-sampleentity)
   - [SampleEntityHistory](#310-sampleentityhistory)
   - [SampleEntityChild](#311-sampleentitychild)
   - [SampleEntityChildHistory](#312-sampleentitychildhistory)
   - [FileEntity](#313-fileentity)
   - [WizardEntity](#314-wizardentity)
   - [MailLog](#315-maillog)
4. [ER 図](#4-er-図)
5. [Enum 定義](#5-enum-定義)
6. [初期データ](#6-初期データ)
7. [接続設定](#7-接続設定)

---

## 1. テーブル一覧

| # | テーブル名 | 概要 | 種別 |
|---|-----------|------|------|
| 1 | ApplicationUser | ユーザーアカウント | Identity |
| 2 | ApplicationRole | ロール定義 | Identity |
| 3 | ApplicationUserRole | ユーザーとロールの関連 | Identity |
| 4 | ApplicationUserClaim | ユーザークレーム | Identity |
| 5 | ApplicationUserLogin | 外部ログインプロバイダー | Identity |
| 6 | ApplicationUserToken | ユーザートークン | Identity |
| 7 | ApplicationRoleClaim | ロールクレーム | Identity |
| 8 | UserPreviousPassword | パスワード履歴 | アプリ |
| 9 | SampleEntity | サンプルエンティティ（親） | アプリ |
| 10 | SampleEntityHistory | SampleEntity 履歴 | アプリ |
| 11 | SampleEntityChild | サンプルエンティティ（子） | アプリ |
| 12 | SampleEntityChildHistory | SampleEntityChild 履歴 | アプリ |
| 13 | FileEntity | ファイルメタ情報 | アプリ |
| 14 | WizardEntity | 多段階フォームデータ | アプリ |
| 15 | MailLog | メール送信ログ | アプリ |

---

## 2. 共通カラム定義

アプリテーブル（SiteEntityBase 継承）の共通カラム。

| カラム名 | 型 | NULL | 説明 |
|---------|---|------|------|
| `Id` | bigint | NOT NULL | 主キー（IDENTITY） |
| `DelFlag` | bit | NOT NULL | 論理削除フラグ（0：有効、1：削除済み） |
| `CreateDate` | datetime2 | NOT NULL | 作成日時 |
| `UpdateDate` | datetime2 | NOT NULL | 最終更新日時 |
| `CreateApplicationUserId` | nvarchar(max) | NULL | 作成者ユーザー ID |
| `UpdateApplicationUserId` | nvarchar(max) | NULL | 最終更新者ユーザー ID |

---

## 3. テーブル定義

### 3.1 ApplicationUser

ASP.NET Core Identity の `IdentityUser` をカスタマイズしたユーザーテーブル。

| カラム名 | 型 | NULL | 制約 | 説明 |
|---------|---|------|------|------|
| `Id` | nvarchar(450) | NOT NULL | PK | ユーザー ID（GUID 文字列） |
| `UserName` | nvarchar(256) | NULL | - | ユーザー名 |
| `NormalizedUserName` | nvarchar(256) | NULL | UNIQUE INDEX | 正規化ユーザー名（大文字） |
| `Email` | nvarchar(256) | NULL | - | メールアドレス |
| `NormalizedEmail` | nvarchar(256) | NULL | INDEX | 正規化メールアドレス（大文字） |
| `EmailConfirmed` | bit | NOT NULL | - | メール確認済みフラグ |
| `PasswordHash` | nvarchar(max) | NULL | - | パスワードハッシュ（PBKDF2） |
| `SecurityStamp` | nvarchar(max) | NULL | - | セキュリティスタンプ |
| `ConcurrencyStamp` | nvarchar(max) | NULL | - | 同時実行制御スタンプ |
| `PhoneNumber` | nvarchar(max) | NULL | - | 電話番号 |
| `PhoneNumberConfirmed` | bit | NOT NULL | - | 電話番号確認済みフラグ |
| `TwoFactorEnabled` | bit | NOT NULL | - | 二要素認証有効フラグ |
| `LockoutEnd` | datetimeoffset(7) | NULL | - | ロックアウト終了日時 |
| `LockoutEnabled` | bit | NOT NULL | - | ロックアウト有効フラグ |
| `AccessFailedCount` | int | NOT NULL | - | ログイン失敗回数 |
| `ResetPasswordTimeOut` | datetime2 | NULL | - | パスワードリセットタイムアウト日時 |
| `PasswordAvailableEndDate` | datetime2 | NULL | - | パスワード有効期限 |
| `ApplicationRoleName` | nvarchar(max) | NULL | - | ロール名（表示用） |
| `UpdateApplicationUserId` | nvarchar(max) | NULL | - | 最終更新者ユーザー ID |

---

### 3.2 ApplicationRole

ロール定義テーブル。

| カラム名 | 型 | NULL | 制約 | 説明 |
|---------|---|------|------|------|
| `Id` | nvarchar(450) | NOT NULL | PK | ロール ID（GUID 文字列） |
| `Name` | nvarchar(256) | NULL | - | ロール名 |
| `NormalizedName` | nvarchar(256) | NULL | UNIQUE INDEX | 正規化ロール名（大文字） |
| `ConcurrencyStamp` | nvarchar(max) | NULL | - | 同時実行制御スタンプ |

**初期データ：**

| Id | Name | NormalizedName |
|----|------|----------------|
| *(GUID)* | Admin | ADMIN |
| *(GUID)* | Member | MEMBER |

---

### 3.3 ApplicationUserRole

ユーザーとロールの多対多関連テーブル。

| カラム名 | 型 | NULL | 制約 | 説明 |
|---------|---|------|------|------|
| `UserId` | nvarchar(450) | NOT NULL | PK, FK → ApplicationUser.Id | ユーザー ID |
| `RoleId` | nvarchar(450) | NOT NULL | PK, FK → ApplicationRole.Id | ロール ID |

---

### 3.4 ApplicationUserClaim

ユーザークレームテーブル。

| カラム名 | 型 | NULL | 制約 | 説明 |
|---------|---|------|------|------|
| `Id` | int | NOT NULL | PK, IDENTITY | クレーム ID |
| `UserId` | nvarchar(450) | NOT NULL | FK → ApplicationUser.Id | ユーザー ID |
| `ClaimType` | nvarchar(max) | NULL | - | クレーム種別 |
| `ClaimValue` | nvarchar(max) | NULL | - | クレーム値 |

---

### 3.5 ApplicationUserLogin

外部ログインプロバイダー関連テーブル。

| カラム名 | 型 | NULL | 制約 | 説明 |
|---------|---|------|------|------|
| `LoginProvider` | nvarchar(450) | NOT NULL | PK | ログインプロバイダー |
| `ProviderKey` | nvarchar(450) | NOT NULL | PK | プロバイダーキー |
| `ProviderDisplayName` | nvarchar(max) | NULL | - | プロバイダー表示名 |
| `UserId` | nvarchar(450) | NOT NULL | FK → ApplicationUser.Id | ユーザー ID |

---

### 3.6 ApplicationUserToken

ユーザートークンテーブル。

| カラム名 | 型 | NULL | 制約 | 説明 |
|---------|---|------|------|------|
| `UserId` | nvarchar(450) | NOT NULL | PK, FK → ApplicationUser.Id | ユーザー ID |
| `LoginProvider` | nvarchar(450) | NOT NULL | PK | ログインプロバイダー |
| `Name` | nvarchar(450) | NOT NULL | PK | トークン名 |
| `Value` | nvarchar(max) | NULL | - | トークン値 |

---

### 3.7 ApplicationRoleClaim

ロールクレームテーブル。

| カラム名 | 型 | NULL | 制約 | 説明 |
|---------|---|------|------|------|
| `Id` | int | NOT NULL | PK, IDENTITY | クレーム ID |
| `RoleId` | nvarchar(450) | NOT NULL | FK → ApplicationRole.Id | ロール ID |
| `ClaimType` | nvarchar(max) | NULL | - | クレーム種別 |
| `ClaimValue` | nvarchar(max) | NULL | - | クレーム値 |

---

### 3.8 UserPreviousPassword

パスワード履歴テーブル。過去に使用したパスワードを保持し、同一パスワードの再使用を防止する。

| カラム名 | 型 | NULL | 制約 | 説明 |
|---------|---|------|------|------|
| `Id` | int | NOT NULL | PK, IDENTITY | 履歴 ID |
| `ApplicationUserId` | nvarchar(450) | NOT NULL | FK → ApplicationUser.Id | ユーザー ID |
| `PasswordHash` | nvarchar(max) | NOT NULL | - | パスワードハッシュ |
| `CreateDate` | datetime2 | NOT NULL | - | 登録日時 |

---

### 3.9 SampleEntity

CRUD サンプルの親エンティティテーブル。共通カラムに加え以下を持つ。

| カラム名 | 型 | NULL | 制約 | 説明 |
|---------|---|------|------|------|
| `Id` | bigint | NOT NULL | PK, IDENTITY | エンティティ ID |
| `ApplicationUserId` | nvarchar(128) | NULL | - | 関連ユーザー ID |
| `StringData` | nvarchar(max) | NOT NULL | - | 文字列データ（必須） |
| `IntData` | int | NOT NULL | - | 数値データ |
| `BoolData` | bit | NOT NULL | - | 真偽値データ |
| `EnumData` | int | NOT NULL | - | 列挙値 1（SampleEnum） |
| `EnumData2` | int | NOT NULL | - | 列挙値 2（SampleEnum2） |
| `FileData` | nvarchar(max) | NULL | - | 添付ファイルのパス |
| `DelFlag` | bit | NOT NULL | - | 論理削除フラグ |
| `CreateDate` | datetime2 | NOT NULL | - | 作成日時 |
| `UpdateDate` | datetime2 | NOT NULL | - | 最終更新日時 |
| `CreateApplicationUserId` | nvarchar(max) | NULL | - | 作成者ユーザー ID |
| `UpdateApplicationUserId` | nvarchar(max) | NULL | - | 最終更新者ユーザー ID |

---

### 3.10 SampleEntityHistory

SampleEntity の変更履歴テーブル。SampleEntity と同じカラム構成に `HistoryId` を追加。

| カラム名 | 型 | NULL | 制約 | 説明 |
|---------|---|------|------|------|
| `HistoryId` | bigint | NOT NULL | PK, IDENTITY | 履歴 ID |
| `Id` | bigint | NOT NULL | - | 元エンティティの ID |
| `ApplicationUserId` | nvarchar(128) | NULL | - | 関連ユーザー ID |
| `StringData` | nvarchar(max) | NOT NULL | - | 文字列データ |
| `IntData` | int | NOT NULL | - | 数値データ |
| `BoolData` | bit | NOT NULL | - | 真偽値データ |
| `EnumData` | int | NOT NULL | - | 列挙値 1 |
| `EnumData2` | int | NOT NULL | - | 列挙値 2 |
| `FileData` | nvarchar(max) | NULL | - | 添付ファイルのパス |
| `DelFlag` | bit | NOT NULL | - | 論理削除フラグ |
| `CreateDate` | datetime2 | NOT NULL | - | 元データの作成日時 |
| `UpdateDate` | datetime2 | NOT NULL | - | 変更が記録された日時 |
| `CreateApplicationUserId` | nvarchar(max) | NULL | - | 作成者ユーザー ID |
| `UpdateApplicationUserId` | nvarchar(max) | NULL | - | 変更者ユーザー ID |

---

### 3.11 SampleEntityChild

CRUD サンプルの子エンティティテーブル。SampleEntity と 1:N の関係。

| カラム名 | 型 | NULL | 制約 | 説明 |
|---------|---|------|------|------|
| `Id` | bigint | NOT NULL | PK, IDENTITY | エンティティ ID |
| `SumpleEntityID` | bigint | NOT NULL | - | 親エンティティ ID（SampleEntity.Id） ※typo |
| `ApplicationUserId` | nvarchar(128) | NULL | - | 関連ユーザー ID |
| `StringData` | nvarchar(max) | NOT NULL | - | 文字列データ（必須） |
| `IntData` | int | NOT NULL | - | 数値データ |
| `BoolData` | bit | NOT NULL | - | 真偽値データ |
| `DelFlag` | bit | NOT NULL | - | 論理削除フラグ |
| `CreateDate` | datetime2 | NOT NULL | - | 作成日時 |
| `UpdateDate` | datetime2 | NOT NULL | - | 最終更新日時 |
| `CreateApplicationUserId` | nvarchar(max) | NULL | - | 作成者ユーザー ID |
| `UpdateApplicationUserId` | nvarchar(max) | NULL | - | 最終更新者ユーザー ID |

> **注意：** `SumpleEntityID` は `SampleEntityID` のタイポ。FK 制約は EF Core レベルでは未定義（NavProperty なし）。

---

### 3.12 SampleEntityChildHistory

SampleEntityChild の変更履歴テーブル。

| カラム名 | 型 | NULL | 制約 | 説明 |
|---------|---|------|------|------|
| `HistoryId` | bigint | NOT NULL | PK, IDENTITY | 履歴 ID |
| `Id` | bigint | NOT NULL | - | 元エンティティの ID |
| `SumpleEntityID` | bigint | NOT NULL | - | 親エンティティ ID |
| `ApplicationUserId` | nvarchar(128) | NULL | - | 関連ユーザー ID |
| `StringData` | nvarchar(max) | NOT NULL | - | 文字列データ |
| `IntData` | int | NOT NULL | - | 数値データ |
| `BoolData` | bit | NOT NULL | - | 真偽値データ |
| `DelFlag` | bit | NOT NULL | - | 論理削除フラグ |
| `CreateDate` | datetime2 | NOT NULL | - | 元データの作成日時 |
| `UpdateDate` | datetime2 | NOT NULL | - | 変更が記録された日時 |
| `CreateApplicationUserId` | nvarchar(max) | NULL | - | 作成者ユーザー ID |
| `UpdateApplicationUserId` | nvarchar(max) | NULL | - | 変更者ユーザー ID |

---

### 3.13 FileEntity

ファイル管理のメタ情報テーブル。物理ファイルは `wwwroot/Uploads/Files/` に保存。

| カラム名 | 型 | NULL | 制約 | 説明 |
|---------|---|------|------|------|
| `Id` | bigint | NOT NULL | PK, IDENTITY | ファイル ID |
| `OriginalFileName` | nvarchar(260) | NOT NULL | - | アップロード時の元ファイル名 |
| `SavedFileName` | nvarchar(260) | NOT NULL | - | サーバー上の保存ファイル名（GUID + 拡張子） |
| `FileSize` | bigint | NOT NULL | - | ファイルサイズ（バイト） |
| `ContentType` | nvarchar(100) | NOT NULL | - | MIME タイプ（Content-Type） |
| `Description` | nvarchar(500) | NULL | - | ファイルの説明（任意） |
| `DelFlag` | bit | NOT NULL | - | 論理削除フラグ |
| `CreateDate` | datetime2 | NOT NULL | - | 作成日時 |
| `UpdateDate` | datetime2 | NOT NULL | - | 最終更新日時 |
| `CreateApplicationUserId` | nvarchar(max) | NULL | - | 作成者ユーザー ID |
| `UpdateApplicationUserId` | nvarchar(max) | NULL | - | 最終更新者ユーザー ID |

---

### 3.14 WizardEntity

多段階フォームサンプルの保存テーブル。ウィザード完了時に全ステップのデータをまとめて保存。

| カラム名 | 型 | NULL | 制約 | 説明 |
|---------|---|------|------|------|
| `Id` | bigint | NOT NULL | PK, IDENTITY | エンティティ ID |
| `Name` | nvarchar(100) | NOT NULL | - | 氏名（Step 1） |
| `Email` | nvarchar(256) | NOT NULL | - | メールアドレス（Step 1） |
| `Phone` | nvarchar(20) | NULL | - | 電話番号（Step 1、任意） |
| `Subject` | nvarchar(200) | NOT NULL | - | 件名（Step 2） |
| `Content` | nvarchar(2000) | NOT NULL | - | 内容（Step 2） |
| `Category` | int | NOT NULL | - | カテゴリ（Step 2、WizardCategory Enum） |
| `DesiredDate` | datetime2 | NULL | - | 希望対応日（Step 2、任意） |
| `DelFlag` | bit | NOT NULL | - | 論理削除フラグ |
| `CreateDate` | datetime2 | NOT NULL | - | 作成日時 |
| `UpdateDate` | datetime2 | NOT NULL | - | 最終更新日時 |
| `CreateApplicationUserId` | nvarchar(max) | NULL | - | 作成者ユーザー ID |
| `UpdateApplicationUserId` | nvarchar(max) | NULL | - | 最終更新者ユーザー ID |

---

### 3.15 MailLog

お問い合わせフォームからのメール送信結果を記録するログテーブル。
ログデータのため論理削除・履歴テーブルは持たない。

| カラム名 | 型 | NULL | 制約 | 説明 |
|---------|---|------|------|------|
| `Id` | bigint | NOT NULL | PK, IDENTITY | ログ ID |
| `SenderName` | nvarchar(100) | NOT NULL | - | 送信者名（フォーム入力値） |
| `SenderEmail` | nvarchar(256) | NOT NULL | - | 送信者メールアドレス（フォーム入力値） |
| `Subject` | nvarchar(200) | NOT NULL | - | 件名（フォーム入力値） |
| `Body` | nvarchar(2000) | NOT NULL | - | 本文（フォーム入力値） |
| `IsSuccess` | bit | NOT NULL | - | 送信成功フラグ（1: 成功、0: 失敗） |
| `ErrorMessage` | nvarchar(1000) | NULL | - | エラーメッセージ（送信失敗時のみセット） |
| `DelFlag` | bit | NOT NULL | - | 論理削除フラグ（ログのため通常は false） |
| `CreateDate` | datetime2 | NOT NULL | - | 送信日時（＝レコード作成日時） |
| `UpdateDate` | datetime2 | NOT NULL | - | 最終更新日時 |
| `CreateApplicationUserId` | nvarchar(max) | NULL | - | 作成者ユーザー ID |
| `UpdateApplicationUserId` | nvarchar(max) | NULL | - | 最終更新者ユーザー ID |

---

## 4. ER 図

```
ApplicationUser ──┬── ApplicationUserRole ── ApplicationRole
                  │                               │
                  │                               └── ApplicationRoleClaim
                  ├── ApplicationUserClaim
                  ├── ApplicationUserLogin
                  ├── ApplicationUserToken
                  └── UserPreviousPassword


SampleEntity ────── SampleEntityChild
     │                      │
     └── SampleEntityHistory └── SampleEntityChildHistory


FileEntity（独立）

WizardEntity（独立）

MailLog（独立）
```

### 関連詳細

| 関連 | 種別 | 説明 |
|------|------|------|
| ApplicationUser → ApplicationUserRole | 1:N | ユーザーが複数ロールを持つ |
| ApplicationRole → ApplicationUserRole | 1:N | ロールに複数ユーザーが所属 |
| ApplicationUser → UserPreviousPassword | 1:N | ユーザーが複数のパスワード履歴を持つ |
| SampleEntity → SampleEntityChild | 1:N | 親エンティティが複数の子を持つ |
| SampleEntity → SampleEntityHistory | 1:N | 変更の都度履歴レコードを追加 |
| SampleEntityChild → SampleEntityChildHistory | 1:N | 変更の都度履歴レコードを追加 |

---

## 5. Enum 定義

### ApplicationRoleType（ロール種別）

| 値 | 名前 | 表示名 |
|---|------|--------|
| 1 | Admin | 管理者 |
| 2 | Member | 運営者 |

### SampleEnum（サンプル列挙値 1）

SampleEntity.EnumData に使用。

| 値 | 名前 | 表示名 |
|---|------|--------|
| 0 | select1 | 選択肢1 |
| 2 | select2 | 選択肢2 |
| 3 | select3 | 選択肢3 |

### SampleEnum2（サンプル列挙値 2）

SampleEntity.EnumData2 に使用。

| 値 | 名前 | 表示名 |
|---|------|--------|
| 0 | select21 | 選択肢21 |
| 2 | select22 | 選択肢22 |
| 3 | select23 | 選択肢23 |

### WizardCategory（ウィザードカテゴリ）

WizardEntity.Category に使用。

| 値 | 名前 | 表示名 |
|---|------|--------|
| 1 | Inquiry | お問い合わせ |
| 2 | Request | ご要望 |
| 3 | BugReport | 不具合報告 |
| 4 | Other | その他 |

### ErrorType（エラー種別）

エラーページの種別区分。

| 値 | 名前 | 表示名 |
|---|------|--------|
| 1 | syserror | システムエラー |
| 2 | urlerror | 不正なURLエラー |
| 3 | usererror | 不正な操作 |
| 4 | sessionerror | セッションタイムアウト |
| 5 | cannotuseerror | 使用できない機能 |

---

## 6. 初期データ

DbMigrationRunner によって以下のデータが投入される。

### ロール

| Id | Name | NormalizedName |
|----|------|----------------|
| *(GUID)* | Admin | ADMIN |
| *(GUID)* | Member | MEMBER |

### ユーザー

| UserName | Email | Password | Role |
|----------|-------|----------|------|
| admin1@sample.jp | admin1@sample.jp | Admin1! | Admin |
| member1@sample.jp | member1@sample.jp | Member1! | Member |

> パスワードは ASP.NET Core Identity の PBKDF2 でハッシュ化して保存される。

---

## 7. 接続設定

### 接続文字列

```json
{
  "ConnectionStrings": {
    "SiteConnection": "Server=localhost;Database=DevNextDB;Integrated Security=False;User ID=admin;Password=admin;TrustServerCertificate=True;"
  }
}
```

| パラメータ | 値 | 説明 |
|----------|---|------|
| Server | localhost | SQL Server インスタンス |
| Database | DevNextDB | データベース名 |
| Integrated Security | False | SQL Server 認証を使用 |
| User ID | admin | SQL ユーザー名 |
| Password | admin | SQL パスワード |
| TrustServerCertificate | True | 自己署名証明書を信頼 |

### DB 作成・初期化

```bash
cd DbMigrationRunner
dotnet run
```

`EnsureCreatedAsync()` によりテーブルが自動生成される（Entity Framework Core のモデルに基づく）。

---

*本ドキュメントは DevNext プロジェクトの DB 設計を記述したものです。*
