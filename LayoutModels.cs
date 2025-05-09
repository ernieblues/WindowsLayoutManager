// Layout of applications and windows.
public class Layout
{
    public string LayoutName { get; set; }
    public DateTime LayoutDate { get; set; } = DateTime.Now;
    public List<WindowInfo> Windows { get; set; } = new();
    public List<ApplicationInfo> Applications { get; set; } = new();
}

// Explorer windows information.
public class WindowInfo
{
    public string Path { get; set; }
    public long HWnd { get; set; }
    public string LocationName { get; set; }
    public string LocationURL { get; set; }
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

// Application information.
public class ApplicationInfo
{
    public string ExecutablePath { get; set; }
    public string Arguments { get; set; }
    public string WorkingDirectory { get; set; }
    public bool LaunchMaximized { get; set; }
}
