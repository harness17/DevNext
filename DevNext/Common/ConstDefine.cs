namespace Site.Common
{
    /// <summary>
    /// 定数定義
    /// </summary>
    public static class Const
    {
        public const string baseFilePath = "~/Uploads/";

        // ポイント: DbMigrationRunner で Id="1" に固定した初期 Admin ユーザーは削除禁止とする
        public const string SystemAdminUserId = "1";
        public const string sampleFilePath = "~/Uploads/Sample/{0}/";
        public const string ExcelTempFilePath = "~/Templates/Excel/";
        public const string PDFTempFilePath = "~/Templates/PDF/";
    }
}
