namespace SsiSdk.Internal;

internal enum LogLevel
{
    Debug = 0,
    Info = 1,
    Error = 2,
}

internal sealed class Logger
{
    private static volatile LogLevel s_globalLevel = LogLevel.Info;

    private readonly string _component;

    public Logger(string component) => _component = component;

    public static void SetLevel(LogLevel level) => s_globalLevel = level;

    public static void SetLevelFromString(string level)
    {
        s_globalLevel = level.ToUpperInvariant() switch
        {
            "DEBUG" => LogLevel.Debug,
            "ERROR" => LogLevel.Error,
            _ => LogLevel.Info,
        };
    }

    public void Debug(string message) => Log(LogLevel.Debug, "DEBUG", message);
    public void Info(string message) => Log(LogLevel.Info, "INFO ", message);
    public void Error(string message) => Log(LogLevel.Error, "ERROR", message);

    private void Log(LogLevel level, string tag, string message)
    {
        if (level < s_globalLevel) return;
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Console.Error.WriteLine($"{now} {tag} [{_component}]: {message}");
    }
}
