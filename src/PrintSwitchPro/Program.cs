using System.Windows.Forms;

namespace PrintSwitchPro;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var startMinimized = args.Any(a =>
            string.Equals(a, "/tray", StringComparison.OrdinalIgnoreCase)
            || string.Equals(a, "--tray", StringComparison.OrdinalIgnoreCase));

        Application.Run(new Forms.MainForm(startMinimized));
    }
}
