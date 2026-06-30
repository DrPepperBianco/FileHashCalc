using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace FileHashCalc;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<HashResult> _results = [];
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();

        AlgorithmCombo.ItemsSource = HashService.SupportedAlgorithms;
        AlgorithmCombo.SelectedItem = "SHA-256";

        _results = new ObservableCollection<HashResult>(ResultStore.Load());
        ResultsGrid.ItemsSource = _results;
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
            Clipboard.SetText($"{result.FileName}\t{result.Hash}\t{result.Algorithm}\t{result.Timestamp:dd.MM.yyyy HH:mm:ss}");
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

    private void BtnRegisterSendTo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ContextMenuRegistrar.RegisterSendTo();
            MessageBox.Show(
                "Verknüpfung wurde im 'Senden an'-Menü erstellt.\n\n" +
                "Rechtsklick auf Datei(en) → Senden an → 'File Hash berechnen'",
                "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exc)
        {
            MessageBox.Show(
                $"Registrierung fehlgeschlagen:\n{exc.Message}",
                "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnRegisterContext_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ContextMenuRegistrar.Register();
            MessageBox.Show(
                "Kontextmenü-Eintrag wurde registriert.\n\n" +
                "Rechtsklick auf eine Datei → 'File Hash berechnen'",
                "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exc)
        {
            MessageBox.Show(
                $"Registrierung fehlgeschlagen (Admin-Rechte erforderlich?):\n{exc.Message}",
                "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveResults() => ResultStore.Save([.. _results]);
}
