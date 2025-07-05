using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using AvaloniaFantomGamesFacade.Views;
using ConsoleFantomGamesFacade;

namespace AvaloniaFantomGamesFacade.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task Main(string[] args)
    {
        /* NOTE: for debug logs.
        if (!Directory.Exists("../Logs/"))
            Directory.CreateDirectory("../Logs/");
        */

        MainWindow? window;

        // Start a separate Thread, use the command line in this one
        var uiThread = new Thread(() =>
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        });
        uiThread.Start();

        // await the MainWindow assignment
        window = await App.WaitForMainWindowAsync();
        window.mainView.ParentWindow = window;

        if (args.Length > 0 && args[0] == "--cli")
        {

            bool running = true;
            var intermediary = window.mainView.Commander;
            CommandParser parser = new(intermediary, window.mainView);

            while (running)
            {
                string? command = Normalize(Console.ReadLine());
                if (command == null || command == "exit")
                {
                    running = false;
                    // Clicked the Exit button in the game
                    if (!window.mainView.Exited)
                        try
                        {
                            // Start the job on the ui thread and return immediately.
                            Dispatcher.UIThread.Post(() => window.Close());
                            // intermediary.Exit();
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    break;
                }

                if (!parser.Parse(command))
                {
                    Console.Error.WriteLine($"'{command}' is not valid.");
                }
            }

        }


    }

    private static string? Normalize(string? raw)
    {
        return raw?.Trim().ToLower();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
