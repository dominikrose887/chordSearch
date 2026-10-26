using System.Runtime.InteropServices;
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
            ShowStartupFailure("Chord Search failed to start", ex);
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
            ShowStartupFailure("Chord Search failed to open the main window", ex);
            throw;
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        AppLog.Fatal($"UI UnhandledException: {e.Message}", e.Exception);
        ShowStartupFailure("Chord Search hit an unexpected error", e.Exception);

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

    /// <summary>Win32 message box so unpackaged startup failures are visible (no silent exit).</summary>
    private static void ShowStartupFailure(string title, Exception? ex)
    {
        try
        {
            var logHint = string.IsNullOrEmpty(AppLog.LogFilePath)
                ? "%LocalAppData%\\ChordSearch\\logs"
                : AppLog.LogFilePath;
            var detail = ex?.Message ?? "Unknown error";
            var text = $"{detail}\n\nLog:\n{logHint}";
            _ = MessageBoxW(IntPtr.Zero, text, title, 0x00000010); // MB_ICONERROR
        }
        catch
        {
            // never throw from error UI
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
