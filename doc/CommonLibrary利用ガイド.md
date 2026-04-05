# CommonLibrary 利用ガイド

| 項目 | 内容 |
|------|------|
| ドキュメント名 | CommonLibrary 利用ガイド |
| バージョン | 1.0 |
| 作成日 | 2026-03-28 |
| 更新日 | 2026-04-05 |
| 対象システム | DevNext Web アプリケーション |

---

## 目次

1. [概要](#1-概要)
2. [リポジトリ基盤（RepositoryBase）](#2-リポジトリ基盤repositorybase)
3. [エンティティ基底クラス（EntityBase）](#3-エンティティ基底クラスentitybase)
4. [アクセスログ属性（AccessLogAttribute）](#4-アクセスログ属性accesslogattribute)
5. [ファイル検証属性](#5-ファイル検証属性)
6. [バッチ処理基盤（BatchService）](#6-バッチ処理基盤batchservice)
7. [拡張メソッド](#7-拡張メソッド)
8. [ユーティリティクラス](#8-ユーティリティクラス)
9. [ページング・サマリー](#9-ページングサマリー)
10. [ロガー（Logger）](#10-ロガーlogger)
11. [新規プロジェクトへの導入方法](#11-新規プロジェクトへの導入方法)
12. [CommonLibrary への追加ルール](#12-commonlibrary-への追加ルール)

---

## 1. 概要

`CommonLibrary` は、業務 Web アプリケーションで繰り返し必要となる共通機能を集約したクラスライブラリである。
DevNext 本体（`Site` 名前空間）から参照しており、別プロジェクトへそのまま流用できる設計になっている。

### 提供する機能

| カテゴリ | 主なクラス | 説明 |
|---------|-----------|------|
| データアクセス | `RepositoryBase<T, TCond>` | CRUD・論理削除・履歴管理の基盤 |
| エンティティ | `EntityBase` | 作成日時・更新日時・削除フラグの共通管理 |
| フィルター属性 | `AccessLogAttribute` | コントローラーへのアクセスを自動記録 |
| バリデーション属性 | `FileSizeAttribute`, `FileTypesAttribute` | ファイルアップロードの検証 |
| バッチ実行 | `BatchService` | Mutex による多重起動防止 |
| 拡張メソッド | `StringExtensions` など | 文字列・Enum・コレクション操作 |
| ユーティリティ | `Util`, `EnumUtility` など | 共通関数・Enum 操作 |
| ページング | `CommonListPagerModel` | 一覧画面のページング・ソート |

### 名前空間

```
Dev.CommonLibrary.Attributes
Dev.CommonLibrary.Batch
Dev.CommonLibrary.Common
Dev.CommonLibrary.Entity
Dev.CommonLibrary.Extensions
Dev.CommonLibrary.Repository
```

---

## 2. リポジトリ基盤（RepositoryBase）

### 概要

全リポジトリは `RepositoryBase<TEntity, TCondModel>` を継承する。
汎用 CRUD・論理削除・バッチ処理を共通実装として提供し、各リポジトリは差分のみ実装する。

### 基本的な使い方

```csharp
// 1. エンティティに対応するリポジトリを実装
public class SampleEntityRepository
    : RepositoryBase<SampleEntity, SampleEntityCondModel>
{
    public SampleEntityRepository(DBContext context, IMapper mapper)
        : base(context, mapper) { }

    // 差分クエリのみ実装（例：検索条件の適用）
    protected override IQueryable<SampleEntity> BuildQuery(
        IQueryable<SampleEntity> query,
        SampleEntityCondModel cond)
    {
        if (!string.IsNullOrEmpty(cond.Name))
            query = query.Where(e => e.Name.Contains(cond.Name));
        return query;
    }
}

// 2. DI に登録
builder.Services.AddScoped<SampleEntityRepository>();
```

### 提供するメソッド

| メソッド | 説明 |
|---------|------|
| `SelectById(long id)` | ID で1件取得 |
| `Select(TCondModel cond)` | 条件で一覧取得（ページング・ソート対応） |
| `Insert(TEntity entity)` | 1件登録（`SetForCreate()` 自動呼び出し） |
| `Update(TEntity entity)` | 1件更新（`SetForUpdate()` 自動呼び出し） |
| `LogicalDelete(long id)` | 論理削除（`DelFlag = true` に更新） |
| `PhysicalDelete(long id)` | 物理削除（使用は最小限に） |
| `BulkInsert(IList<TEntity>)` | 複数件一括登録 |

### 履歴管理版

`RepositoryBase<TEntity, TEntityHistory, TCondModel>` を使うと、更新のたびに履歴テーブルに自動記録される。

```csharp
public class SampleEntityRepository
    : RepositoryBase<SampleEntity, SampleEntityHistory, SampleEntityCondModel>
{
    // 履歴への変換ロジックを実装
    protected override SampleEntityHistory CreateHistory(SampleEntity entity)
    {
        return mapper.Map<SampleEntityHistory>(entity);
    }
}
```

---

## 3. エンティティ基底クラス（EntityBase）

### 概要

全エンティティは `EntityBase` を継承することで、共通カラムを自動管理できる。

### 提供するカラム

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `CreateUserId` | `string?` | 作成ユーザー ID |
| `UpdateUserId` | `string?` | 最終更新ユーザー ID |
| `CreateTime` | `DateTime` | 作成日時 |
| `UpdateTime` | `DateTime` | 最終更新日時 |
| `DelFlag` | `bool` | 論理削除フラグ（`true` = 削除済み） |

### 使い方

```csharp
// エンティティ定義
public class SampleEntity : EntityBase
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    // ... 業務固有カラム
}
```

### IEntity インターフェース

`EntityBase` は `IEntity` インターフェースを実装している。
リポジトリ側で以下のメソッドが自動呼び出しされる。

| メソッド | 呼び出しタイミング | 処理内容 |
|---------|-----------------|---------|
| `SetForCreate()` | Insert 時 | `CreateTime`, `UpdateTime` を現在時刻に設定 |
| `SetForUpdate()` | Update 時 | `UpdateTime` を現在時刻に更新 |
| `SetForLogicalDelete()` | LogicalDelete 時 | `DelFlag = true` に設定 |

---

## 4. アクセスログ属性（AccessLogAttribute）

### 概要

コントローラーに付与するだけで、アクセスログを DB に自動記録する。
`IActionFilter` の実装で、アクション実行前後に処理を挟む。

### DI 登録（必須）

`ServiceFilter` を使う場合、`Program.cs` への登録が必要。

```csharp
// Program.cs
builder.Services.AddScoped<AccessLogAttribute>();
```

### コントローラーへの適用

```csharp
// コントローラー全体に適用
[Authorize]
[ServiceFilter(typeof(AccessLogAttribute))]
public class DatabaseSampleController : Controller
{
    // ...
}
```

### 記録内容

| 項目 | 内容 |
|------|------|
| コントローラー名 | `[Controller]` 属性または規約から取得 |
| アクション名 | `[Action]` 属性または規約から取得 |
| ユーザー ID | ログインユーザーの Identity ID |
| アクセス日時 | サーバー側タイムスタンプ |

---

## 5. ファイル検証属性

### FileSizeAttribute

ファイルサイズの上限を検証するカスタムバリデーション属性。

```csharp
// ViewModel での使い方
public class FileUploadViewModel
{
    // 最大 10MB のファイルのみ許可
    [FileSize(10 * 1024 * 1024)]
    public IFormFile? UploadFile { get; set; }
}
```

### FileTypesAttribute

許可する拡張子を制限するカスタムバリデーション属性。

```csharp
// ViewModel での使い方
public class FileUploadViewModel
{
    // PDF・Excel・画像ファイルのみ許可
    [FileTypes("pdf,xlsx,xls,jpg,jpeg,png")]
    public IFormFile? UploadFile { get; set; }
}
```

### 組み合わせて使用

```csharp
public class FileUploadViewModel
{
    [FileSize(5 * 1024 * 1024)]       // 最大 5MB
    [FileTypes("jpg,jpeg,png,gif")]    // 画像のみ
    public IFormFile? ImageFile { get; set; }
}
```

---

## 6. バッチ処理基盤（BatchService）

### 概要

`Mutex` を使ってバッチ処理の多重起動を防止する。
バッチアプリ（コンソールアプリ）で使用する。

### 使い方

```csharp
// 1. IBatch インターフェースを実装
public class SampleBatch : IBatch
{
    public void Exec()
    {
        // バッチ処理のメインロジック
        Console.WriteLine("バッチ処理を実行中...");
    }

    public void ExceptionHandler(Exception ex)
    {
        // エラー時の処理（ログ記録など）
        Console.WriteLine($"エラー: {ex.Message}");
    }
}

// 2. BatchService.Run() で実行（Mutex 名でプロセス排他）
var batchService = new BatchService();
batchService.Run(new SampleBatch(), "SampleBatch_Mutex");
```

### 動作

- 同じ `mutexName` のバッチが既に起動中の場合、後から起動したプロセスは即座に終了する
- `Exec()` で例外が発生した場合は `ExceptionHandler()` が呼び出される

---

## 7. 拡張メソッド

### StringExtensions（文字列拡張）

```csharp
using Dev.CommonLibrary.Extensions;

string html = "<script>alert('xss')</script>";

// HTML エンコード（XSS 対策）
string safe = html.HtmlEncode();    // &lt;script&gt;...

// 改行を <br> に変換（テキストエリア内容を HTML 表示）
string br = "Line1\nLine2".GetBrText();  // Line1<br>Line2

// 文字列切り出し（VB.NET 系の感覚で使用可）
string left  = "Hello World".Left(5);    // "Hello"
string right = "Hello World".Right(5);   // "World"
string mid   = "Hello World".Mid(6, 5);  // "World"

// 複数キーワードの存在チェック
bool all = "ABC DEF GHI".ContainsAll("ABC", "GHI");  // true
bool any = "ABC DEF".ContainsAny("XYZ", "ABC");       // true
```

### EnumExtensions（Enum 拡張）

```csharp
using Dev.CommonLibrary.Extensions;

// [Display(Name = "承認待ち")] が付いた Enum の表示名取得
string name = ApprovalStatus.Pending.DisplayName();  // "承認待ち"

// [Description("...")]の取得
string desc = ApprovalStatus.Pending.DisplayDescription();
```

### ObjectExtensions（オブジェクト拡張）

```csharp
// null 安全な ToString
string val = someObject.ToStringOrDefault("デフォルト値");
string val2 = someObject.ToStringOrEmpty();  // null なら ""

// ディープコピー
var copy = original.Clone<MyClass>();
```

### IEnumerableExtension（コレクション拡張）

```csharp
// null またはEmpty チェック
bool empty = list.IsNullOrEmpty();  // null でも例外が出ない
```

---

## 8. ユーティリティクラス

### Util（共通関数）

```csharp
using Dev.CommonLibrary.Common;

// MD5 ハッシュ計算
string hash = Util.CalcMd5("password123");

// ファイルパスからファイル名のみ取得
string fileName = Util.SetFileName(@"C:\path\to\file.txt");  // "file.txt"

// パストラバーサル攻撃の検出
bool safe = Util.IsSafePath("uploads/photo.jpg");        // true
bool danger = Util.IsSafePath("../../etc/passwd");       // false

// ページングサマリー文字列の生成
var summary = Util.CreateSummary(currentPage: 2, pageSize: 10, totalRecords: 95);
// summary.Summary → "11 〜 20 件目（全 95 件）"
```

### EnumUtility（Enum ユーティリティ）

```csharp
using Dev.CommonLibrary.Common;

// Enum の表示名取得
string name = EnumUtility.GetEnumDisplay(ApprovalStatus.Pending);

// Enum の補足値取得（[SubValue(...)] 属性を使用）
string sub = EnumUtility.GetEnumSubValue(SomeEnum.Value1);
```

### SelectListUtility（ドロップダウン生成）

```csharp
using Dev.CommonLibrary.Common;

// Enum からドロップダウンリストを生成
var items = SelectListUtility.GetEnumSelectListItem<ApprovalStatus>();
// → SelectListItem のリスト（Value=Enum文字列, Text=表示名）

// 数値範囲のドロップダウンリスト
var numbers = SelectListUtility.GetNumberSelectList(1, 10);
```

### CookieUtility（Cookie 操作）

```csharp
using Dev.CommonLibrary.Common;

// Cookie 取得
string? value = CookieUtility.GetCookieValueByKey(HttpContext, "key");

// Cookie 設定
CookieUtility.SetCookie(HttpContext, "key", "value", expireDays: 7);

// Cookie 削除
CookieUtility.DeleteCookie(HttpContext, "key");
```

### ConstRegExpr（正規表現定数）

```csharp
using Dev.CommonLibrary.Common;

// 定義済みの正規表現パターンを使用
bool isEmail = Regex.IsMatch(input, ConstRegExpr.Email);
bool isPhone = Regex.IsMatch(input, ConstRegExpr.Phone);
bool isZip   = Regex.IsMatch(input, ConstRegExpr.ZipCode);
```

---

## 9. ページング・サマリー

### CommonListPagerModel（ページングパラメータ）

一覧画面の検索・ページング・ソート条件を保持するモデル。
リポジトリの `TCondModel` に組み込んで使用する。

```csharp
// 検索条件モデルに組み込む
public class SampleEntityCondModel : IRepositoryCondModel
{
    // IRepositoryCondModel の必須プロパティ
    public CommonListPagerModel Pager { get; set; } = new();

    // 業務固有の検索条件
    public string? Name { get; set; }
}
```

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `page` | `int` | 現在ページ番号（1始まり） |
| `sort` | `string?` | ソートするカラム名 |
| `sortdir` | `string?` | 昇順(`asc`) / 降順(`desc`) |
| `recoedNumber` | `int` | 1ページあたりの表示件数 |

### CommonListSummaryModel（表示サマリー）

ページングの「〇〜〇件目 / 全〇件」表示に使用する。

```csharp
// RepositoryBase.Select() の戻り値に含まれる
var result = repository.Select(cond);
// result.Summary → "1 〜 10 件目（全 95 件）"
```

---

## 10. ロガー（Logger）

### 概要

Singleton パターンで実装された共通ロガー。
アプリ起動時に ASP.NET Core の `ILogger` を注入しておくことで、任意の箇所から呼び出せる。

### 初期化（Program.cs）

```csharp
// Program.cs でロガーを設定
var logger = app.Services.GetRequiredService<ILogger<Program>>();
Dev.CommonLibrary.Common.Logger.SetLogger(logger);
```

### 使い方

```csharp
using Dev.CommonLibrary.Common;

// ログ出力
Logger.Debug("デバッグメッセージ");
Logger.Info("情報メッセージ");
Logger.Warn("警告メッセージ");
Logger.Error("エラーメッセージ", exception);
```

---

## 11. 新規プロジェクトへの導入方法

新しいプロジェクトに CommonLibrary を導入する手順：

### 1. プロジェクト参照の追加

```xml
<!-- .csproj に追加 -->
<ItemGroup>
  <ProjectReference Include="..\CommonLibrary\CommonLibrary.csproj" />
</ItemGroup>
```

### 2. 必要な DI 登録（Program.cs）

```csharp
// AccessLogAttribute を ServiceFilter で使う場合
builder.Services.AddScoped<AccessLogAttribute>();
```

### 3. エンティティ定義

```csharp
// EntityBase を継承
public class MyEntity : EntityBase
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
}
```

### 4. リポジトリ定義

```csharp
// RepositoryBase を継承
public class MyRepository : RepositoryBase<MyEntity, MyCondModel>
{
    public MyRepository(MyDbContext context, IMapper mapper)
        : base(context, mapper) { }
}
```

### 5. 名前空間の using

```csharp
using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Extensions;
using Dev.CommonLibrary.Repository;
```

---

## 12. CommonLibrary への追加ルール

### 基本原則

> **「このライブラリを別プロジェクトに持っていっても使える」ものだけを置く。**

### 追加してよいカテゴリ（ホワイトリスト）

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

### 追加してはいけないもの（ブラックリスト）

| NG の例 | 理由 |
|--------|------|
| アプリ固有の定数・Enum | プロジェクト依存 |
| Sample 固有のヘルパー | Sample は CommonLibrary を使う側 |
| 特定機能の業務ロジック | 呼び出し側プロジェクトに書く |

### 既存クラスへの追記ルール

> **既存クラスへの追記が許されるのは、そのクラスの責務名で説明できるときだけ。**
> 説明できなければ新クラスを作る。

例：`CookieUtility` に Cookie 削除メソッドを追加 → OK（Cookie 操作の責務内）  
例：`CookieUtility` に MD5 計算を追加 → NG（責務外 → `Util` クラスへ）

### グレーゾーンの判断ゲート（3問チェック）

ホワイトリストに当てはまらないものを追加しようとするとき：

```
① 他プロジェクトに持っていっても使えるか？
     NO → CommonLibrary には入れない。呼び出し側に書く。

② 単一の責務に収まるか？
     NO → CommonLibrary には入れない。責務を分解してから再検討。

③ 独立したクラスとして成立するか？
     NO → 既存クラスの責務名で説明できるか確認。できなければ新クラスを作る。
     YES → 新クラスとして追加する。
```

---

*本ドキュメントは DevNext プロジェクトの CommonLibrary 利用ガイドを記述したものです。*
