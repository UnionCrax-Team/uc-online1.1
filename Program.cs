using System;
using System.Windows.Forms;
using UCOnline;

namespace UCOnline;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var launcher = new SteamLauncher();

        if (!launcher.Initialize())
        {
            MessageBox.Show("Failed to initialize Steam.\nEnsure Steam is installed and running.", "uc-online", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!string.IsNullOrEmpty(launcher.Config.GameExecutable))
        {
            var process = launcher.LaunchGame();

            if (process != null)
            {
                process.WaitForExit();
            }
            else
            {
                MessageBox.Show($"Game executable not found:\n{launcher.Config.GameExecutable}", "uc-online", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        else
        {
            MessageBox.Show("No game executable configured in union-crax.ini", "uc-online", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
