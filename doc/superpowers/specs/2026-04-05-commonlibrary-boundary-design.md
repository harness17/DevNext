# CommonLibrary 責務境界定義

**作成日:** 2026-04-05  
**ステータス:** active

---

## 目的

CommonLibrary は「別プロジェクトにコピーしてもビルドが通り、意味のある機能を提供できる」汎用ライブラリとして維持する。  
プロジェクト固有のコードを混入させず、肥大化を防ぐためのルールを定義する。

---

## スコープ定義

### 原則

> **「このライブラリを別プロジェクトに持っていっても使える」ものだけを置く。**

### ホワイトリスト（入れていいカテゴリ）

| カテゴリ | 具体例 | 判断基準 |
|---------|-------|---------|
| Entity 基底 | `EntityBase`, `SiteEntityBase`, `IEntity` | 監査カラム・論理削除パターンの共通化 |
| Identity エンティティ | `ApplicationUser`, `ApplicationRole`, `UserPreviousPassword` | 認証基盤として再利用する共通エンティティ |
| Repository 基底 | `RepositoryBase`, `IRepository` | CRUD + 履歴パターンの共通化 |
| ページング | `CommonListPagerModel`, `CommonListSummaryModel` | リスト系 UI の共通パターン |
| ロギング | `Logger`, `LogModel`, `ILogModel` | ロガーラッパー |
| 属性 | `AccessLogAttribute`, `SubValueAttribute`, `FileAttribute` | MVC / 検証の共通属性 |
| 拡張メソッド | `StringExtensions`, `EnumExtensions` など | 型に紐づく純粋な変換・操作 |
| バッチ基底 | `IBatch`, `BatchService` | バッチ処理の共通インターフェース |

### ブラックリスト（入れてはいけないもの）

| NG の例 | 理由 |
|--------|------|
| アプリ固有の定数・Enum | プロジェクト依存 |
| Sample 固有のヘルパー | Sample は CommonLibrary を使う側 |
| 特定機能の業務ロジック | 呼び出し側プロジェクトに書く |

---

## 既存クラスへの追記ルール

> **既存クラスへの追記が許されるのは、そのクラスの責務名で説明できるときだけ。**  
> 説明できなければ新クラスを作る。

**例:**
- `CookieUtility` に Cookie 削除メソッドを追加 → **OK**（Cookie 操作の責務内）
- `CookieUtility` に MD5 計算メソッドを追加 → **NG**（責務外 → `Util` クラスへ）

### 現状の `util.cs` について

`util.cs` には以下の4クラスが同居しており、責務が混在している。  
新規追加時は必ず適切なクラスに振り分けること。新しいカテゴリは独立したファイルに作成する。

```
util.cs
  ├── EnumUtility        → Enum の表示名・順序を取得
  ├── SelectListUtility  → Enum から SelectListItem を生成
  ├── CookieUtility      → Cookie の読み書き・削除
  └── Util               → MD5・ファイル名・パス検証・ページングサマリー
```

---

## グレーゾーンの判断ゲート（3問チェック）

ホワイトリストに当てはまらないものを追加しようとするとき、以下の順番で判断する。

```
① 他プロジェクトに持っていっても使えるか？
     NO → CommonLibrary には入れない。呼び出し側に書く。

② 単一の責務に収まるか？
     NO → CommonLibrary には入れない。責務を分解してから再検討。

③ 独立したクラスとして成立するか？（既存クラスへの追記ではないか）
     NO → 既存クラスの責務名で説明できるか確認。できなければ新クラスを作る。
     YES → 新クラスとして追加する。
```

### 判断例

| 追加候補 | ① | ② | ③ | 結論 |
|---------|---|---|---|------|
| メール送信ヘルパー | ✅ | ✅ | ✅ | CommonLibrary に新クラスで追加 |
| 承認フロー固有の定数 | ❌ | - | - | 呼び出し側プロジェクトに書く |
| `CookieUtility` に削除メソッド追加 | ✅ | ✅ | ❌（追記） | 既存クラスの責務内なので追記 OK |
| Sample 固有の CSV パーサー | ❌ | - | - | Sample プロジェクト内に書く |

---

## 関連ドキュメント

- `CLAUDE.md` — プロジェクト全体の設計方針
- `.claude/rules/coding-policy.md` — コーディング規約
