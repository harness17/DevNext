# テストルール

## 実行コマンド

```bash
cd Tests && dotnet test
```

## フレームワーク

- xUnit + Moq

## 注意事項

`SignInResult` は `Microsoft.AspNetCore.Identity.SignInResult` と `Microsoft.AspNetCore.Mvc.SignInResult` が競合するため、エイリアスを使用すること。

```csharp
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;
```
