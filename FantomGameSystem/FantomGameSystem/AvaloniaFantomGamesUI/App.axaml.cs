using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using AvaloniaFantomGamesFacade.ViewModels;
using AvaloniaFantomGamesFacade.Views;
using System.Threading.Tasks;

namespace AvaloniaFantomGamesFacade;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private static readonly TaskCompletionSource<MainWindow> _windowReadyTcs = new();

    public static Task<MainWindow> WaitForMainWindowAsync() => _windowReadyTcs.Task;

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };

            desktop.MainWindow = mainWindow;
            desktop.Exit += mainWindow.mainView.OnExit;

            // complete the Task
            _windowReadyTcs.TrySetResult(mainWindow);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
