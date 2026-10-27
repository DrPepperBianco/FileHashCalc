using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace FileHashCalc;

public enum ThemeMode { System, Light, Dark }

public static class ThemeManager
{
    public static bool IsDark { get; private set; }
    public static ThemeMode Mode { get; private set; } = ThemeMode.System;

    private static CancellationTokenSource? _watcherCts;

    private const string ThemeRegistryPath =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    [DllImport("advapi32.dll")]
    private static extern int RegNotifyChangeKeyValue(
        nint hKey, bool watchSubtree, int notifyFilter, nint hEvent, bool asynchronous);

    private const int REG_NOTIFY_CHANGE_LAST_SET = 0x00000004;

    private static readonly Dictionary<string, Color> LightColors = new()
    {
        ["WindowBackground"] = Color.FromRgb(0xFF, 0xFF, 0xFF),
        ["TextForeground"] = Color.FromRgb(0x1E, 0x1E, 0x1E),
        ["SubtleForeground"] = Color.FromRgb(0x88, 0x88, 0x88),
        ["AccentBrush"] = Color.FromRgb(0x25, 0x63, 0xEB),
        ["SelectionBackground"] = Color.FromRgb(0xCF, 0xE2, 0xFA),
        ["SelectionForeground"] = Color.FromRgb(0x1E, 0x1E, 0x1E),
        ["GridBackground"] = Color.FromRgb(0xFF, 0xFF, 0xFF),
        ["GridRowAlt"] = Color.FromRgb(0xF5, 0xF9, 0xFC),
        ["GridBorder"] = Color.FromRgb(0xCC, 0xCC, 0xCC),
        ["GridHeaderBackground"] = Color.FromRgb(0xF0, 0xF0, 0xF0),
        ["GridHeaderForeground"] = Color.FromRgb(0x1E, 0x1E, 0x1E),
        ["GridCellForeground"] = Color.FromRgb(0x1E, 0x1E, 0x1E),
        ["ButtonBackground"] = Color.FromRgb(0xE8, 0xE8, 0xE8),
        ["ButtonForeground"] = Color.FromRgb(0x1E, 0x1E, 0x1E),
        ["ButtonBorder"] = Color.FromRgb(0xCC, 0xCC, 0xCC),
        ["ComboBackground"] = Color.FromRgb(0xFF, 0xFF, 0xFF),
        ["ComboForeground"] = Color.FromRgb(0x1E, 0x1E, 0x1E),
    };

    private static readonly Dictionary<string, Color> DarkColors = new()
    {
        ["WindowBackground"] = Color.FromRgb(0x1E, 0x1E, 0x1E),
        ["TextForeground"] = Color.FromRgb(0xE0, 0xE0, 0xE0),
        ["SubtleForeground"] = Color.FromRgb(0x88, 0x88, 0x88),
        ["AccentBrush"] = Color.FromRgb(0x6B, 0x9B, 0xF5),
        ["SelectionBackground"] = Color.FromRgb(0x1C, 0x4A, 0x75),
        ["SelectionForeground"] = Color.FromRgb(0xFF, 0xFF, 0xFF),
        ["GridBackground"] = Color.FromRgb(0x25, 0x25, 0x25),
        ["GridRowAlt"] = Color.FromRgb(0x2A, 0x2A, 0x2A),
        ["GridBorder"] = Color.FromRgb(0x44, 0x44, 0x44),
        ["GridHeaderBackground"] = Color.FromRgb(0x33, 0x33, 0x33),
        ["GridHeaderForeground"] = Color.FromRgb(0xE0, 0xE0, 0xE0),
        ["GridCellForeground"] = Color.FromRgb(0xE0, 0xE0, 0xE0),
        ["ButtonBackground"] = Color.FromRgb(0x3A, 0x3A, 0x3A),
        ["ButtonForeground"] = Color.FromRgb(0xE0, 0xE0, 0xE0),
        ["ButtonBorder"] = Color.FromRgb(0x55, 0x55, 0x55),
        ["ComboBackground"] = Color.FromRgb(0x33, 0x33, 0x33),
        ["ComboForeground"] = Color.FromRgb(0xE0, 0xE0, 0xE0),
    };

    public static event Action? ThemeChanged;

    public static void Init()
    {
        Mode = SettingsStore.Load().ThemeMode;
        ResolveDark();
        Apply();
        StartRegistryWatcher();
    }

    public static void SetMode(ThemeMode mode)
    {
        Mode = mode;
        var settings = SettingsStore.Load();
        settings.ThemeMode = mode;
        SettingsStore.Save(settings);
        ResolveDark();
        Apply();
        ThemeChanged?.Invoke();
    }

    private static void StartRegistryWatcher()
    {
        _watcherCts = new CancellationTokenSource();
        var ct = _watcherCts.Token;

        Task.Run(() =>
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(ThemeRegistryPath);
                if (key == null) return;

                var handle = key.Handle.DangerousGetHandle();

                while (!ct.IsCancellationRequested)
                {
                    // Blocks until a value in this key changes
                    int result = RegNotifyChangeKeyValue(handle, false, REG_NOTIFY_CHANGE_LAST_SET, 0, false);
                    if (result != 0 || ct.IsCancellationRequested)
                        break;

                    if (Mode != ThemeMode.System)
                        continue;

                    var wasDark = IsDark;
                    ResolveDark();

                    if (wasDark != IsDark)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Apply();
                            ThemeChanged?.Invoke();
                        });
                    }
                }
            }
            catch
            {
                // Registry watch failed — silent fallback
            }
        }, ct);
    }

    private static void ResolveDark()
    {
        IsDark = Mode switch
        {
            ThemeMode.Light => false,
            ThemeMode.Dark => true,
            _ => ReadSystemDark(),
        };
    }

    private static bool ReadSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(ThemeRegistryPath);
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int lightTheme)
                return lightTheme == 0;
        }
        catch
        {
            // ignore
        }
        return false;
    }

    public static void Shutdown()
    {
        _watcherCts?.Cancel();
        _watcherCts?.Dispose();
        _watcherCts = null;
    }

    private static void Apply()
    {
        var colors = IsDark ? DarkColors : LightColors;
        var resources = Application.Current.Resources;

        foreach (var (key, color) in colors)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            resources[key] = brush;
        }
    }
}
