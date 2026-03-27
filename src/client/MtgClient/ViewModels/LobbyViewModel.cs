using MtgClient.Helpers;
using MtgClient.Services;
using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Protocol;
using System.Collections.ObjectModel;
using System.Windows;

namespace MtgClient.ViewModels;

/// <summary>
/// US-003 / US-004 / US-005 — Lobby, Create game, Join game
/// </summary>
public sealed class LobbyViewModel : ObservableObject
{
    private readonly ServerConnection _connection;
    private readonly NavigationService _navigation;

    private string _newGameName = string.Empty;
    private int _newGameMaxPlayers = 4;
    private GameInfoDto? _selectedGame;
    private string _errorMessage = string.Empty;

    public ObservableCollection<GameInfoDto> Games { get; } = [];

    public string NewGameName
    {
        get => _newGameName;
        set => SetProperty(ref _newGameName, value);
    }

    public int NewGameMaxPlayers
    {
        get => _newGameMaxPlayers;
        set => SetProperty(ref _newGameMaxPlayers, value);
    }

    public GameInfoDto? SelectedGame
    {
        get => _selectedGame;
        set => SetProperty(ref _selectedGame, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public AsyncRelayCommand CreateGameCommand { get; }
    public AsyncRelayCommand JoinGameCommand { get; }
    public RelayCommand ImportDeckCommand { get; }

    public LobbyViewModel(ServerConnection connection, NavigationService navigation)
    {
        _connection = connection;
        _navigation = navigation;

        _connection.LobbyUpdateReceived += OnLobbyUpdate;
        _connection.GameEventReceived += OnGameEvent;
        _connection.ErrorReceived += err => Application.Current.Dispatcher.Invoke(() =>
            ErrorMessage = err.Message);

        CreateGameCommand = new AsyncRelayCommand(CreateGameAsync, () => !string.IsNullOrWhiteSpace(NewGameName));
        JoinGameCommand = new AsyncRelayCommand(JoinGameAsync, () => SelectedGame != null);
        ImportDeckCommand = new RelayCommand(() =>
            _navigation.NavigateTo<DeckImportViewModel>());
    }

    private async Task CreateGameAsync()
    {
        await _connection.SendAsync(new CreateGameMessage
        {
            Type = "create_game",
            GameName = NewGameName,
            MaxPlayers = NewGameMaxPlayers
        });
        NewGameName = string.Empty;
    }

    private async Task JoinGameAsync()
    {
        if (SelectedGame == null) return;

        // Navigate to deck import with the game context
        _navigation.NavigateTo<DeckImportViewModel>(vm => vm.GameIdToJoin = SelectedGame.GameId);
    }

    private void OnLobbyUpdate(LobbyUpdateMessage msg)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Games.Clear();
            foreach (GameInfoDto game in msg.Games)
            {
                if (game.Status == GameStatus.WaitingForPlayers)
                    Games.Add(game);
            }
        });
    }

    private void OnGameEvent(GameEventMessage msg)
    {
        if (msg.EventType == "game_created")
        {
            // Lobby will update via LobbyUpdateMessage
        }
    }
}
