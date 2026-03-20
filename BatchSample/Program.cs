using Dev.CommonLibrary.Batch;
using Dev.CommonLibrary.Common;
using Site.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BatchSample
{
    /// <summary>
    /// バッチ処理記載方法例
    /// </summary>
    internal class Program
    {
        public static DBContext _context = null!;
        public static Logger _logger = null!;
        public static string mutexName = string.Empty;

        /// <summary>
        /// 処理実行
        /// </summary>
        private static void Main(string[] args)
        {
            // 設定ファイル読み込み
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // ロガー設定
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            var microsoftLogger = loggerFactory.CreateLogger("BatchSample");
            Logger.GetLogger().SetLogger(microsoftLogger);
            _logger = Logger.GetLogger();

            // DBContext 初期化
            var connectionString = config.GetConnectionString("SiteConnection");
            var dbContextOptions = new DbContextOptionsBuilder<DBContext>()
                .UseSqlServer(connectionString)
                .Options;
            _context = new DBContext(dbContextOptions);

            // 二重起動防止用Mutex名
            mutexName = config["AppSettings:AppName"] ?? "BatchSample";

            var service = new BatchService();
            var batch = new Batch();
            service.Run(batch, mutexName);
        }

        /// <summary>
        /// バッチ処理用クラス
        /// </summary>
        public class Batch : IBatch
        {
            /// <summary>
            /// バッチ処理
            /// </summary>
            public void Exec()
            {
                // 実際にバッチ処理にて実行したい処理を記載する
                // 以下サンプル処理

                // 開始ログ出力
                _logger.Info(new LogModel(mutexName + " 実行開始"));

                // 処理としてサンプルテーブルの項目数を表示
                Console.Write("SampleEntity:" + _context.SampleEntity.Count());
                Console.ReadKey();
            }

            /// <summary>
            /// 例外発生時処理
            /// </summary>
            public void ExceptionHandler(Exception ex)
            {
                // バッチ処理(Exec)にて例外が発生した場合に行う処理を記載
                var innerExceptionString = ex.InnerException?.ToString() ?? string.Empty;

                // ログ出力
                _logger.Error(new LogModel(ex.ToString()));
                _logger.Error(new LogModel("Inner: " + innerExceptionString));
            }
        }
    }
}
