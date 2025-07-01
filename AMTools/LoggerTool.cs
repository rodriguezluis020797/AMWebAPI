using System.Runtime.CompilerServices;
using NLog;

namespace AMTools;

public interface IAMLogger
{
    void LogInfo(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? path = null,
        [CallerMemberName] string? caller = null);

    void LogAudit(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? path = null,
        [CallerMemberName] string? caller = null);

    void LogError(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? path = null,
        [CallerMemberName] string? caller = null);
}

public class AMDevLogger : IAMLogger
{
    private static Logger? rollingLog;
    private static Logger? auditLog;


    public AMDevLogger()
    {
        rollingLog = LogManager.GetLogger("info");
        auditLog = LogManager.GetLogger("audit");
    }

    public void LogInfo(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? path = null,
        [CallerMemberName] string? caller = null)
    {
        rollingLog?.Info(
            $"{DateTime.UtcNow:O} | Info | TID: {Environment.CurrentManagedThreadId} | {Path.GetFileNameWithoutExtension(path)} | {caller} () | Line: {lineNumber} | {message}");
    }

    public void LogAudit(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? path = null,
        [CallerMemberName] string? caller = null)
    {
        auditLog?.Info(
            $"{DateTime.UtcNow:O} | Audit | TID: {Environment.CurrentManagedThreadId} | {Path.GetFileNameWithoutExtension(path)} | {caller} () | Line: {lineNumber} | {message}");
        rollingLog?.Info(
            $"{DateTime.UtcNow:O} | Audit | TID: {Thread.CurrentThread.ManagedThreadId} | {Path.GetFileNameWithoutExtension(path)} | {caller} () | Line: {lineNumber} | {message}");
    }

    public void LogError(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? path = null,
        [CallerMemberName] string? caller = null)
    {
        rollingLog?.Info(
            $"{DateTime.UtcNow:O} | Error | TID: {Environment.CurrentManagedThreadId} | {Path.GetFileNameWithoutExtension(path)} | {caller} () | Line: {lineNumber} | {message}");
    }
}