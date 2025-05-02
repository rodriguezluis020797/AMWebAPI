using System.Runtime.CompilerServices;
using NLog;

namespace AMTools.Tools;

public interface IAMLogger
{
    void LogInfo(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string path = null,
        [CallerMemberName] string caller = null);

    void LogAudit(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string path = null,
        [CallerMemberName] string caller = null);

    void LogError(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string path = null,
        [CallerMemberName] string caller = null);
}

public class AMDevLogger : IAMLogger
{
    private static Logger rollingLog;
    private static Logger auditLog;


    public AMDevLogger()
    {
        rollingLog = LogManager.GetLogger("info");
        auditLog = LogManager.GetLogger("audit");
    }

    public void LogInfo(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string path = null,
        [CallerMemberName] string caller = null)
    {
        rollingLog.Info(
            $"{DateTime.UtcNow.ToString("O")} | Info | TID: {Thread.CurrentThread.ManagedThreadId} | {Path.GetFileNameWithoutExtension(path)} | {caller} () | Line: {lineNumber} | {message}");
    }

    public void LogAudit(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string path = null,
        [CallerMemberName] string caller = null)
    {
        var utcTime = DateTime.UtcNow.ToString("O");
        auditLog.Info(
            $"{utcTime} | Audit | TID: {Thread.CurrentThread.ManagedThreadId} | {Path.GetFileNameWithoutExtension(path)} | {caller} () | Line: {lineNumber}{Environment.NewLine}{message}");
    }

    public void LogError(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string path = null,
        [CallerMemberName] string caller = null)
    {
        rollingLog.Info(
            $"{DateTime.UtcNow.ToString("O")} | Error | TID: {Thread.CurrentThread.ManagedThreadId} | {Path.GetFileNameWithoutExtension(path)} | {caller} () | Line: {lineNumber} | {message}");
    }
}