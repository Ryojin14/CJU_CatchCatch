using System.Windows;
using CJUCatch.Client.Desktop.Services;
using CJUCatch.Client.Desktop.Views;

namespace CJUCatch.Client.Desktop;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private CharacterWindow? _characterWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RegisterGlobalExceptionHandlers();

        _mainWindow = new MainWindow();
        _characterWindow = new CharacterWindow(_mainWindow);
        _mainWindow.AttachCharacterWindow(_characterWindow);

        MainWindow = _mainWindow;
        _mainWindow.Show();
        _characterWindow.Show();
    }

    public void ExitApplication()
    {
        _characterWindow?.Close();
        _mainWindow?.CloseWithoutHiding();
        Shutdown();
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += (_, args) =>
        {
            ErrorLogger.Log("DispatcherUnhandledException", args.Exception);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                ErrorLogger.Log("AppDomainUnhandledException", ex);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            ErrorLogger.Log("TaskSchedulerUnobservedTaskException", args.Exception);
            args.SetObserved();
        };
    }
}
