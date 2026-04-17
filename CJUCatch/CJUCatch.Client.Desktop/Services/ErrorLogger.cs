using System.IO;
using System.Text;

namespace CJUCatch.Client.Desktop.Services;

internal static class ErrorLogger
{
    private static readonly Lock SyncLock = new();

    public static void Log(string source, Exception exception)
    {
        try
        {
            var logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, $"error-{DateTime.Now:yyyy-MM-dd}.log");
            var builder = new StringBuilder()
                .AppendLine("==================================================")
                .AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}")
                .AppendLine($"Source: {source}")
                .AppendLine($"Type: {exception.GetType().FullName}")
                .AppendLine($"Message: {exception.Message}")
                .AppendLine("StackTrace:")
                .AppendLine(exception.ToString())
                .AppendLine();

            lock (SyncLock)
            {
                File.AppendAllText(logPath, builder.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // Do not throw while trying to log an error.
        }
    }
}
