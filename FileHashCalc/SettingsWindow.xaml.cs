using System.Windows;
using System.Windows.Controls;

namespace FileHashCalc;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private bool _initialized;

    public SettingsWindow()
    {
        InitializeComponent();

        _settings = SettingsStore.Load();

        ThemeCombo.ItemsSource = new[] { "System", "Light", "Dark" };
        ThemeCombo.SelectedIndex = (int)_settings.ThemeMode;

        AlgorithmCombo.ItemsSource = HashService.SupportedAlgorithms;
        AlgorithmCombo.SelectedItem = _settings.DefaultAlgorithm;

        ChkBringToFront.IsChecked = _settings.BringToFront;

        UpdateRegistrationButtons();

        _initialized = true;
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;
        ThemeManager.SetMode((ThemeMode)ThemeCombo.SelectedIndex);
    }

    private void AlgorithmCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized || AlgorithmCombo.SelectedItem is not string algo) return;
        _settings.DefaultAlgorithm = algo;
        SaveSettings();
    }

    private void ChkBringToFront_Changed(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        _settings.BringToFront = ChkBringToFront.IsChecked == true;
        SaveSettings();
    }

    private void BtnContextMenu_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ContextMenuRegistrar.IsRegistered())
                ContextMenuRegistrar.Unregister();
            else
                ContextMenuRegistrar.Register();
            UpdateRegistrationButtons();
        }
        catch (Exception exc)
        {
            MessageBox.Show(exc.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnSendTo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ContextMenuRegistrar.IsSendToRegistered())
                ContextMenuRegistrar.UnregisterSendTo();
            else
                ContextMenuRegistrar.RegisterSendTo();
            UpdateRegistrationButtons();
        }
        catch (Exception exc)
        {
            MessageBox.Show(exc.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateRegistrationButtons()
    {
        BtnContextMenu.Content = ContextMenuRegistrar.IsRegistered()
            ? "Entfernen" : "Registrieren";
        BtnSendTo.Content = ContextMenuRegistrar.IsSendToRegistered()
            ? "Entfernen" : "Registrieren";
    }

    private void SaveSettings() => SettingsStore.Save(_settings);

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
