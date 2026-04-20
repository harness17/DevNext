# DI 登録ルール

- `[ServiceFilter(typeof(XxxAttribute))]` を使う場合、対象クラスを `Program.cs` で `AddScoped` 登録する
  ```csharp
  builder.Services.AddScoped<AccessLogAttribute>();
  ```

## Service 内でリポジトリ・サービスを `new` しない

Service のコンストラクターで Repository や別の Service を `new` してはならない。
**必ず DI コンテナから注入を受けること。**

```csharp
// NG: DI をバイパスした手動 new
public class NotificationService(DBContext context)
{
    private readonly NotificationRepository _repo = new NotificationRepository(context); // NG
}

// OK: DI 注入
public class NotificationService(NotificationRepository repo)
{
    private readonly NotificationRepository _repo = repo; // OK
}
```

DI 注入を使う場合、注入される型も `Program.cs` で `AddScoped` 登録が必要：
```csharp
builder.Services.AddScoped<Site.Repository.NotificationRepository>();
builder.Services.AddScoped<Site.Service.NotificationService>();
```

# パスワードポリシー

`Program.cs` で設定。以下をすべて満たすこと。

- 最低 6 文字
- 大文字・小文字・数字・記号すべて必須

# ロックアウトポリシー（ブルートフォース対策）

`Program.cs` の `AddIdentity` 内で設定。

- ロックアウト時間: 5分（`DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5)`）
- 最大失敗回数: 5回（`MaxFailedAccessAttempts = 5`）
