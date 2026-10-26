using ChordSearch.App.Diagnostics;
using Microsoft.UI.Xaml;

namespace ChordSearch.App;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        AppLog.Initialize();
        AppLog.Info("App ctor: before InitializeComponent");

        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            InitializeComponent();
            AppLog.Info("App ctor: InitializeComponent OK");
        }
        catch (Exception ex)
        {
            AppLog.Fatal("App ctor InitializeComponent failed", ex);
            throw;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppLog.Info("OnLaunched begin");
        try
        {
            _window = new MainWindow();
            AppLog.Info("MainWindow created");
            _window.Activate();
            AppLog.Info("MainWindow activated");
        }
        catch (Exception ex)
        {
            AppLog.Fatal("OnLaunched failed", ex);
            throw;
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        AppLog.Fatal($"UI UnhandledException: {e.Message}", e.Exception);

#if DEBUG
        // Keep process alive in Debug so the log can be inspected after a UI fault.
        e.Handled = true;
#else
        // Release: fail closed — do not leave a half-dead window running.
        e.Handled = false;
#endif
    }

    private static void OnDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        AppLog.Fatal($"AppDomain UnhandledException (IsTerminating={e.IsTerminating})", e.ExceptionObject as Exception);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        AppLog.Fatal("UnobservedTaskException", e.Exception);
        e.SetObserved();
    }
}
