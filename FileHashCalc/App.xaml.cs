using System.Windows;

namespace FileHashCalc;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new MainWindow();
        mainWindow.Show();

        if (e.Args.Length > 0)
        {
            await mainWindow.HashFilesAsync(e.Args);
        }
    }
}
