# DevNext 活用ルール（新規開発の出発点）

DevNext は「新案件のコアテンプレート + パターン別サンプル集」。新機能を実装するとき、**ゼロから書かずに既存サンプル・既存コアを模倣する**こと。

## 実装前の必須手順

1. **実現したい機能を下のマッピング表で特定する**
2. **該当サンプル / コアコードを読む**（Controller → Service → Repository → View の順）
3. **スプリントコントラクトの中で「どのサンプルをベースにするか」を宣言する**
4. ベースが決まったら、そのサンプルの構成（フォルダ・命名・DI 登録パターン）を踏襲して実装する

ベースを選ばずに書き始めるのは禁止。「一から書いた方が早い」と感じたら立ち止まり、該当サンプルが本当に無いかを確認する。

## 機能 → ベースにするサンプル マッピング

| 実現したい機能 | ベース | 参照先 |
|--------------|--------|--------|
| 一覧 + 詳細 + 登録 + 編集 + 削除（CRUD） | `Samples/DatabaseSample/` | `DatabaseSampleController.cs` 全体 |
| ページャー / ソート / 検索つき一覧 | `Samples/DatabaseSample/` + コアの `ApprovalRequestController` | `CommonListPagerModel` の使い方 |
| 認証・ログイン・パスワード変更 | DevNext 本体 | `AccountController.cs` / `ManageController.cs` |
| ユーザー管理・ロール管理 | DevNext 本体 | `UserManagementController.cs` |
| 所有者チェック（IDOR 対策つき編集・削除） | DevNext 本体 | `ApprovalRequestController.cs` |
| 通知・バッジ表示 | DevNext 本体 | `NotificationController.cs` |
| カレンダー表示・スケジュール | DevNext 本体 | `ScheduleController.cs` |
| マルチステップ登録フォーム（ウィザード） | `Samples/WizardSample/` | `WizardSampleController.cs` |
| ファイルアップロード・ダウンロード | `Samples/FileSample/` | `FileSampleController.cs` |
| Excel 入出力 | `Samples/ExcelSample/` | `ExcelSampleController.cs` |
| PDF 帳票出力 | `Samples/PdfSample/` | `PdfSampleController.cs` + `PlaywrightPdfService` |
| メール送信（smtp4dev） | `Samples/MailSample/` | `MailSampleController.cs` |
| REST API エンドポイント公開 | `Samples/ApiSample/` | Swashbuckle + `Program.cs` 設定 |
| 外部 API 呼び出し（HttpClient） | `Samples/ApiClientSample/` | `IHttpClientFactory` の使い方 |
| バッチ処理・定期実行 | `BatchSample/` | `IBatch` 実装パターン |

## エンティティ・DB 設計のベース

- エンティティ基底: 必ず **`SiteEntityBase` を継承**（`rules/coding-policy.md`）
- 履歴テーブルが必要: `Samples/DatabaseSample/Entity/SampleEntity.cs` の `Base` + `History` パターンを踏襲
- 新エンティティ追加後は `/add-entity` スキルで DI・マイグレーション登録まで実施
- Sample 用エンティティは **Sample 専用の DBContext** に入れる（コア DBContext を汚さない）

## CommonLibrary を再発明しない

実装前に `CommonLibrary/` の該当カテゴリを探す。既にあるものを独自実装すると責務境界が崩れる。

| 探すもの | CommonLibrary 内の場所 |
|---------|----------------------|
| ページング | `Common/CommonListPagerModel.cs` |
| 拡張メソッド | `Extensions/` 配下 |
| HTML ヘルパー | `Extensions/HtmlExtensions*.cs` |
| ロギング | `Common/Logger.cs` |
| PDF 生成 | `Common/PlaywrightPdfService.cs` |
| 属性（AccessLog 等） | `Attributes/` |

詳細な責務境界は `rules/commonlibrary-boundary.md` を参照。

## ベースが見つからない場合

該当サンプルが無い新パターンを実装するときは：

1. **先に advisor に相談**する（`rules/advisor-strategy.md`）
2. **コア本体には入れず、新 Sample として `Samples/` に追加**する（`rules/commonlibrary-boundary.md` のコア方針）
3. Sample 追加手順は `/new-sample` スキルを使用する

## ベースから逸脱してよいケース

既存サンプルと揃えない方が良い場合は、スプリントコントラクトに理由を明記する：

- 「DatabaseSample をベースにするが、○○は業務要件により変更する」
- 「ApiSample ベースだが、認証方式のみ変更する」

理由を書かずに逸脱するのは禁止。
