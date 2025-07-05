using Avalonia.Controls;
using Avalonia.Input;

namespace AvaloniaFantomGamesFacade.Views;

public partial class MainWindow : Window
{

    public MainView mainView;

    public MainWindow()
    {
        InitializeComponent();
        mainView = Main;
    }

    private void OnKeyDown(object? sender, KeyEventArgs args)
    {
        if(args.Key == Key.Space) 
            mainView.ResetView();
    }
}
