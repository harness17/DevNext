# Project Collaboration Profile（DevNext）

`cross-agent-harness.md` を DevNext に適用するためのプロジェクト固有設定。

## プロジェクト

- 名前: DevNext
- 種別: ASP.NET Core 10 MVC Webアプリ + CommonLibrary + Samples + xUnit tests
- 主な検証対象: `DevNext.slnx`、`DevNext/`、`CommonLibrary/`、`Samples/`、`Tests/`
- 注意領域: 認可、Identity、所有者チェック、監査カラム、SQL Server migrations、appsettings.Development.json、Sample 間依存

## 担当境界

| 条件 | 振り先 |
|------|--------|
| 仕様が明確で、触るファイルが限定される実装・テスト修正 | Codex |
| 複数プロジェクト、永続化、Identity、認可、CommonLibrary 境界をまたぐ設計判断 | Claude Code |
| Razor / JavaScript / ブラウザ実動確認が必要な変更 | 実装者がセルフ確認し、反対側がレビュー |
| DB schema、migration、公開・配布・テンプレート方針の判断 | Claude Code + user |
| merge / publish / release 判断 | user |

## Verify コマンド

通常のセルフ verify:

```powershell
dotnet build DevNext.slnx
dotnet test .\Tests\Tests.csproj
```

特定 Sample の変更時:

```powershell
dotnet build .\Samples\<SampleName>\<SampleName>.csproj
```

実動確認が必要な場合:

```powershell
dotnet run --project .\DevNext\DevNext.csproj
```

ブラウザで `https://localhost:<port>` または `http://localhost:<port>` を開き、対象画面の正常系・エラー系を確認する。

## レビュー観点

### 動作

- 完成条件を満たしているか
- 既存の認証、ユーザー管理、基本CRUDを壊していないか
- エラー処理・境界値・重複操作の扱いに穴がないか

### 契約

- Controller / Service / ViewModel / Razor View の契約が一致しているか
- CommonLibrary に寄せるべきものと DevNext 固有に残すものの境界が守られているか
- Sample 同士が依存していないか
- `DevNext.slnx` とプロジェクト参照が実体と一致しているか

### テスト

- 完成条件に対応するテストが追加・更新されているか
- 通常 verify コマンドが pass するか
- バグ修正では再発条件を確認できるテストまたは明示的な手動確認があるか

### セキュリティ・運用

- 認証と所有者チェックを混同していないか
- Admin / Member など role 前提の認可が抜けていないか
- 外部入力、ファイルアップロード、ユーザー制御パスを server-side で検証しているか
- secret、connection string、個人情報、`appsettings.Development.json` をコミット対象にしていないか
- raw exception、内部パス、DB schema、stack trace をユーザーへ露出していないか

### スタイル

- 既存の C# / Razor / JavaScript / docs のスタイルに揃っているか
- unrelated cleanup や依頼外ファイル変更が混ざっていないか
- `git add -A` / `git add .` を使わず、変更ファイルを個別指定しているか

## プロジェクト固有の重大指摘

以下は原則として merge / publish ブロッカーにする。

- 認可、所有者チェック、Identity role 周りの欠落
- `appsettings.Development.json`、secret、connection string、個人情報の混入
- migration / DB schema 変更に対する設計説明や verify 不足
- Sample 間依存の追加
- CommonLibrary へ DevNext 固有責務を混入させる変更
- `DevNext.slnx` が build できない状態
- テスト失敗を既知リスクとして残したままの merge
