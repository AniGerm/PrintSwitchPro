using System.Diagnostics;

namespace PrintSwitchPro.Windows;

internal static class ProcessScanner
{
    public static IReadOnlyList<string> GetProcessesWithWindows()
    {
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(process.MainWindowTitle)
                    && !string.IsNullOrWhiteSpace(process.ProcessName))
                {
                    names.Add(process.ProcessName);
                }
            }
            catch
            {
                // Access denied / exited process — ignore.
            }
            finally
            {
                process.Dispose();
            }
        }

        return names.ToList();
    }

    public static string? GetForegroundProcessName()
    {
        try
        {
            var hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                return null;
            }

            _ = NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0)
            {
                return null;
            }

            using var process = Process.GetProcessById((int)pid);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }
}
