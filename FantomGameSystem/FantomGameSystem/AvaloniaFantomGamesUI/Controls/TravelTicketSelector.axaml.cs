using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AvaloniaFantomGamesFacade.Controls;

public partial class TravelTicketSelector : UserControl
{

    Dictionary<string, int> _buttonToCommand = new() { 
        { "Option0", 0 }, 
        { "Option1", 1 },
        { "Option2", 2 }, 
        { "Option3", 3 }, 
        { "Option4", 4 }, 
        { "Option5", 5 } 
    };

    private Func<int, bool>? TravelTicketSelect;

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not null && TravelTicketSelect is not null)
        {
            var name = (sender as Control).Name;
            if (_buttonToCommand.TryGetValue(name, out int command))
            {                
                if(TravelTicketSelect(command))
                {
                    DeselectAll();

                    // Select this button
                    (sender as Button).Background = new SolidColorBrush(Colors.Gold);
                }
            }
        }
    }

    public void DeselectAll()
    {
        Option0.Background = new SolidColorBrush(Colors.Transparent);
        Option1.Background = new SolidColorBrush(Colors.Transparent);
        Option2.Background = new SolidColorBrush(Colors.Transparent);
        Option3.Background = new SolidColorBrush(Colors.Transparent);
        Option4.Background = new SolidColorBrush(Colors.Transparent);
    }

    public void Hide()
    {
        IsVisible = false;

        DeselectAll();
    }

    public void SeekerMode(int seekerIndex)
    {
        IsVisible = true;

        PieceSelected.Text = $"Seeker {seekerIndex}"; 
        Option3.IsVisible = false;
        Option4.IsVisible = false;
    }

    public void FantomMode()
    {
        IsVisible = true;

        PieceSelected.Text = $"The Fantom";
        Option3.IsVisible = true;
        Option4.IsVisible = true;
    }

    public void AttachCallback(Func<int, bool> onSelect)
    {
        TravelTicketSelect = onSelect;
    }

    public TravelTicketSelector()
    {
        InitializeComponent();
    }

    
}