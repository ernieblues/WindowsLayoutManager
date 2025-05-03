// ShellWindowsCOMProperties.cs

// ShellWindowsCOMProperties.Run();

using SHDocVw;
using Shell32;
using System;
using System.Reflection;
using System.Text;
using System.Windows;
using System.IO; // Needed for Path.GetFileName

// 🧪 Dev tool: Dumps properties from open Windows Explorer windows (via Shell COM)
public static class ShellWindowsCOMProperties
{
    public static void Run()
    {
        Shell shell = new Shell();
        ShellWindows windows = (ShellWindows)shell.Windows();

        MessageBox.Show($"Shell windows found: {windows.Count}", "PropsToTry");

        foreach (InternetExplorer window in windows)
        {
            if (window == null)
                continue;

            string exeName;
            try
            {
                exeName = Path.GetFileName(window.FullName);
            }
            catch
            {
                exeName = "[Unknown EXE]";
            }

            if (!string.Equals(exeName, "explorer.exe", StringComparison.OrdinalIgnoreCase))
                continue;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Explorer Window Properties ===");

            string[] propsToTry = new[]
            {
                "HWND",
                "FullName",
                "LocationURL",
                "LocationName",
                "Visible",
                "Top",
                "Left",
                "Width",
                "Height",
                "Busy",
                "ReadyState",
                "StatusText",
                "Offline",
                "Silent",
                "Type",
                "Parent",
                "Application",
                "Document",
                "View"
            };

            Type type = window.GetType();

            foreach (string prop in propsToTry)
            {
                try
                {
                    var value = type.InvokeMember(prop, BindingFlags.GetProperty, null, window, null);
                    sb.AppendLine($"{prop}: {value}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"{prop}: [ERROR] {ex.Message}");
                }
            }

            MessageBox.Show(sb.ToString(), "PropsToTry");
        }
    }
}