namespace DatabaseSample.Common
{
    /// <summary>
    /// TempData / Session に使用するキー定数
    /// </summary>
    public static class SessionKey
    {
        public static string DatabaseSampleCondViewModel = "DatabaseSampleCondViewModel";
        // ポイント: ページング・ソート状態を保存して一覧復帰時に再現するためのキー
        public static string DatabaseSamplePageModel = "DatabaseSamplePageModel";
        public static string Message = "Message";
    }
}
