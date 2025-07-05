using Avalonia.Controls;
using System;
using System.Xml.Serialization;

namespace AvaloniaFantomGamesFacade.Controls;

public partial class SettingsMenu : UserControl
{

    public GameSettings ActiveSettings;


    private GameSettings _displaySettings;



    private GameSettings CheckValidity(GameSettings settings)
    {
        var seekerCount = Math.Max(4, Math.Min(5, settings.seekerCount));

        return new()
        {
            fantomOp = settings.fantomOp,
            seekerOp = settings.seekerOp,

            // Check limits 
            seekerCount = seekerCount,
            bobbyCount = Math.Max(0, Math.Min(2, settings.bobbyCount))
        };
    }

    public void ToggleVisibility()
    {

        if (IsVisible)
            IsVisible = false;
        else
        {
            _displaySettings = ActiveSettings;
            IsVisible = true;
        }
    }

    private void UpdateDisplay()
    {
        SeekerOpToggle.Content = _displaySettings.seekerOp.ToString();
        FantomOpToggle.Content = _displaySettings.fantomOp.ToString();
        DisplaySeekersCount.Text = _displaySettings.seekerCount.ToString();
        DisplayBobbiesCount.Text = _displaySettings.bobbyCount.ToString();
    }

    public SettingsMenu()
    {
        InitializeComponent();

        // default settings
        ActiveSettings = new()
        {
            fantomOp = false,
            seekerOp = false,
            seekerCount = 5,
            bobbyCount = 0
        };

        _displaySettings = ActiveSettings;
        UpdateDisplay();

        DataContext = this;
    }


    public Action<GameSettings>? OnApply { get; set; }
    public Action? OnExit { get; set; }

    // --------------------------------------------------------------------------

    private void Apply_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // _display settings are valid here
        ActiveSettings = _displaySettings;
        OnApply?.Invoke(ActiveSettings);
    }

    private void Exit_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        OnExit?.Invoke();
    }

    private void ValuesModify_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var name = (sender as Control).Name;

        var settings = _displaySettings;

        switch(name)
        {
            case "SeekerOpToggle": settings.seekerOp = !settings.seekerOp; break;
            case "FantomOpToggle": settings.fantomOp = !settings.fantomOp; break;
            case "SeekerCountDec": settings.seekerCount--; break;
            case "SeekerCountInc": settings.seekerCount++; break;
            case "BobbyCountDec": settings.bobbyCount--; break;
            case "BobbyCountInc": settings.bobbyCount++; break;
        }

        _displaySettings = CheckValidity(settings);
        UpdateDisplay();

    }

}

public struct GameSettings
{
    [XmlElement]
    public bool fantomOp;
    [XmlElement]
    public bool seekerOp;
    [XmlElement]
    public int seekerCount;
    [XmlElement]
    public int bobbyCount;
}