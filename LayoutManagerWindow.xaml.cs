using SHDocVw; // COM Reference: Microsoft Internet (InternetExplorer, ShellWindows)
using Shell32; // COM Reference: Microsoft Shell Controls and Automation (Folder, FolderItem, etc.)
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices; // COM Reference: Microsoft Shell Controls and Automation (COM interop e.g. GUIDs, interfaces)
using System.Text;
using System.Windows;

namespace WindowsLayoutManager
{
    public partial class LayoutManagerWindow : Window
    {
        // Constructor
        public LayoutManagerWindow()
        {
            InitializeComponent();
            GetExplorerWindows();
            GetExplorerPaths();
            OpenWindows();
        }

        // ==================================================
        //                   Event Handlers                  
        // ==================================================

        // Test Button
        private void ClickTestButton(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Test Button Clicked!");
        }

        // ==================================================
        //               Win32 API Declarations
        // ==================================================

        // Delegate definition for EnumWindows callback
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // Enumerates all top-level windows on the screen
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        // Retrieves the title (caption) text of a window
        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        // Checks if a window is visible
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        // Retrieves the bounding rectangle (position and size) of a window
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        // Retrieves the class name of a window (e.g., "CabinetWClass")
        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        // ==================================================
        //                 Struct Definitions
        // ==================================================

        // Structure to hold the coordinates of a window rectangle
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // ==================================================
        //             Explorer Window Management
        // ==================================================

        // Enumerates all open visible Windows Explorer windows and displays their size and position.
        private void GetExplorerWindows()
        {
            EnumWindows((hWnd, lParam) =>
            {
                // Skip windows that aren't visible on the screen
                if (!IsWindowVisible(hWnd))
                    return true;

                // Prepare to read the window's class name (e.g., "CabinetWClass" for Explorer)
                StringBuilder className = new StringBuilder(32);
                GetClassName(hWnd, className, className.Capacity);

                // Check if this window is a Windows Explorer window
                if (className.ToString().Contains("CabinetWClass"))
                {
                    // Get the window title text (usually the folder name or path)
                    StringBuilder windowText = new StringBuilder(256);
                    GetWindowText(hWnd, windowText, windowText.Capacity);

                    // Get the window's size and position on the screen
                    GetWindowRect(hWnd, out RECT rect);

                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;

                    // Display the captured window information
                    MessageBox.Show($"Explorer Window:\nTitle: {windowText}\nLocation: ({rect.Left},{rect.Top})\nSize: {width}x{height}");
                }

                // Continue enumerating remaining windows
                return true;
            }, IntPtr.Zero);
        }

        // Retrieves the folder paths from open Windows Explorer windows using Shell COM.
        private void GetExplorerPaths()
        {
            Shell shell = new Shell();
            ShellWindows windows = (ShellWindows)shell.Windows();

            // Display the number of Shell windows found
            MessageBox.Show("Shell windows found: " + windows.Count.ToString());

            foreach (InternetExplorer window in windows)
            {
                // Protect against null references (COM sometimes gives garbage)
                if (window == null)
                    continue;

                // Make sure this is a Windows Explorer window, not browser or other application
                if (!string.Equals(Path.GetFileName(window.FullName), "explorer.exe", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Display the folder path of the Explorer window
                MessageBox.Show($"Explorer Path: {window.Document.Folder.Self.Path}");
            }
        }

        // Opens a test Explorer window at a specified path.
        private void OpenWindows()
        {
            // Get the current user's Documents folder path (e.g., C:\Users\YourName\Documents)
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Process.Start("explorer.exe", documentsPath);
        }
    }
}
