namespace Site.Common
{
    /// <summary>
    /// セッションキー
    /// </summary>
    public static class SessionKey
    {
        public static string DatabaseSampleCondViewModel = "DatabaseSampleCondViewModel";
        // ポイント: ページング・ソート状態を保存して一覧復帰時に再現するためのキー
        public static string DatabaseSamplePageModel = "DatabaseSamplePageModel";
        public static string Message = "Message";

        // ファイル管理サンプル
        public static string FileManagementCondViewModel = "FileManagementCondViewModel";

        // 多段階フォームサンプル（全ステップのデータを JSON で保持）
        public static string WizardSession = "WizardSession";
        // 多段階フォームサンプル一覧の検索条件
        public static string WizardSampleCondViewModel = "WizardSampleCondViewModel";

        // ユーザー・ロール管理
        public static string UserManagementCondViewModel = "UserManagementCondViewModel";
        public static string UserManagementPageModel = "UserManagementPageModel";

        // メール送信ログ
        public static string MailLogCondViewModel = "MailLogCondViewModel";
        public static string MailLogPageModel = "MailLogPageModel";

        // 承認ワークフロー
        public static string ApprovalRequestCondViewModel = "ApprovalRequestCondViewModel";
        public static string ApprovalRequestPageModel = "ApprovalRequestPageModel";

    }
}
