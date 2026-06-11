namespace UCOnline;

internal interface ILogger
{
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogException(Exception ex, string context);
    void Clear();
}

internal sealed class FileLogger : ILogger
{
    private readonly bool _enabled;
    private readonly string _filePath;

    public FileLogger(bool enabled, string filePath)
    {
        _enabled = enabled;
        _filePath = filePath;
        
        if (_enabled)
        {
            LogInformation("Logger initialized");
        }
    }

    public void LogInformation(string message) => WriteLog("INFO", message);
    public void LogWarning(string message) => WriteLog("WARNING", message);
    public void LogError(string message) => WriteLog("ERROR", message);
    
    public void LogException(Exception ex, string context)
    {
        if (!_enabled) return;
        WriteLog("EXCEPTION", $"{context}: {ex.GetType().Name} - {ex.Message}");
    }

    public void Clear()
    {
        if (!_enabled) return;
        File.WriteAllText(_filePath, $"uc-online Log - {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
    }

    private void WriteLog(string level, string message)
    {
        if (!_enabled) return;
        
        try
        {
            File.AppendAllText(_filePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}\n");
        }
        catch { /* Best effort logging */ }
    }
}