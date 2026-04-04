---
name: add-entity
description: 新しいエンティティ（DBテーブル）を追加する手順。エンティティクラス作成後に DbMigrationRunner を更新するときに使用する。
---

新しいエンティティを追加した後、`DbMigrationRunner/Program.cs` の `ApplyMissingTablesAsync` メソッドを更新します。

## テーブル追加

`CREATE TABLE` ブロックを追記する。

```csharp
// ─── XxxEntity ───────────────────────────────────────────────
Console.WriteLine("  テーブル [XxxEntity] を確認しています...");
await context.Database.ExecuteSqlRawAsync(@"
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'XxxEntity')
    BEGIN
        CREATE TABLE [XxxEntity] (
            -- エンティティのカラム定義
            CONSTRAINT [PK_XxxEntity] PRIMARY KEY ([Id])
        )
    END");
```

## カラム追加・変更・削除

`ALTER TABLE` ブロックを追記する。

```csharp
// ─── XxxEntity: [NewColumn] カラム追加 ───────────────────────
Console.WriteLine("  テーブル [XxxEntity] にカラム [NewColumn] を確認しています...");
await context.Database.ExecuteSqlRawAsync(@"
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('XxxEntity') AND name = 'NewColumn')
    BEGIN
        ALTER TABLE [XxxEntity] ADD [NewColumn] nvarchar(100) NOT NULL DEFAULT ''
    END");
```

## カラム型の対応表

| C# 型 | SQL 型 |
|---|---|
| `string`（`[Required][MaxLength(N)]`） | `nvarchar(N) NOT NULL` |
| `string?`（`[MaxLength(N)]`） | `nvarchar(N) NULL` |
| `string?`（MaxLength 指定なし） | `nvarchar(max) NULL` |
| `long` | `bigint NOT NULL` |
| `bool` | `bit NOT NULL` |
| `DateTime` | `datetime2(7) NOT NULL` |
| `DateTime?` | `datetime2(7) NULL` |
| `enum`（int） | `int NOT NULL` |

## DbMigrationRunner の実行

スキーマ変更後は必ず DbMigrationRunner を再実行する。

```bash
cd DbMigrationRunner && dotnet run
```
