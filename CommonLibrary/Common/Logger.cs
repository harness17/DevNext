using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Dev.CommonLibrary.Common
{
    /// <summary>
    /// ログ出力クラス
    /// </summary>
    public class Logger
    {
        private static Logger _instance = new Logger();
        private ILogger? _logger;

        private Logger() { }

        public static Logger GetLogger() => _instance;

        public void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void Debug(ILogModel model)
        {
            if (model.Message != null)
                _logger?.LogDebug(model.Message);
        }

        public void Info(ILogModel model)
        {
            if (model.Message != null)
                _logger?.LogInformation(model.Message);
        }

        public void Warn(ILogModel model)
        {
            if (model.Message != null)
                _logger?.LogWarning(model.Message);
        }

        public void Warn(ILogModel model, Exception ex)
        {
            if (model.Message != null)
                _logger?.LogWarning(ex, model.Message);
        }

        public void Error(ILogModel model)
        {
            if (model.Message != null)
                _logger?.LogError(model.Message);
        }

        public void Error(ILogModel model, Exception ex)
        {
            _logger?.LogError(ex, model.Message ?? "");
        }
    }

    /// <summary>
    /// ログ出力用インターフェース
    /// </summary>
    public interface ILogModel
    {
        string? Message { get; }
    }

    /// <summary>
    /// ログ出力用クラス
    /// </summary>
    public class LogModel : ILogModel
    {
        protected const string logFormat = "{0}\t{1}\t{2}";
        protected string FileName { get; private set; }
        protected string Method { get; private set; }
        protected string? Msg { get; set; }

        public LogModel(string msg, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string method = "")
            : this(sourceFilePath, method)
        {
            this.Msg = msg;
        }

        protected LogModel([CallerFilePath] string sourceFilePath = "", [CallerMemberName] string method = "")
        {
            this.FileName = Path.GetFileName(sourceFilePath);
            this.Method = method;
        }

        public virtual string? Message => string.Format(logFormat, FileName, Method, Msg);
    }
}
