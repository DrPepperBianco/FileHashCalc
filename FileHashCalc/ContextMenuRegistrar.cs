using System.Diagnostics;
using Microsoft.Win32;

namespace FileHashCalc;

public static class ContextMenuRegistrar
{
    private const string KeyName = @"*\shell\FileHashCalc";

    public static void Register()
    {
        var exePath = Process.GetCurrentProcess().MainModule!.FileName;

        using var key = Registry.CurrentUser.CreateSubKey(KeyName);
        key.SetValue("", "File Hash berechnen");
        key.SetValue("Icon", exePath);

        using var commandKey = key.CreateSubKey("command");
        commandKey.SetValue("", $"\"{exePath}\" \"%1\"");
    }

    public static void Unregister()
    {
        Registry.CurrentUser.DeleteSubKeyTree(KeyName, throwOnMissingSubKey: false);
    }
}
