using Avalonia.Controls;

namespace AvaloniaFantomGamesFacade.Controls;

public partial class FantomDisplay : UserControl
{

    public void SetTicket(int ticket, int value)
    {
        switch (ticket)
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

            case 3:
                Ticket4.Text = value.ToString();
                break;

            case 4:
                Ticket5.Text = value.ToString();
                break;

        }
    }

    public FantomDisplay()
    {
        InitializeComponent();
        DataContext = this;
    }
}