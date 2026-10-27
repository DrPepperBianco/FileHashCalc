using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace FileHashCalc;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<HashResult> _results = [];
    private CancellationTokenSource? _cts;
    private Dictionary<string, Brush?> _colorMap = new(StringComparer.OrdinalIgnoreCase);

    // Material Design Icons (Apache 2.0) — viewBox 0 0 24 24
    private static readonly Geometry IconSun = Geometry.Parse(
        "M12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9M12,2L14.39,5.42C13.65,5.15 12.84,5 12,5C11.16,5 10.35,5.15 9.61,5.42L12,2M3.34,7L7.5,6.65C6.9,7.16 6.36,7.78 5.94,8.5C5.5,9.24 5.25,10 5.11,10.79L3.34,7M3.36,17L5.12,13.23C5.26,14 5.53,14.78 5.95,15.5C6.37,16.24 6.91,16.86 7.5,17.37L3.36,17M20.65,7L18.88,10.79C18.74,10 18.47,9.23 18.05,8.5C17.63,7.78 17.1,7.15 16.5,6.64L20.65,7M20.64,17L16.5,17.36C17.09,16.85 17.62,16.22 18.04,15.5C18.46,14.77 18.73,14 18.87,13.21L20.64,17M12,22L9.59,18.56C10.33,18.83 11.14,19 12,19C12.82,19 13.63,18.83 14.37,18.56L12,22Z");
    private static readonly Geometry IconMoon = Geometry.Parse(
        "M17.75,4.09L15.22,6.03L16.13,9.09L13.5,7.28L10.87,9.09L11.78,6.03L9.25,4.09L12.44,4L13.5,1L14.56,4L17.75,4.09M21.25,11L19.61,12.25L20.2,14.23L18.5,13.06L16.8,14.23L17.39,12.25L15.75,11L17.81,10.95L18.5,9L19.19,10.95L21.25,11M18.97,15.95C19.8,15.87 20.69,17.05 20.16,17.8C19.84,18.25 19.5,18.67 19.08,19.07C15.17,23 8.84,23 4.94,19.07C1.03,15.17 1.03,8.83 4.94,4.93C5.34,4.53 5.76,4.17 6.21,3.85C6.96,3.32 8.14,4.21 8.06,5.04C7.79,7.9 8.75,10.87 10.95,13.06C13.14,15.26 16.1,16.22 18.97,15.95M17.33,17.97C14.5,17.81 11.7,16.64 9.53,14.5C7.36,12.31 6.2,9.5 6.04,6.68C3.23,9.82 3.34,14.64 6.35,17.66C9.37,20.67 14.19,20.78 17.33,17.97Z");
    private static readonly Geometry IconSunMoon = Geometry.Parse(
        "M7.5,2C5.71,3.15 4.5,5.18 4.5,7.5C4.5,9.82 5.71,11.85 7.53,13C4.46,13 2,10.54 2,7.5A5.5,5.5 0 0,1 7.5,2M19.07,3.5L20.5,4.93L4.93,20.5L3.5,19.07L19.07,3.5M12.89,5.93L11.41,5L9.97,6L10.39,4.3L9,3.24L10.75,3.12L11.33,1.47L12,3.1L13.73,3.13L12.38,4.26L12.89,5.93M9.59,9.54L8.43,8.81L7.31,9.59L7.65,8.27L6.56,7.44L7.92,7.35L8.37,6.06L8.88,7.33L10.24,7.36L9.19,8.23L9.59,9.54M19,13.5A5.5,5.5 0 0,1 13.5,19C12.28,19 11.15,18.6 10.24,17.93L17.93,10.24C18.6,11.15 19,12.28 19,13.5M14.6,20.08L17.37,18.93L17.13,22.28L14.6,20.08M18.93,17.38L20.08,14.61L22.28,17.15L18.93,17.38M20.08,12.42L18.94,9.64L22.28,9.88L20.08,12.42M9.63,18.93L12.4,20.08L9.87,22.27L9.63,18.93Z");

    public MainWindow()
    {
        InitializeComponent();

        ThemeManager.Init();
        ThemeManager.ThemeChanged += () => RefreshColors();
        ThemeManager.ThemeChanged += UpdateThemeButton;
        UpdateThemeButton();

        var settings = SettingsStore.Load();
        AlgorithmCombo.ItemsSource = HashService.SupportedAlgorithms;
        AlgorithmCombo.SelectedItem = settings.DefaultAlgorithm;

        _results = new ObservableCollection<HashResult>(ResultStore.Load());
        _results.CollectionChanged += Results_CollectionChanged;
        ResultsGrid.ItemsSource = _results;
        RefreshColors();
    }

    private void Results_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshColors();
    }

    private void RefreshColors()
    {
        _colorMap = HashColorService.BuildColorMap(_results);

        // Group duplicates together, ordered by newest timestamp per group
        var sorted = _results
            .GroupBy(r => r.Hash, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Max(r => r.Timestamp))
            .SelectMany(g => g.OrderByDescending(r => r.Timestamp))
            .ToList();

        if (!_results.SequenceEqual(sorted))
        {
            _results.CollectionChanged -= Results_CollectionChanged;
            _results.Clear();
            foreach (var item in sorted)
                _results.Add(item);
            _results.CollectionChanged += Results_CollectionChanged;
        }

        ResultsGrid.Items.Refresh();
    }

    private void ResultsGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (e.Row.DataContext is HashResult result &&
            _colorMap.TryGetValue(result.Hash, out var brush) && brush != null)
        {
            e.Row.Background = brush;
        }
        else
        {
            e.Row.ClearValue(DataGridRow.BackgroundProperty);
        }
    }

    public async Task HashFilesAsync(IEnumerable<string> filePaths)
    {
        var algorithm = (string)AlgorithmCombo.SelectedItem;
        var files = filePaths.Where(File.Exists).ToList();

        if (files.Count == 0)
            return;

        _cts = new CancellationTokenSource();
        ProgressBar.Visibility = Visibility.Visible;
        ProgressBar.Value = 0;
        BtnSelectFiles.IsEnabled = false;

        try
        {
            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                StatusText.Text = $"Berechne {Path.GetFileName(file)} ({i + 1}/{files.Count})...";

                var progress = new Progress<double>(p =>
                {
                    var overall = ((double)i + p) / files.Count * 100;
                    ProgressBar.Value = overall;
                });

                var result = await HashService.ComputeHashAsync(
                    file, algorithm, progress, _cts.Token);

                _results.Insert(0, result);
            }

            SaveResults();
            StatusText.Text = $"{files.Count} Datei(en) berechnet";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Abgebrochen";
        }
        catch (Exception exc)
        {
            MessageBox.Show(exc.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Fehler bei Berechnung";
        }
        finally
        {
            ProgressBar.Visibility = Visibility.Collapsed;
            BtnSelectFiles.IsEnabled = true;
            _cts = null;
        }
    }

    private void BtnSelectFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Title = "Dateien für Hash-Berechnung auswählen",
            Filter = "Alle Dateien (*.*)|*.*",
        };

        if (dialog.ShowDialog() == true)
        {
            _ = HashFilesAsync(dialog.FileNames);
        }
    }

    private void BtnClearHistory_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Verlauf wirklich löschen?", "Verlauf löschen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _results.Clear();
            SaveResults();
            StatusText.Text = "Verlauf gelöscht";
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            _ = HashFilesAsync(files);
        }
    }

    private void CopyHash_Click(object sender, RoutedEventArgs e)
    {
        if (ResultsGrid.SelectedItem is HashResult result)
        {
            Clipboard.SetText(result.Hash);
            StatusText.Text = "Hash kopiert";
        }
    }

    private void CopyRow_Click(object sender, RoutedEventArgs e)
    {
        if (ResultsGrid.SelectedItem is HashResult result)
        {
            Clipboard.SetText($"{result.FilePath}\t{result.Hash}\t{result.Algorithm}\t{result.Timestamp:dd.MM.yyyy HH:mm:ss}");
            StatusText.Text = "Zeile kopiert";
        }
    }

    private void RemoveEntry_Click(object sender, RoutedEventArgs e)
    {
        var selected = ResultsGrid.SelectedItems.Cast<HashResult>().ToList();
        foreach (var item in selected)
            _results.Remove(item);
        SaveResults();
    }

    private void BtnThemeCycle_Click(object sender, RoutedEventArgs e)
    {
        var next = (ThemeMode)(((int)ThemeManager.Mode + 1) % 3);
        ThemeManager.SetMode(next);
    }

    private void UpdateThemeButton()
    {
        var (icon, tooltip) = ThemeManager.Mode switch
        {
            ThemeMode.Light => (IconSun, "Light Mode — Klicken für Dark"),
            ThemeMode.Dark => (IconMoon, "Dark Mode — Klicken für System"),
            _ => (IconSunMoon, "System — Klicken für Light"),
        };
        ThemeIcon.Data = icon;
        BtnThemeCycle.ToolTip = tooltip;
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow { Owner = this };
        settingsWindow.ShowDialog();
    }

    private void SaveResults() => ResultStore.Save([.. _results]);
}
