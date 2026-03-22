namespace Site.Common
{
    /// <summary>
    /// セッションキー
    /// </summary>
    public static class SessionKey
    {
        public static string DatabaseSampleCondViewModel = "DatabaseSampleCondViewModel";
        public static string Message = "Message";

        // ファイル管理サンプル
        public static string FileManagementCondViewModel = "FileManagementCondViewModel";

        // 多段階フォームサンプル（全ステップのデータを JSON で保持）
        public static string WizardSession = "WizardSession";
        // 多段階フォームサンプル一覧の検索条件
        public static string WizardSampleCondViewModel = "WizardSampleCondViewModel";


    }
}
