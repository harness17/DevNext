namespace FileSample.Common
{
    // FileSample で使用する列挙型

    /// <summary>
    /// ページ読み込み種別（ページング・ソート・検索・件数変更）
    /// </summary>
    public enum PageRead
    {
        Resarch,
        Paging,
        Sorting,
        ChangeRecordNum
    }
}
