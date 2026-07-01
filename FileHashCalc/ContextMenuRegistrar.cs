using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FileHashCalc;

public static class ContextMenuRegistrar
{
    private const string KeyName = @"Software\Classes\*\shell\FileHashCalc";
    private const string SendToLinkName = "File Hash berechnen.lnk";

    public static bool IsRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyName);
        return key != null;
    }

    public static bool IsSendToRegistered()
    {
        var sendToDir = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
        return File.Exists(Path.Combine(sendToDir, SendToLinkName));
    }

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

    public static void RegisterSendTo()
    {
        var exePath = Process.GetCurrentProcess().MainModule!.FileName;
        var sendToDir = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
        var linkPath = Path.Combine(sendToDir, SendToLinkName);

        var shellLink = (IShellLink)new ShellLink();
        shellLink.SetPath(exePath);
        shellLink.SetDescription("File Hash berechnen");
        shellLink.SetIconLocation(exePath, 0);

        var persistFile = (IPersistFile)shellLink;
        persistFile.Save(linkPath, false);

        Marshal.ReleaseComObject(persistFile);
        Marshal.ReleaseComObject(shellLink);
    }

    public static void UnregisterSendTo()
    {
        var sendToDir = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
        var linkPath = Path.Combine(sendToDir, SendToLinkName);
        if (File.Exists(linkPath))
            File.Delete(linkPath);
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLink
    {
        void GetPath(nint pszFile, int cch, nint pfd, int fFlags);
        void GetIDList(out nint ppidl);
        void SetIDList(nint pidl);
        void GetDescription(nint pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory(nint pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments(nint pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation(nint pszIconPath, int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(nint hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile(out nint ppszFileName);
    }
}
