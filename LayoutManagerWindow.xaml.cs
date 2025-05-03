// System
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Windows;

// COM Interop
using SHDocVw; // COM Reference: Microsoft Internet (InternetExplorer, ShellWindows)
using Shell32; // COM Reference: Microsoft Shell Controls and Automation (Folder, FolderItem, etc.)

namespace WindowsLayoutManager
{
    public partial class LayoutManagerWindow : Window
    {
        // Declarations
        public static List<ExplorerWindowInfo> layouts = new List<ExplorerWindowInfo>();

        // Constructor
        public LayoutManagerWindow()
        {
            InitializeComponent();
            GetExplorerWindowsInfo();
            SaveLayoutsToFile(layouts);
            //OpenWindows();
            //ShellWindowsCOMProperties.Run();
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
        //             Explorer Window Management
        // ==================================================

        // Saves layouts to a JSON file
        public static void SaveLayoutsToFile(List<ExplorerWindowInfo> layouts)
        {
            string json = JsonSerializer.Serialize(layouts, new JsonSerializerOptions { WriteIndented = true });
            string outputPath = AppDomain.CurrentDomain.BaseDirectory;
            string projectPath = Path.GetFullPath(Path.Combine(outputPath, @"..\..\..\"));
            string filePath = Path.Combine(projectPath, "layouts.json");
            File.WriteAllText(filePath, json);
        }

        // Safely retrieves a property value from a COM object using reflection.
        // Returns default(T) if the property is missing or inaccessible.
        private T TryGetComProp<T>(Type type, string propName, object target)
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

        // Retrieves information from open Windows Explorer windows using Shell COM.
        private void GetExplorerWindowsInfo()
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

                Type type = window.GetType();

                // Store the captured window information
                layouts.Add(new ExplorerWindowInfo
                {
                    Path = window.Document.Folder.Self.Path,
                    HWND = TryGetComProp<long>(type, "HWND", window),
                    LocationName = TryGetComProp<string>(type, "LocationName", window),
                    LocationURL = TryGetComProp<string>(type, "LocationURL", window),
                    Top = TryGetComProp<int>(type, "Top", window),
                    Left = TryGetComProp<int>(type, "Left", window),
                    Width = TryGetComProp<int>(type, "Width", window),
                    Height = TryGetComProp<int>(type, "Height", window),
                });
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
