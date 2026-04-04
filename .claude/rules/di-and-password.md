# DI 登録ルール

- `[ServiceFilter(typeof(XxxAttribute))]` を使う場合、対象クラスを `Program.cs` で `AddScoped` 登録する
  ```csharp
  builder.Services.AddScoped<AccessLogAttribute>();
  ```

# パスワードポリシー

`Program.cs` で設定。以下をすべて満たすこと。

- 最低 6 文字
- 大文字・小文字・数字・記号すべて必須
