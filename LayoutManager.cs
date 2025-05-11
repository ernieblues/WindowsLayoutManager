// System
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;


// COM Interop
using SHDocVw; // COM Reference: Microsoft Internet (InternetExplorer, ShellWindows)
using Shell32; // COM Reference: Microsoft Shell Controls and Automation (Folder, FolderItem, etc.)

public static class LayoutManager
{
    // ==================================================
    //                    Declarations
    // ==================================================

    // List of current layouts
    public static List<Layout> layouts = new();

    // Stack of layout states for undo functionality
    public static Stack<List<Layout>> undoLayoutStack = new();

    // ==================================================
    //                     Win32 API
    // ==================================================

    // Win32 API constants and handles
    private static readonly IntPtr HWND_TOP = new IntPtr(0); // Used with SetWindowPos to position window at the top (ignored if SWP_NOZORDER is set)
    private const uint SWP_NOZORDER = 0x0004;                // Retains the current Z-order (ignores hWndInsertAfter in SetWindowPos)
    private const uint SWP_NOACTIVATE = 0x0010;              // Prevents the window from gaining focus
    private const uint WM_CLOSE = 0x0010;                    // Tells a window to close (like clicking the "X" button)

    // Imports the native Win32 PostMessage function from user32.dll.
    // Used to send messages (like WM_CLOSE) to window handles asynchronously.
    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    // Moves and resizes a window with optional z-order and repaint flags.
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    // ==================================================
    //                   Public Methods
    // ==================================================

    // Closes an Explorer window by sending it a WM_CLOSE message.
    public static void CloseWindow(long HWnd)
    {
        PostMessage(new IntPtr(HWnd), WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
    }

    // Gets information from all open Explorer windows using the Shell COM interface.
    public static List<WindowInfo> GetWindowsInfo()
    {
        List<ApplicationInfo> appList = new();
        List<WindowInfo> windowList = new();

        Shell shell = new Shell();
        ShellWindows windows = (ShellWindows)shell.Windows();

        foreach (InternetExplorer window in windows)
        {
            // Protect against null references (COM sometimes gives garbage)
            if (window == null)
                continue;

            // Make sure this is a Windows Explorer window, not browser or other application
            if (!string.Equals(Path.GetFileName(window.FullName), "explorer.exe", StringComparison.OrdinalIgnoreCase))
                continue;

            Type type = window.GetType();

            // Capture the window information
            var info = new WindowInfo
            {
                Path = window.Document.Folder.Self.Path,
                HWnd = TryGetComProp<long>(type, "HWND", window),
                LocationName = TryGetComProp<string>(type, "LocationName", window),
                LocationURL = TryGetComProp<string>(type, "LocationURL", window),
                Left = TryGetComProp<int>(type, "Left", window),
                Top = TryGetComProp<int>(type, "Top", window),
                Width = TryGetComProp<int>(type, "Width", window),
                Height = TryGetComProp<int>(type, "Height", window)
            };

            // Add the window info to the list of windows
            windowList.Add(info);
        }
        
        return windowList;
    }

    // Loads the saved layouts from the layouts.json file in the project directory.
    public static List<Layout> LoadLayoutsFromFile()
    {
        string outputPath = AppDomain.CurrentDomain.BaseDirectory;
        string projectPath = Path.GetFullPath(Path.Combine(outputPath, @"..\..\..\"));
        string filePath = Path.Combine(projectPath, "layouts.json");

        if (!File.Exists(filePath))
            return new List<Layout>();

        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<Layout>>(json);
    }

    // Moves and resizes the specified window to match its saved layout values
    public static void MoveResizeWindow(WindowInfo window)
    {
        SetWindowPos(
        new IntPtr(window.HWnd),
        HWND_TOP,
        window.Left,
        window.Top,
        window.Width,
        window.Height,
        SWP_NOZORDER | SWP_NOACTIVATE);
    }

    // Restores a saved windows layout
    public static void RestoreLayout(Layout selectedLayout)
    {
        List<WindowInfo> openWindows = GetWindowsInfo();

        // Close open windows not in the layout
        foreach (WindowInfo openWindow in openWindows)
        {
            bool inLayout = selectedLayout.Windows.Any(layoutWindow =>
                layoutWindow.Path.Equals(openWindow.Path, StringComparison.OrdinalIgnoreCase));

            if (!inLayout)
            {
                CloseWindow(openWindow.HWnd);
            }
        }

        // Open windows in the layout not already opened
        foreach (WindowInfo layoutWindow in selectedLayout.Windows)
        {
            bool alreadyOpen = openWindows.Any(openWindow =>
                openWindow.Path.Equals(layoutWindow.Path, StringComparison.OrdinalIgnoreCase));

            if (!alreadyOpen)
            {
                // Launch a new Explorer window at the saved path
                Process.Start("explorer.exe", layoutWindow.Path);
            }
        }

        Thread.Sleep(1000); // Wait for new Explorer windows to fully open before scanning
        openWindows = GetWindowsInfo();

        // Match layout windows to live windows by path, and move them using fresh HWNDs
        foreach (WindowInfo layoutWindow in selectedLayout.Windows)
        {
            var liveWindow = openWindows.FirstOrDefault(w =>
                w.Path.Equals(layoutWindow.Path, StringComparison.OrdinalIgnoreCase));

            if (liveWindow != null)
            {
                // Use the saved layout's position, but the live window's current handle
                liveWindow.Left = layoutWindow.Left;
                liveWindow.Top = layoutWindow.Top;
                liveWindow.Width = layoutWindow.Width;
                liveWindow.Height = layoutWindow.Height;

                MoveResizeWindow(liveWindow);
            }
        }

    }

    // Creates a layout with the specified name.
    public static void SaveLayout(string layoutName)
    {
        // Add the list of windows to the layout
        var currentLayout = new Layout
        {
            LayoutName = layoutName,
            LayoutDate = DateTime.Now,
            Windows = new List<WindowInfo>(LayoutManager.GetWindowsInfo()),
            Applications = new List<ApplicationInfo>()
        };

        // Check if a layout with the same name already exists
        var existingLayout = layouts.FirstOrDefault(l => l.LayoutName == layoutName);

        if (existingLayout != null)
        {
            var result = MessageBox.Show(
                $"A layout named '{layoutName}' already exists.\nDo you want to overwrite it?",
                "Confirm Overwrite",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            // Overwrite it
            existingLayout.LayoutDate = currentLayout.LayoutDate;
            existingLayout.Windows = currentLayout.Windows;
            existingLayout.Applications = currentLayout.Applications;
        }
        else
        {
            layouts.Add(currentLayout);
        }

        SaveLayoutsToFile(layouts);
    }

    // Saves list of layouts to the layouts.json file in the project directory.
    public static void SaveLayoutsToFile(List<Layout> layouts)
    {
        string json = JsonSerializer.Serialize(layouts, new JsonSerializerOptions { WriteIndented = true });
        string outputPath = AppDomain.CurrentDomain.BaseDirectory;
        string projectPath = Path.GetFullPath(Path.Combine(outputPath, @"..\..\..\"));
        string filePath = Path.Combine(projectPath, "layouts.json");
        File.WriteAllText(filePath, json);
    }

    // Safely retrieves a property value from a COM object using reflection.
    private static T TryGetComProp<T>(Type type, string propName, object target)
    {
        try
        {
            return (T)type.InvokeMember(propName, BindingFlags.GetProperty, null, target, null);
        }
        catch
        {
            return default(T); // null for ref types, 0 for int/long, etc.
        }
    }
}