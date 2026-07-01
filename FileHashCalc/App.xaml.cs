using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FileHashCalc;

public partial class App : Application
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(nint hWnd);

    private const int SW_RESTORE = 9;
    private SingleInstance? _singleInstance;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new SingleInstance();

        if (!_singleInstance.IsFirstInstance)
        {
            _singleInstance.SendToRunningInstance(e.Args);
            _singleInstance.Dispose();
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow();

        _singleInstance.FilesReceived += files =>
        {
            mainWindow.Dispatcher.Invoke(async () =>
            {
                if (SettingsStore.Load().BringToFront)
                    BringToFront(mainWindow);
                await mainWindow.HashFilesAsync(files);
            });
        };

        _singleInstance.StartListening();
        mainWindow.Show();

        if (e.Args.Length > 0)
        {
            await mainWindow.HashFilesAsync(e.Args);
        }
    }

    private static void BringToFront(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (IsIconic(hwnd))
            ShowWindow(hwnd, SW_RESTORE);
        SetForegroundWindow(hwnd);
        window.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ThemeManager.Shutdown();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
