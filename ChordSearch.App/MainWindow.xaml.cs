using ChordSearch.App.Diagnostics;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ChordSearch.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        AppLog.Info("MainWindow ctor begin");
        try
        {
            InitializeComponent();
            AppLog.Info("MainWindow InitializeComponent OK");

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            try { AppWindow.SetIcon("Assets/AppIcon.ico"); }
            catch (Exception ex) { AppLog.Warn("SetIcon failed: " + ex.Message); }

            MaximizeOnLaunch();

            Closed += OnClosed;

            AppLog.Info("Navigating to MainPage");
            RootFrame.Navigate(typeof(MainPage));
            AppLog.Info("Navigate(MainPage) requested");
        }
        catch (Exception ex)
        {
            AppLog.Fatal("MainWindow ctor failed", ex);
            throw;
        }
    }

    private void MaximizeOnLaunch()
    {
        try
        {
            if (AppWindow.Presenter is not OverlappedPresenter presenter)
            {
                AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
                presenter = (OverlappedPresenter)AppWindow.Presenter;
            }

            presenter.Maximize();
            AppLog.Info("MainWindow maximized on launch");
        }
        catch (Exception ex)
        {
            AppLog.Warn("MaximizeOnLaunch failed: " + ex.Message);
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        try
        {
            if (RootFrame.Content is MainPage page)
                page.ViewModel.DisposeMidi();
            AppLog.Info("MainWindow closed — MIDI disposed");
        }
        catch (Exception ex)
        {
            AppLog.Error("MainWindow close cleanup failed", ex);
        }
    }
}
