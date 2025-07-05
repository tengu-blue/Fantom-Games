using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using AvaloniaFantomGamesFacade.Controls;
using ConsoleFantomGamesFacade;
using FantomGamesCore;
using FantomGamesIntermediary;
using FantomGamesSystemUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AvaloniaFantomGamesFacade.Views;

public partial class MainView : UserControl, IFacadeCommander, IFantomGamesFacade
{

    // calculating the x and y positions for the given tiles
    Dictionary<int, (int, int)> _boardTilesOffsets = [];
    Dictionary<(int, int), int> _offsetsToBoardTiles = [];

    private const float MIN_SCALE = 0.1f, MAX_SCALE = 1.0f;
    private const int BOARD_OFFSET_X = 82, BOARD_OFFSET_Y = 130;
    private const int TILE_SIZE = 130;
    private const float TILE_RADIUS = 32;
    private const int ODD_TILE_OFFSET = 5;
    private const int BOARD_WIDTH = 2870, BOARD_HEIGHT = 2870;

    private const int PIECE_SIZE = 93;

    private double _scale = 1.0;
    private double _offset_x = 0, _offset_y = 0;

    // private TextWriter logger;

    public bool Exited { get; private set; } = false;
    public MainWindow? ParentWindow { get; set; }
    public FantomIntermediary Commander { get; private set; }

    
    private IntermediarySettings ISettingsFromGameSettings()
    {
        return new()
        {
            ComputerFantom = Settings.ActiveSettings.fantomOp,
            ComputerSeeker = Settings.ActiveSettings.seekerOp,

            // if spectating, show Fantom
            LivingPlayerIsFantom = Settings.ActiveSettings.seekerOp || !Settings.ActiveSettings.fantomOp,
            LivingPlayerIsSeekers = !Settings.ActiveSettings.seekerOp
        };
    }
    private FantomGameSettings FSettingsFromGameSettings(bool order)
    {
        return new()
        {
            DetectivesCount = Settings.ActiveSettings.seekerCount - Settings.ActiveSettings.bobbyCount,
            BobbiesCount = Settings.ActiveSettings.bobbyCount,

            MaxRounds = 22,
            MaxMoves = 24,

            DetectiveStartingTickets =
                    [
                        new TicketGroup(TicketKinds.Mode1, 11 ),
                            new TicketGroup(TicketKinds.Mode2, 8),
                            new TicketGroup(TicketKinds.Mode3, 4),
                    ],
            FantomStartingTickets =
                    [
                        new TicketGroup(TicketKinds.Mode1,  2),         // 4
                            new TicketGroup(TicketKinds.Mode2,  5),     // 3
                            new TicketGroup(TicketKinds.Mode3,  3),     // 3
                            new TicketGroup(TicketKinds.Black,  5),
                            new TicketGroup(TicketKinds.Double, 2),
                    ],
            SupplyTickets = [],

            // TODO
            // TicketNames = [new TicketKindAlias(TicketKinds.Mode1, "Taxi")],

            SeekerStartingPositions = [13, 26, 29, 34, 50, 53, 91, 94, 103, 112, 117, 123, 138, 141, 155, 174],
            // DetectiveStartingPositions = [156, 180, 198, 159, 188],
            FantomStartingPositions = [35, 45, 51, 71, 78, 104, 106, 127, 132, 146, 166, 170, 172],
            //FantomStartingPositions = [185],

            FantomFirstMoveStatic = false,

            // NOTE: now only support for true, but should be false
            SeekerOrder = order            
        };
    }


    public MainView()
    {
        InitializeComponent();

        // load board tile coordinates for the board background image
        using (var r = new StreamReader("./BoardTileCoordinates.txt"))
        {
            for (int i = 1; i <= 199; ++i)
            {
                var line = r.ReadLine();

                var tokens = line?.Split(":");

                if (tokens == null || tokens.Length < 0)
                    break;

                var tile = int.Parse(tokens[0].Trim());
                tokens = tokens[1].Split(",");

                var y = int.Parse(tokens[0].Trim());
                var x = int.Parse(tokens[1].Trim());

                _boardTilesOffsets.Add(tile, (x, y));
                _offsetsToBoardTiles.Add((x, y), tile);
            }

            // For invalid tile
            _boardTilesOffsets.Add(-1, (0, 0));
        }

        UpdateBoard();

        _seekers = new() {
            { 0, Seeker0Piece },
            { 1, Seeker1Piece },
            { 2, Seeker2Piece },
            { 3, Seeker3Piece },
            { 4, Seeker4Piece },
        };

        _bobbies = new()
        {
            { 0, BobbyMod0 },
            { 1, BobbyMod1 },
            { 2, BobbyMod2 },
            { 3, BobbyMod3 },
            { 4, BobbyMod4 },
        };

        _tickets = new()
        {
            { TicketKinds.Mode1, new Bitmap(AssetLoader.Open(new Uri("avares://AvaloniaFantomGamesFacade/Assets/Ticket0.png"))) },
            { TicketKinds.Mode2, new Bitmap(AssetLoader.Open(new Uri("avares://AvaloniaFantomGamesFacade/Assets/Ticket1.png"))) },
            { TicketKinds.Mode3, new Bitmap(AssetLoader.Open(new Uri("avares://AvaloniaFantomGamesFacade/Assets/Ticket2.png"))) },
            { TicketKinds.River, new Bitmap(AssetLoader.Open(new Uri("avares://AvaloniaFantomGamesFacade/Assets/Ticket3.png"))) },
            { TicketKinds.Black, new Bitmap(AssetLoader.Open(new Uri("avares://AvaloniaFantomGamesFacade/Assets/Ticket4.png"))) },
            { TicketKinds.Double, new Bitmap(AssetLoader.Open(new Uri("avares://AvaloniaFantomGamesFacade/Assets/TicketDouble.png"))) },
        };

        _passTicket = new Bitmap(AssetLoader.Open(new Uri("avares://AvaloniaFantomGamesFacade/Assets/TicketPass.png")));

        _fantomTicketDisplay = FantomTickets;

        _ticketDisplays = new()
        {
            {1, Seeker0Tickets},
            {2, Seeker1Tickets},
            {3, Seeker2Tickets},
            {4, Seeker3Tickets},
            {5, Seeker4Tickets},

        };

        TravelTicketSelect.AttachCallback(TicketModeSelected);

        Settings.OnApply = SettingsChanged;
        Settings.OnExit = ExitApp;

        // TODO: logger only when arg maybe
        // logger = new StreamWriter($"../Logs/{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.txt");
        Commander = FantomIntermediary.CreateIntermediary(
            this,
            // Initial settings have logger and game board
            // The initial Intermediary settings
            ISettingsFromGameSettings(),
            // The initial Fantom Game settings
            new()
            {
                DetectivesCount = Settings.ActiveSettings.seekerCount - Settings.ActiveSettings.bobbyCount,
                BobbiesCount = Settings.ActiveSettings.bobbyCount,

                MaxRounds = 22,
                MaxMoves = 24,

                DetectiveStartingTickets =
                    [
                            new TicketGroup(TicketKinds.Mode1, 11),
                            new TicketGroup(TicketKinds.Mode2, 8),
                            new TicketGroup(TicketKinds.Mode3, 4),
                    ],
                FantomStartingTickets =
                    [
                            new TicketGroup(TicketKinds.Mode1,  2),
                            new TicketGroup(TicketKinds.Mode2,  5),
                            new TicketGroup(TicketKinds.Mode3,  3),
                            new TicketGroup(TicketKinds.Black,  5),
                            new TicketGroup(TicketKinds.Double, 2),
                    ],
                SupplyTickets = [],

                SeekerStartingPositions = [13, 26, 29, 34, 50, 53, 91, 94, 103, 112, 117, 123, 138, 141, 155, 174],
                FantomStartingPositions = [35, 45, 51, 71, 78, 104, 106, 127, 132, 146, 166, 170, 172],

                FantomFirstMoveStatic = false,

                SeekerOrder = false,


                // LoggerDestination = logger,
                GameBoardLoader = new IntermediaryFantomBoardLoader("./GameBoard.txt")
                
            }
        );

        FantomLast.IsVisible = false;

    }


    // TODO: add all things for resetting when passing between menus etc.
    public void Reset()
    {
        command_selected = null;
        OnUiThread(() =>
        {
            FantomTicketHistory.Children.Clear();
            GameOverStatus.Text = "";
            doublesUsed = 0;
            
            FantomLast[Canvas.TopProperty] = 0;
            FantomLast[Canvas.LeftProperty] = -60;
        });
    }

    private (int, int) GetTilePosition(int tileIndex)
    {
        (int x, int y) = _boardTilesOffsets[tileIndex];

        // Returns coordinates for the center of the tile
        return (
            x * TILE_SIZE + (x + y % 2) * ODD_TILE_OFFSET + BOARD_OFFSET_X,
            y * TILE_SIZE + ((x + y + 1) % 2) * ODD_TILE_OFFSET + BOARD_OFFSET_Y
            );
    }

    private int? GetTileIndexForPositions(int cx, int cy)
    {
        // Offset the mouse click to be aligned with the board tiles and centered
        var gx = (cx - BOARD_OFFSET_X + TILE_SIZE / 2) / (TILE_SIZE + 5);
        var gy = (cy - BOARD_OFFSET_Y + TILE_SIZE / 2) / TILE_SIZE;

        if (_offsetsToBoardTiles.TryGetValue((gx, gy), out int tileIndex))
        {

            // Confirm click on the tile circle area
            var (dx, dy) = GetTilePosition(tileIndex);

            if ((cx - dx) * (cx - dx) + (cy - dy) * (cy - dy) < TILE_RADIUS * TILE_RADIUS)
            {
                return tileIndex;
            }
        }


        return null;
    }


    public void ResetView()
    {
        _offset_x = 0;
        _offset_y = 0;

        UpdateBoard();
    }

    // Track right mouse button pressed movement for Translating the board
    Point right_pressed;
    private void PointerPressedHandler(object sender, PointerPressedEventArgs args)
    {
        var point = args.GetCurrentPoint(sender as Control);

        if (point.Properties.IsRightButtonPressed)
        {
            // Dragging the board via mouse2
            right_pressed = point.Position;
        }


    }


    // ------------------------------------------------------------------------
    // Movement of pieces

    int? command_selected = null;
    int? seeker_selected = null;

    private void BoardMousePressed(object? sender, PointerPressedEventArgs args)
    {
        var point = args.GetCurrentPoint(sender as Control);

        if (point.Properties.IsLeftButtonPressed)
        {
            var tileClicked = GetTileIndexForPositions((int)point.Position.X, (int)point.Position.Y);

            // Process the tile click
            if (tileClicked is not null)
            {
                // NOTE: commander is always set on start up
                Debug.Assert(Commander is not null);

                // Fantom is playing and the player is in control
                if (!Settings.ActiveSettings.fantomOp && Commander.IsFantomTurn())
                {
                    // 1) have to click on the Board to show the ticket select
                    //   1.1) if click again, without selecting first, hide the ticket select
                    if (command_selected is null)
                    {
                        if (!TravelTicketSelect.IsVisible)
                            TravelTicketSelect.FantomMode();
                        else
                            TravelTicketSelect.Hide();
                    }

                    // 2) have to select a ticket mode (using the TravelTicketSelect)

                    // 3) have to select a tile to move to using that ticket mode
                    else
                    {
                        // NOTE: might do double moves
                        if (Commander.MoveFantom(tileClicked.Value, (TicketKinds)command_selected))
                        {
                            // Try to end the turn
                            if (Commander.ConfirmFantomTurnOver())
                            {
                                command_selected = null;
                            }
                        }
                    }
                }
                // Seekers are playing and the player is in control
                else if (!Settings.ActiveSettings.seekerOp && Commander.IsSeekersTurn())
                {
                    // 1) If click on a Seeker, select it
                    //   1.1) If no command selected and no seeker, then hide

                    int? new_seeker_selection = null;
                    // Find if a seeker was clicked
                    for (int seekerIndex = 0; seekerIndex < _seekerPositions.Length; ++seekerIndex)
                    {
                        if (_seekerPositions[seekerIndex] == tileClicked.Value)
                        {
                            new_seeker_selection = seekerIndex;
                            break;
                        }
                    }

                    if (new_seeker_selection != null)
                    {
                        seeker_selected = new_seeker_selection;
                        TravelTicketSelect.SeekerMode(seeker_selected.Value);
                    }

                    if (command_selected is null && new_seeker_selection is null)
                    {
                        // None selected 
                        TravelTicketSelect.Hide();
                    }

                    // 2) have to select a ticket mode (using the TravelTicketSelect)

                    // 3) have to select a tile to move to using that ticket mode
                    else if (new_seeker_selection is null && command_selected is not null && seeker_selected is not null)
                    {
                        // NOTE: might do double moves
                        if (Commander.MoveSeeker(seeker_selected.Value, tileClicked.Value, (TicketKinds)command_selected))
                        {
                            // Try to end the turn
                            if (Commander.ConfirmSeekersTurnOver())
                            {
                                command_selected = null;
                            }
                            seeker_selected = null;
                        }
                    }
                }
            }
        }
    }

    private bool TicketModeSelected(int command)
    {
        Debug.Assert(Commander is not null);

        // Mode 1, 2, 3 or Black
        if (command < 4)
        {
            // Abort
            if (command_selected == command)
            {
                command_selected = null;
                seeker_selected = null;
                TravelTicketSelect.Hide();
                return false;
            }
            else
            {
                command_selected = command;
                return true;
            }
        }
        // Double and Pass
        else
        {
            // Playing as the Fantom
            if (!Settings.ActiveSettings.fantomOp && Commander.IsFantomTurn())
            {
                // Double
                if (command == 4)
                {
                    Commander.UseDouble();
                }
                // Pass
                else
                {
                    if (Commander.CannotMoveFantom() == null)
                    {
                        if (Commander.ConfirmFantomTurnOver())
                        {
                            command_selected = null;
                        }
                    }
                }
            }
            
            // Playing as the Seekers
            if (!Settings.ActiveSettings.seekerOp && Commander.IsSeekersTurn())
            {
                if(command == 5)
                {
                    if (Commander.CannotMoveSeeker() == null)
                    {
                        if (Commander.ConfirmSeekersTurnOver())
                        {
                            command_selected = null;
                            seeker_selected = null;
                        }
                    }
                }
            }
        }
        return false;
    }

    // ------------------------------------------------------------------------------------------

    public void OnExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        Commander.Exit();
        // logger.Close();
        Exited = true;
    }

    private void ExitApp()
    {
        Dispatcher.UIThread.Post(() => ParentWindow?.Close());
    }

    private void SettingsChanged(GameSettings newSettings)
    {
        var fantomGameSettings = FSettingsFromGameSettings(newSettings.seekerOp);

        Commander.Restart(fantomGameSettings);

        var newInterSettings = ISettingsFromGameSettings();

        Commander.ChangeSettings(newInterSettings);

        // if can see Fantom piece -> hide last pos
        OnUiThread(() =>
        {
            // if playing as the Seekers but not both then show last pos
            FantomLast.IsVisible = (newSettings.fantomOp && !newSettings.seekerOp);
        });

        Settings.ToggleVisibility();
    }



    private void PointerReleasedHandler(object sender, PointerReleasedEventArgs args)
    {
        if (right_pressed.X == 0 && right_pressed.Y == 0)
            return;

        var point = args.GetCurrentPoint(sender as Control);

        var offset = point.Position - right_pressed;
        _offset_x = (int)(_scale * (_offset_x / _scale + offset.X));
        _offset_y = (int)(_scale * (_offset_y / _scale + offset.Y));

        CanvasContent[Canvas.LeftProperty] = (int)(_offset_x / _scale);
        CanvasContent[Canvas.TopProperty] = (int)(_offset_y / _scale);

        right_pressed = new();

    }

    private void PointerMovedHandler(object sender, PointerEventArgs args)
    {
        var point = args.GetCurrentPoint(sender as Control);

        if (point.Properties.IsRightButtonPressed)
        {
            var offset = point.Position - right_pressed;

            CanvasContent[Canvas.LeftProperty] = (int)(_offset_x / _scale + offset.X);
            CanvasContent[Canvas.TopProperty] = (int)(_offset_y / _scale + offset.Y);
        }
    }

    private void PointerWheelChangedHandler(object sender, PointerWheelEventArgs args)
    {
        // based on wheel direction -> zoom in or out
        int dir = args.Delta.Y > 0 ? 1 : args.Delta.Y < 0 ? -1 : 0;

        var oldScale = _scale;

        // manipulate the scale as int (kind-of)
        _scale = ((_scale * 100) + dir * 10) / 100f;
        _scale = (Math.Max(Math.Min(_scale, MAX_SCALE), MIN_SCALE));

        // change offset to keep the same position over mouse pointer location 
        if (_scale != oldScale)
        {
            var point = args.GetCurrentPoint(sender as Control);
            var kx = point.Position.X - (int)(_offset_x / oldScale);
            var ky = point.Position.Y - (int)(_offset_y / oldScale);
            kx /= oldScale;
            ky /= oldScale;
            _offset_x = (int)(_scale * (point.Position.X - _scale * kx));
            _offset_y = (int)(_scale * (point.Position.Y - _scale * ky));
        }

        if (dir != 0) UpdateBoard();

    }

    private void ResetButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Commander?.Reset();
    }

    private void MenuButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Settings.ToggleVisibility();
    }


    private void UpdateBoard()
    {
        CanvasContent[RenderTransformProperty] = new MatrixTransform(Matrix.CreateScale(_scale, _scale));

        CanvasContent[Canvas.LeftProperty] = (int)(_offset_x / _scale);
        CanvasContent[Canvas.TopProperty] = (int)(_offset_y / _scale);
    }

    private void MovePiece(Control control, int tileIndex)
    {
        var (posx, posy) = GetTilePosition(tileIndex);

        posx -= PIECE_SIZE;
        posy -= PIECE_SIZE;

        control[Canvas.LeftProperty] = posx;
        control[Canvas.TopProperty] = posy;
    }


    int doublesUsed = 0;
    private void PlayFantomTicket(Bitmap b, bool wasDouble)
    {
        var im = new Image();

        im[Image.SourceProperty] = b;

        // make double tickets be overlaid by the next ticket
        var c = FantomTicketHistory.Children.Count - doublesUsed;

        if (wasDouble)
            doublesUsed++;

        int x = c / 8;
        int y = c % 8;

        im[Grid.ColumnProperty] = x;
        im[Grid.RowProperty] = y;
        im[MarginProperty] = new Thickness(55, 0, 0, 0);
        FantomTicketHistory.Children.Add(im);
    }



    private Dictionary<int, Image> _seekers;
    private Dictionary<int, Image> _bobbies;

    private FantomDisplay _fantomTicketDisplay;
    private Dictionary<int, PlayerDisplay> _ticketDisplays;

    private Dictionary<TicketKinds, Bitmap> _tickets;
    private Bitmap _passTicket;

    // Place the pieces at positions specified by the game state
    private void UpdateWithState(FantomGameState state)
    {
        // Position playing Seekers
        for (int index = 0; index < state.SeekersCount; ++index)
        {
            MovePiece(_seekers[index], state.GetSeekerPosition(index));
        }

        // Position Bobbies if any 
        for (int index = 0; index < state.BobbiesCount; ++index)
        {
            MovePiece(_bobbies[index], state.GetSeekerPosition(state.DetectivesCount + index));
        }

        // Position Fantom if known / last known
        if (state.FantomPosition > 0)
            MovePiece(FantomPiece, state.FantomPosition);
        // Remove Fantom
        else
        {
            FantomPiece[Canvas.LeftProperty] = -60;
            FantomPiece[Canvas.TopProperty] = 0;
        }

        // Print out Tickets
        for (int index = 0; index < state.DetectivesCount; ++index)
        {
            Console.WriteLine($"Detective {index} has " +
                $"{state.GetDetectiveTickets(index, TicketKinds.Mode1)} Mode 1, " +
                $"{state.GetDetectiveTickets(index, TicketKinds.Mode2)} Mode 2 and " +
                $"{state.GetDetectiveTickets(index, TicketKinds.Mode3)} Mode 3 Tickets.");
        }

        Console.WriteLine($"Fantom has " +
                $"{state.GetFantomTickets(TicketKinds.Mode1)} Mode 1, " +
                $"{state.GetFantomTickets(TicketKinds.Mode2)} Mode 2, " +
                $"{state.GetFantomTickets(TicketKinds.Mode3)} Mode 3, " +
                $"{state.GetFantomTickets(TicketKinds.River)} River, " +
                $"{state.GetFantomTickets(TicketKinds.Black)} Black and " +
                $"{state.GetFantomTickets(TicketKinds.Double)} Double Tickets.");

        //Array.Copy(state.ActorTickets, actorTickets, actorTickets.GetLength(0) * actorTickets.GetLength(1));

        RoundDisplay.Text = $"Round 1 / {_maxRounds}";

        state.CopyFantomTicketsTo(_fantomTickets);
        state.CopyDetectiveTicketsTo(_detectiveTickets);

        for (int seekerIndex = 0; seekerIndex < _seekerPositions.Length; ++seekerIndex)
        {
            _seekerPositions[seekerIndex] = state.GetSeekerPosition(seekerIndex);
        }

        UpdateTickets();
    }

    private void UpdateTickets()
    {

        // Fantom Tickets
        for (int kind = 0; kind < _fantomTickets.Length; ++kind)
            _fantomTicketDisplay.SetTicket(kind, _fantomTickets[kind]);

        // Detectives' Tickets
        for (int index = 0; index < _detectiveTickets.GetLength(0); ++index)
        {

            // for non-fantom actors, only show the first 3 kinds
            for (int kind = 0; kind < 3; ++kind)
            {
                _ticketDisplays[1 + index].SetTicket(kind, _detectiveTickets[index, kind]);
            }
        }
        // For Bobbies / non-playing
        for (int index = _detectiveTickets.GetLength(0); index < 5; ++index)
        {
            _ticketDisplays[1 + index].DisableTicketDisplay();
        }
    }

    private void OnUiThread(Action action)
    {
        try
        {
            // Start the job on the ui thread and return immediately.
            Dispatcher.UIThread.Post(() => action.Invoke());

        }
        catch (Exception)
        {
            throw;
        }
    }

    // ------------------------------------------------------------------------------------------------------------

    int _lastFantomTile = -1;
    int _detectivesCount = 0;
    int[] _fantomTickets;
    int[,] _detectiveTickets;

    int[] _seekerPositions;

    uint _maxRounds = 22;

    List<int> _fantomMovesHistory = [];

    public void ErrorMessage(string message)
    {
        Console.Error.WriteLine(message);
    }

    public void FantomCouldNotBeMoved()
    {
        OnUiThread(() => PlayFantomTicket(_passTicket, false));
    }

    public void FantomMovedTo(int tile, TicketKinds via)
    {
        OnUiThread(() => MovePiece(FantomPiece, tile));
        _lastFantomTile = tile;

        OnUiThread(() =>
        {
            FantomLast[Canvas.TopProperty] = 0;
            FantomLast[Canvas.LeftProperty] = -60;
        });
    }

    public void FantomHistoryMoveTo(int tile)
    {
        _fantomMovesHistory.Add(tile);
    }

    public void FantomPlacedAt(int tile)
    {
        OnUiThread(() => MovePiece(FantomPiece, tile));
        _lastFantomTile = tile;

        OnUiThread(() =>
        {
            FantomLast[Canvas.TopProperty] = 0;
            FantomLast[Canvas.LeftProperty] = -60;
        });
    }

    public void FantomRevealedAt(int tile)
    {
        OnUiThread(() => MovePiece(FantomPiece, tile));
        _lastFantomTile = tile;

        OnUiThread(() =>
        {
            FantomLast[Canvas.TopProperty] = 0;
            FantomLast[Canvas.LeftProperty] = -60;
        });
    }

    public void FantomTurnBegin(uint fantomMove)
    {
        OnUiThread(TravelTicketSelect.Hide);
        Console.WriteLine("Fantom playing.");
    }

    public void FantomTurnEnd()
    {
        OnUiThread(TravelTicketSelect.Hide);
        Console.WriteLine("Seekers playing.");
    }

    public void FantomUsedDouble()
    {
        // Already done in used Ticket
        // OnUiThread(() => PlayFantomTicket(_tickets[TicketKinds.Double]));
    }

    public void FantomUsedTicket(TicketKinds ticketKind)
    {
       OnUiThread(() => PlayFantomTicket(_tickets[ticketKind], ticketKind==TicketKinds.Double));

        // Mark as not known any more 
        if (_lastFantomTile != -1)
            OnUiThread(() => MovePiece(FantomLast, _lastFantomTile));

        _fantomTickets[(int)ticketKind]--;
        OnUiThread(UpdateTickets);
    }

    public void GameFinished(FantomGameResult gameResult)
    {
        OnUiThread(() => GameOverStatus.Text =
                gameResult.FantomWon ? "Fantom won" :
                gameResult.SeekersWon ? "Seekers Won" :
                                        "Draw");

        // remove tickets from the Fantom Ticket display and instead replace with Fantom's movement history
        OnUiThread(() =>
        {
            FantomTicketHistory.Children.Clear();
            for (int i = 0; i < _fantomMovesHistory.Count; i++)
            {
                var txt = new TextBlock();
                txt[TextBlock.TextProperty] = $"{_fantomMovesHistory[i]}";
                txt.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
                txt.Foreground = new SolidColorBrush(Colors.Wheat);
                txt.FontFamily = "Bernard MT";
                txt.FontSize = 50;

                // make double tickets be overlaid by the next ticket
                var c = FantomTicketHistory.Children.Count;

                int x = c / 8;
                int y = c % 8;

                txt[Grid.ColumnProperty] = x;
                txt[Grid.RowProperty] = y;
                txt[MarginProperty] = new Thickness(100, 0, 0, 0);

                FantomTicketHistory.Children.Add(txt);
            }
        });


        Console.Write("Game over: ");
        Console.WriteLine(gameResult.FantomWon ? "Fantom won" : gameResult.SeekersWon ? "Seekers Won" : "Draw");
        Console.WriteLine(gameResult.WinningCondition);
    }

    public void GameReset()
    {
        Reset();
    }

    public void GameRestarted(FantomGameSettings newSettings)
    {
        _maxRounds = newSettings.MaxRounds;
        // GameReset();
    }

    public void GameStarted(FantomGameState gameState)
    {
        _detectivesCount = gameState.DetectivesCount;
        _fantomTickets = new int[FantomGameSettings.TICKET_KINDS_COUNT];
        _detectiveTickets = new int[gameState.DetectivesCount, 3];
        _seekerPositions = new int[gameState.SeekersCount];
        _lastFantomTile = -1;
        _fantomMovesHistory.Clear();

        OnUiThread(() =>
        {
            TravelTicketSelect.Hide();
            UpdateWithState(gameState);
            for (int i = 1; i <= _ticketDisplays.Count; i++)
            {
                _ticketDisplays[i].Reset();
            }

            // only show pieces that are in the game
            for(int i= 0; i< FantomGameSettings.MAXIMUM_SEEKERS_COUNT; ++i)
            {
                _seekers[i].IsVisible = i < gameState.SeekersCount;
                // show only the ones needed 
                _bobbies[i].IsVisible = i < gameState.BobbiesCount;
            }

        });
    }

    public void Reload(FantomGameState? gameState)
    {
        if (gameState is not null)
            OnUiThread(() => UpdateWithState(gameState.Value));
    }

    public void RoundOver(uint round)
    {
        OnUiThread(() =>
        { 
            RoundDisplay.Text = $"Round {Math.Min(_maxRounds, round + 1)} / {_maxRounds}";
            for (int i = 1; i <= _ticketDisplays.Count; i++)
            {
                _ticketDisplays[i].Reset();
            }
        });
        Console.WriteLine($"Round {round} over.");
    }

    public void SeekerCouldNotBeMoved(int seekerIndex)
    {
        Console.WriteLine($"Seeker {seekerIndex} could not Move.");
    }

    public void SeekersCouldNotBeMoved()
    {
        Console.WriteLine($"Seekers could not Move.");
    }


    public void SeekerMovedTo(int seekerIndex, int tile, TicketKinds via)
    {
        OnUiThread(() => MovePiece(_seekers[seekerIndex], tile));
        if (seekerIndex >= _detectivesCount)
            OnUiThread(() => {
                MovePiece(_bobbies[seekerIndex - _detectivesCount], tile);
                });
        else
        {
            _fantomTickets[(int)via]++;
            _detectiveTickets[seekerIndex, (int)via]--;
            OnUiThread(UpdateTickets);
        }

        OnUiThread(() => _ticketDisplays[1 + seekerIndex].Played());
        _seekerPositions[seekerIndex] = tile;
    }

    public void SeekerPlacedAt(int seekerIndex, int tile)
    {
        OnUiThread(() => MovePiece(_seekers[seekerIndex], tile));
        if (seekerIndex >= _detectivesCount)
            OnUiThread(() => MovePiece(_bobbies[seekerIndex - _detectivesCount], tile));
    }

    public void SeekerTurnBegin(int seekerIndex)
    {
        // NOTE: when running with opponent he moves so quickly that these return non-sense
        // Console.WriteLine($"Seeker {seekerIndex} playing.");
    }

    public void SeekerTurnEnd(int seekerIndex)
    {
        // see note above ^^ 
        // Console.WriteLine($"Seeker {seekerIndex} done.");
    }

    public void ShowInfo(Info status)
    {
        Console.Write("Currently playing: ");
        Console.WriteLine(status.IsFantomTurn ? "Fantom." : $"Seeker {status.SeekerIndex}.");
    }


}
