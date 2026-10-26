using ChordSearch.App.Diagnostics;
using ChordSearch.App.ViewModels;
using ChordSearch.Core.Results;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace ChordSearch.App;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; private set; } = null!;

    public MainPage()
    {
        AppLog.Info("MainPage ctor begin");
        try
        {
            InitializeComponent();
            AppLog.Info("MainPage InitializeComponent OK");

            ViewModel = new MainViewModel();
            ViewModel.AttachDispatcher(DispatcherQueue);
            AppLog.Info("MainViewModel created");

            DataContext = ViewModel;
            Fretboard.NoteClicked += async (_, pitch) =>
            {
                try { await ViewModel.PlayPitchAsync(pitch); }
                catch (Exception ex) { AppLog.Error("PlayPitch from fretboard failed", ex); }
            };
            AppLog.Info("MainPage ctor complete");
        }
        catch (Exception ex)
        {
            AppLog.Fatal("MainPage ctor failed", ex);
            throw;
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        AppLog.Info("MainPage OnNavigatedTo");
        base.OnNavigatedTo(e);
        // ContentDialog needs XamlRoot after loaded
        SettingsDialog.XamlRoot = XamlRoot;
    }

    private async void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SettingsDialog.XamlRoot = XamlRoot;
            await SettingsDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            AppLog.Error("Settings dialog failed", ex);
        }
    }

    private void SettingsDialog_PrimaryClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            ViewModel.ApplyTuningFromEditorCommand.Execute(null);
        }
        catch (Exception ex)
        {
            AppLog.Error("Apply tuning from settings failed", ex);
        }
    }

    private void RootGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        try
        {
            if (e.ClickedItem is ChipItem item)
                ViewModel.SelectRootCommand.Execute(item);
        }
        catch (Exception ex) { AppLog.Error("RootGrid_ItemClick", ex); }
    }

    private void QualityGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        try
        {
            if (e.ClickedItem is ChipItem item)
                ViewModel.SelectQualityCommand.Execute(item);
        }
        catch (Exception ex) { AppLog.Error("QualityGrid_ItemClick", ex); }
    }

    private void ExtensionGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        try
        {
            if (e.ClickedItem is ChipItem item)
                ViewModel.ToggleExtensionCommand.Execute(item);
        }
        catch (Exception ex) { AppLog.Error("ExtensionGrid_ItemClick", ex); }
    }

    private void BassGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        try
        {
            if (e.ClickedItem is BassOption item)
                ViewModel.SelectBassCommand.Execute(item);
        }
        catch (Exception ex) { AppLog.Error("BassGrid_ItemClick", ex); }
    }

    private void ResultList_ItemClick(object sender, ItemClickEventArgs e)
    {
        try
        {
            if (e.ClickedItem is VoicingResult result)
                ViewModel.SelectedResult = result;
        }
        catch (Exception ex) { AppLog.Error("ResultList_ItemClick", ex); }
    }
}
