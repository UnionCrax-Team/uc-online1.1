using System.Runtime.InteropServices;

namespace UCOnline;

internal static class SteamApi
{
    private static IntPtr s_library;
    private static bool s_isInitialized;
    private static int s_hSteamPipe;
    private static int s_hSteamUser;

    #region Native delegates

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NativeCreateSteamPipe();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NativeConnectToGlobalUser(int hSteamPipe);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NativeReleaseUser(int hSteamPipe, int hSteamUser);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool NativeBReleaseSteamPipe(int hSteamPipe);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NativeBreakpadSetAppId(uint appId);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NativeBreakpadSetSteamId(ulong steamId);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NativeReleaseThreadLocalMemory();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr NativeCreateInterface(string version);

    #endregion

    #region Loaded function pointers

    private static NativeCreateSteamPipe? s_createSteamPipe;
    private static NativeConnectToGlobalUser? s_connectToGlobalUser;
    private static NativeReleaseUser? s_releaseUser;
    private static NativeBReleaseSteamPipe? s_bReleaseSteamPipe;
    private static NativeBreakpadSetAppId? s_breakpadSetAppId;
    private static NativeBreakpadSetSteamId? s_breakpadSetSteamId;
    private static NativeReleaseThreadLocalMemory? s_releaseThreadLocalMemory;

    #endregion

    static SteamApi()
    {
        s_library = LoadSteamClientDllFromRegistry();
        if (s_library != IntPtr.Zero)
            LoadExports();
    }

    public static bool Initialize(uint appId, out string errorMessage)
    {
        errorMessage = "";

        if (s_library == IntPtr.Zero)
        {
            errorMessage = "steamclient.dll not found. Is Steam installed?";
            return false;
        }

        if (s_createSteamPipe == null || s_connectToGlobalUser == null)
        {
            errorMessage = "Required steamclient exports not found, wtf?";
            return false;
        }

        s_hSteamPipe = s_createSteamPipe();
        if (s_hSteamPipe == 0)
        {
            errorMessage = "Failed to create Steam pipe";
            return false;
        }

        s_hSteamUser = s_connectToGlobalUser(s_hSteamPipe);
        if (s_hSteamUser == 0)
        {
            errorMessage = "Failed to connect to global Steam user. Is Steam running?";
            return false;
        }

        s_breakpadSetAppId?.Invoke(appId);

        s_isInitialized = true;
        return true;
    }

    public static void Shutdown()
    {
        if (!s_isInitialized) return;

        s_releaseUser?.Invoke(s_hSteamPipe, s_hSteamUser);
        s_bReleaseSteamPipe?.Invoke(s_hSteamPipe);

        s_isInitialized = false;
        s_hSteamPipe = 0;
        s_hSteamUser = 0;
    }

    public static void RunCallbacks()
    {
        if (!s_isInitialized) return;
        s_releaseThreadLocalMemory?.Invoke();
    }

    public static IntPtr GetSteamClient()
    {
        if (s_library == IntPtr.Zero) return IntPtr.Zero;
        var createInterface = GetExport<NativeCreateInterface>("CreateInterface");
        return createInterface?.Invoke("SteamClient023") ?? IntPtr.Zero;
    }

    public static IntPtr GetSteamApps() => IntPtr.Zero;
    public static IntPtr GetSteamUser() => IntPtr.Zero;

    #region DLL loading

    private static IntPtr LoadSteamClientDllFromRegistry()
    {
        var steamPath = FindSteamPath();
        if (steamPath == null) return IntPtr.Zero;
        return LoadSteamClientDllFromPath(steamPath);
    }

    private static IntPtr LoadSteamClientDllFromPath(string basePath)
    {
        bool is64Bit = Environment.Is64BitProcess;
        string dllName = is64Bit ? "steamclient64.dll" : "steamclient.dll";

        string[] searchPaths =
        [
            Path.Combine(basePath, "bin", is64Bit ? "win64" : "", dllName),
            Path.Combine(basePath, "bin", dllName),
            Path.Combine(basePath, dllName)
        ];

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
                return NativeLibrary.Load(path);
        }

        return IntPtr.Zero;
    }

    private static string? FindSteamPath()
    {
        try
        {
            var value = Microsoft.Win32.Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            if (!string.IsNullOrEmpty(value))
                return value.Replace('/', '\\');
        }
        catch { }

        try
        {
            var value = Microsoft.Win32.Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) as string;
            if (!string.IsNullOrEmpty(value))
                return value;
        }
        catch { }

        try
        {
            var value = Microsoft.Win32.Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null) as string;
            if (!string.IsNullOrEmpty(value))
                return value;
        }
        catch { }

        return null;
    }

    #endregion

    #region Export loading

    private static void LoadExports()
    {
        s_createSteamPipe = GetExport<NativeCreateSteamPipe>("Steam_CreateSteamPipe");
        s_connectToGlobalUser = GetExport<NativeConnectToGlobalUser>("Steam_ConnectToGlobalUser");
        s_releaseUser = GetExport<NativeReleaseUser>("Steam_ReleaseUser");
        s_bReleaseSteamPipe = GetExport<NativeBReleaseSteamPipe>("Steam_BReleaseSteamPipe");
        s_breakpadSetAppId = GetExport<NativeBreakpadSetAppId>("Breakpad_SteamSetAppID");
        s_breakpadSetSteamId = GetExport<NativeBreakpadSetSteamId>("Breakpad_SteamSetSteamID");
        s_releaseThreadLocalMemory = GetExport<NativeReleaseThreadLocalMemory>("Steam_ReleaseThreadLocalMemory");
    }

    private static T? GetExport<T>(string name) where T : Delegate
    {
        if (s_library == IntPtr.Zero) return null;
        var addr = NativeLibrary.GetExport(s_library, name);
        if (addr == IntPtr.Zero) return null;
        return Marshal.GetDelegateForFunctionPointer<T>(addr);
    }

    #endregion
}
