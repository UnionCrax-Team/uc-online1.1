namespace UCOnline;

public sealed class Config
{
    public uint AppId { get; set; } = 480;
    public uint? OgAppId { get; set; }
    public string? GameExecutable { get; set; }
    public string? GameArguments { get; set; }

    public LoggerSettings Logging { get; set; } = new();

    public static Config Load(string filePath = "union-crax.ini")
    {
        var config = new Config();

        if (!File.Exists(filePath))
        {
            config.CreateDefault(filePath);
            return config;
        }

        string? currentSection = null;

        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmed = line.Trim();

            if (string.IsNullOrEmpty(trimmed) || trimmed[0] is ';' or '#')
                continue;

            if (trimmed[0] == '[' && trimmed[^1] == ']')
            {
                currentSection = trimmed[1..^1];
                continue;
            }

            if (currentSection != null && trimmed.Contains('='))
            {
                var parts = trimmed.Split('=', 2);
                var key = parts[0].Trim().ToLowerInvariant();
                var value = parts.Length > 1 ? parts[1].Trim() : "";

                switch (currentSection)
                {
                    case "uc-online":
                    case "Config":
                        ParseConfigValue(config, key, value);
                        break;
                    case "Logging":
                        ParseLoggingValue(config.Logging, key, value);
                        break;
                }
            }
        }

        return config;
    }

    private static void ParseConfigValue(Config config, string key, string value)
    {
        switch (key)
        {
            case "appid" when uint.TryParse(value, out var id):
                config.AppId = id;
                break;
            case "ogappid" or "ogappid" when uint.TryParse(value, out var ogid) && ogid > 0:
                config.OgAppId = ogid;
                break;
            case "gameexecutable":
                config.GameExecutable = value;
                break;
            case "gamearguments":
                config.GameArguments = value;
                break;
        }
    }

    private static void ParseLoggingValue(LoggerSettings settings, string key, string value)
    {
        switch (key)
        {
            case "enablelogging" when bool.TryParse(value, out var enabled):
                settings.Enabled = enabled;
                break;
            case "logfile":
                settings.FilePath = value;
                break;
        }
    }

    public void Save(string filePath = "union-crax.ini")
    {
        var lines = new List<string>
        {
            "[Config]",
            "; Steam App ID (default: 480 - Spacewar)",
            $"AppId={AppId}",
            "; Original Game AppID for overlay support (optional - doesn't always work)",
            $"OgAppId={OgAppId?.ToString() ?? ""}",
            "; Full path to game executable",
            "GameExecutable=",
            "; Launch arguments for the game",
            "GameArguments=",
            "",
            "[Logging]",
            "EnableLogging=true",
            "LogFile=uc-online.log",
            ""
        };

        File.WriteAllLines(filePath, lines);
    }

    private void CreateDefault(string filePath)
    {
        File.WriteAllLines(filePath, new[]
        {
            "[Config]",
            "; Steam App ID (default: 480 - Spacewar)",
            "AppId=480",
            "; Original Game AppID for overlay support (optional)",
            "OgAppId=",
            "; Full path to game executable",
            "GameExecutable=",
            "; Launch arguments for the game",
            "GameArguments=",
            "",
            "[Logging]",
            "EnableLogging=true",
            "LogFile=uc-online.log",
            ""
        });
    }
}

public sealed class LoggerSettings
{
    public bool Enabled { get; set; } = true;
    public string FilePath { get; set; } = "uc-online.log";
}