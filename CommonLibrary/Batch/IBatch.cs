namespace Dev.CommonLibrary.Batch
{
    /// <summary>
    /// バッチインターフェース
    /// </summary>
    public interface IBatch
    {
        void Exec();
        void ExceptionHandler(Exception ex);
    }
}
