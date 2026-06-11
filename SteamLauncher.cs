using System;
using System.Diagnostics;

namespace UCOnline;

public sealed class SteamLauncher : IDisposable
{
    private readonly Config _config;
    private readonly ILogger _logger;
    private bool _isInitialized;

    public Config Config => _config;
    public bool IsInitialized => _isInitialized;

    public SteamLauncher(Config? config = null)
    {
        _config = config ?? Config.Load();
        _logger = new FileLogger(_config.Logging.Enabled, _config.Logging.FilePath);
    }

    public bool Initialize()
    {
        if (_isInitialized) return true;

        try
        {
            if (_config.AppId > 0)
            {
                SetAppIDEnv(_config.AppId, _config.OgAppId);
                WriteAppIDFile(_config.AppId);
                _logger.LogInformation($"Initializing Steam with AppID: {_config.AppId}");
            }
            else
            {
                _logger.LogWarning("No AppID configured. Defaulting to 480.");
            }

            if (!SteamApi.Initialize(_config.AppId, out var error))
            {
                _logger.LogError($"Steam initialization failed: {error}");
                return false;
            }

            _isInitialized = true;
            _logger.LogInformation("Steam initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Steam initialization error: {ex.Message}");
            return false;
        }
    }

    public Process? LaunchGame()
    {
        if (string.IsNullOrWhiteSpace(_config.GameExecutable) ||
            !File.Exists(_config.GameExecutable))
        {
            _logger.LogError($"Game executable not found: {_config.GameExecutable}");
            return null;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _config.GameExecutable,
            Arguments = _config.GameArguments ?? "",
            WorkingDirectory = Path.GetDirectoryName(_config.GameExecutable),
            UseShellExecute = false
        };

        try
        {
            var process = Process.Start(startInfo);
            _logger.LogInformation($"Game launched (PID: {process?.Id})");
            return process;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to launch game: {ex.Message}");
            return null;
        }
    }

    public void SetGameExecutable(string path)
    {
        _config.GameExecutable = path;
        _config.Save();
    }

    public void SetGameArguments(string arguments)
    {
        _config.GameArguments = arguments;
        _config.Save();
    }

    private static ulong ToCGameID(uint appId)
    {
        return (ulong)appId;
    }

    private static void SetAppIDEnv(uint appId, uint? ogAppId)
    {
        // SteamAppId - the spoofed AppId as string
        Environment.SetEnvironmentVariable("SteamAppId", appId.ToString());

        // SteamOverlayGameId - original game ID for overlay support
        var overlayAppId = ogAppId ?? appId;
        var overlayGameId = ToCGameID(overlayAppId);
        Environment.SetEnvironmentVariable("SteamOverlayGameId", overlayGameId.ToString());
    }

    private static void WriteAppIDFile(uint appId)
    {
        try
        {
            File.WriteAllText("steam_appid.txt", appId.ToString());
        }
        catch
        {
            // Best effort, ignore failures
        }
    }

    public void Dispose()
    {
        if (_isInitialized)
        {
            SteamApi.Shutdown();
            _logger.LogInformation("Steam shutdown complete");
        }
    }
}