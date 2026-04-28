# テストルール

## 実行コマンド

```bash
cd Tests && dotnet test
```

## フレームワーク

- xUnit + Moq

## テスト追加の義務（重要）

**機能追加・サンプル追加を行った際は、必ず対応するテストを同じコミットに含めること。**

### テストが必要なトリガー

| 変更内容 | テスト対象 |
|---------|-----------|
| CommonLibrary に新しいクラス・メソッドを追加した | `Tests/` 配下に対応するテストクラスを作成 |
| Service / Helper クラスを追加・変更した | 対象クラスの公開メソッドをテスト |
| 新しい Sample プロジェクトを追加した | Sample 固有のロジック（Service・Helper）をテスト |
| バグ修正を行った | バグを再現するテストを追加（リグレッション防止） |

### テストクラスの配置ルール

```
Tests/
  Common/        ← Dev.CommonLibrary.Common のテスト
  Extensions/    ← Dev.CommonLibrary.Extensions のテスト
  Schedule/      ← Site.Service 系のテスト（例: ScheduleRecurrenceHelper）
  Samples/       ← 各 Sample 固有ロジックのテスト（必要に応じて作成）
```

### テストケース設計の最低ライン

新規メソッドには以下を必ずカバーすること：

- **正常系**: 期待通りの入力で期待通りの結果を返すか
- **境界値**: null・空・最大・最小など
- **異常系**: 無効な引数・例外発生ケース（`Assert.Throws`）

### 完成条件への組み込み

スプリントコントラクトを宣言する際は「テストを追加した」を完成条件の1つとして必ず含めること。

```
完成条件：
- 〇〇が動作する（正常系）
- 〇〇エラーが返る（異常系）
- **Tests/ に対応するテストを追加し dotnet test が全件パスする**
```

## 注意事項

`SignInResult` は `Microsoft.AspNetCore.Identity.SignInResult` と `Microsoft.AspNetCore.Mvc.SignInResult` が競合するため、エイリアスを使用すること。

```csharp
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;
```
