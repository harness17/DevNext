using Dev.CommonLibrary.Common;
using System.Threading;

namespace Dev.CommonLibrary.Batch
{
    /// <summary>
    /// バッチ実行サービス
    /// </summary>
    public class BatchService
    {
        private readonly Logger _logger = Logger.GetLogger();

        public void Run(IBatch batch, string mutexName)
        {
            bool createdNew;
            using (var mutex = new Mutex(true, mutexName, out createdNew))
            {
                if (!createdNew)
                {
                    _logger.Info(new LogModel("既に起動しています。処理をスキップします。"));
                    return;
                }
                try
                {
                    batch.Exec();
                }
                catch (Exception ex)
                {
                    batch.ExceptionHandler(ex);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }
    }
}
