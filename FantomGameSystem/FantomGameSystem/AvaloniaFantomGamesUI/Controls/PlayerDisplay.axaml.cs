using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System;

namespace AvaloniaFantomGamesFacade.Controls;

public partial class PlayerDisplay : UserControl
{

    public static readonly StyledProperty<Bitmap> LogoProperty =
        AvaloniaProperty.Register<PlayerDisplay, Bitmap>(nameof(Logo));

    public Bitmap Logo
    {
        get => GetValue(LogoProperty); 
        set => SetValue(LogoProperty, value);
    }

    public void DisableTicketDisplay()
    {
        Ticket1.Text = "--";
        Ticket2.Text = "--";
        Ticket3.Text = "--";

        Status.IsVisible = false;
    }

    public void SetTicket(int ticket, int value)
    {
        switch(ticket)
        {
            case 0:
                Ticket1.Text = value.ToString();
                break;
            
            case 1:
                Ticket2.Text = value.ToString();
                break;

            case 2:
                Ticket3.Text = value.ToString();
                break;

        }
    }

    public void Played()
    {
        Status.IsVisible = true;
    }

    public void Reset()
    {
        Status.IsVisible = false;
    }
 
    public PlayerDisplay()
    {        
        InitializeComponent();
        DataContext = this;
    }
}