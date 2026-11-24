# スプリントコントラクト（DevNext 固有追加）

基本ルール・フォーマット・観点ガイドはグローバル `~/.claude/rules/sprint-contract.md` を参照。ここは DevNext 固有の追加事項のみ。

## 完成条件に必ず含めること

- **ベースサンプルの宣言**（`rules/devnext-template-usage.md` のマッピング表から選ぶ）
  - 例: 「`Samples/DatabaseSample/` をベースに実装する。逸脱点: 〇〇」
- **テスト追加**（`rules/testing.md`）
  - 例: 「Tests/ に対応するテストを追加し `dotnet test` が全件パスする」
- **認可観点**（DevNext は認証つきアプリ）
  - ロールが関わる機能なら「Admin のみ〇〇できる」「非所有者は〇〇できない（IDOR対策）」を明示
