using System.Collections.ObjectModel;
using System.Windows;
using MtgClient.Helpers;
using MtgClient.Models;
using MtgClient.Services;
using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Protocol;

namespace MtgClient.ViewModels;

/// <summary>
/// US-007/008/009/010/011/013/014 — Main game board
/// </summary>
public sealed class GameViewModel : ObservableObject
{
    private readonly ServerConnection _connection;
    private readonly NavigationService _navigation;

    // Game state
    private string _gameId = string.Empty;
    private Phase _currentPhase;
    private int _turnNumber;
    private string _activePlayerId = string.Empty;
    private GameStatus _gameStatus;
    private string _statusMessage = string.Empty;
    private string _errorMessage = string.Empty;

    // Current player state
    private int _life;
    private int _libraryCount;
    private string _manaDisplay = string.Empty;
    private bool _isMyTurn;
    private bool _canPlayCard;
    private bool _canDeclareAttackers;
    private bool _canDeclareBlockers;

    // Selection
    private HandCardDto? _selectedHandCard;
    private PermanentDto? _selectedBattlefieldCard;
    private string? _selectedTargetId;

    // Chat (US-014)
    private string _chatInput = string.Empty;

    #region Properties

    public string GameId { get => _gameId; set => SetProperty(ref _gameId, value); }
    public Phase CurrentPhase { get => _currentPhase; set => SetProperty(ref _currentPhase, value); }
    public int TurnNumber { get => _turnNumber; set => SetProperty(ref _turnNumber, value); }
    public string ActivePlayerId { get => _activePlayerId; set => SetProperty(ref _activePlayerId, value); }
    public GameStatus GameStatus { get => _gameStatus; set => SetProperty(ref _gameStatus, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
    public int Life { get => _life; set => SetProperty(ref _life, value); }
    public int LibraryCount { get => _libraryCount; set => SetProperty(ref _libraryCount, value); }
    public string ManaDisplay { get => _manaDisplay; set => SetProperty(ref _manaDisplay, value); }
    public bool IsMyTurn { get => _isMyTurn; set => SetProperty(ref _isMyTurn, value); }
    public bool CanPlayCard { get => _canPlayCard; set => SetProperty(ref _canPlayCard, value); }
    public bool CanDeclareAttackers { get => _canDeclareAttackers; set => SetProperty(ref _canDeclareAttackers, value); }
    public bool CanDeclareBlockers { get => _canDeclareBlockers; set => SetProperty(ref _canDeclareBlockers, value); }

    public HandCardDto? SelectedHandCard
    {
        get => _selectedHandCard;
        set
        {
            SetProperty(ref _selectedHandCard, value);
            UpdateCanPlayCard();
        }
    }

    public PermanentDto? SelectedBattlefieldCard
    {
        get => _selectedBattlefieldCard;
        set => SetProperty(ref _selectedBattlefieldCard, value);
    }

    public string? SelectedTargetId
    {
        get => _selectedTargetId;
        set => SetProperty(ref _selectedTargetId, value);
    }

    public string ChatInput
    {
        get => _chatInput;
        set => SetProperty(ref _chatInput, value);
    }

    #endregion

    #region Collections

    public ObservableCollection<HandCardDto> Hand { get; } = [];
    public ObservableCollection<PermanentDto> MyBattlefield { get; } = [];
    public ObservableCollection<CardDto> MyGraveyard { get; } = [];
    public ObservableCollection<CardDto> MyCommandZone { get; } = [];
    public ObservableCollection<OpponentViewModel> Opponents { get; } = [];
    public ObservableCollection<StackItemDto> Stack { get; } = [];
    public ObservableCollection<ChatMessage> ChatMessages { get; } = [];

    // Combat selections
    public ObservableCollection<string> SelectedAttackerIds { get; } = [];
    public ObservableCollection<string> SelectedBlockerMappings { get; } = [];

    #endregion

    #region Commands

    public AsyncRelayCommand PlayCardCommand { get; }
    public AsyncRelayCommand PassPriorityCommand { get; }
    public AsyncRelayCommand DeclareAttackersCommand { get; }
    public AsyncRelayCommand DeclareBlockersCommand { get; }
    public AsyncRelayCommand ActivateAbilityCommand { get; }
    public AsyncRelayCommand StartGameCommand { get; }
    public AsyncRelayCommand SendChatCommand { get; }
    public RelayCommand SelectTargetCommand { get; }

    #endregion

    public string? MyPlayerId { get; set; }

    public GameViewModel(ServerConnection connection, NavigationService navigation)
    {
        _connection = connection;
        _navigation = navigation;

        // Commands
        PlayCardCommand = new AsyncRelayCommand(PlayCardAsync, () => CanPlayCard && SelectedHandCard != null);
        PassPriorityCommand = new AsyncRelayCommand(PassPriorityAsync);
        DeclareAttackersCommand = new AsyncRelayCommand(DeclareAttackersAsync, () => CanDeclareAttackers);
        DeclareBlockersCommand = new AsyncRelayCommand(DeclareBlockersAsync, () => CanDeclareBlockers);
        ActivateAbilityCommand = new AsyncRelayCommand(ActivateAbilityAsync, () => SelectedBattlefieldCard != null);
        StartGameCommand = new AsyncRelayCommand(StartGameAsync);
        SendChatCommand = new AsyncRelayCommand(SendChatAsync, () => !string.IsNullOrWhiteSpace(ChatInput));
        SelectTargetCommand = new RelayCommand(p => SelectedTargetId = p as string);

        // Subscribe to server events
        _connection.GameStateReceived += OnGameStateReceived;
        _connection.PrivateStateReceived += OnPrivateStateReceived;
        _connection.GameEventReceived += OnGameEvent;
        _connection.ErrorReceived += err => Application.Current.Dispatcher.Invoke(() =>
            ErrorMessage = err.Message);
        _connection.ChatReceived += OnChatReceived;
    }

    public void Initialize(GameStateDto initialState)
    {
        GameId = initialState.GameId;
        UpdateFromGameState(initialState);
    }

    #region Server message handlers

    private void OnGameStateReceived(GameStateMessage msg)
    {
        Application.Current.Dispatcher.Invoke(() => UpdateFromGameState(msg.State));
    }

    private void OnPrivateStateReceived(PrivatePlayerStateMessage msg)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Hand.Clear();
            foreach (var card in msg.Hand)
                Hand.Add(card);
        });
    }

    private void OnGameEvent(GameEventMessage msg)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (msg.EventType == "game_ended")
            {
                var winnerId = msg.Data.GetValueOrDefault("winnerId")?.ToString();
                _navigation.NavigateTo<GameEndViewModel>(vm =>
                {
                    vm.WinnerId = winnerId ?? "unknown";
                    vm.IsWin = winnerId == MyPlayerId;
                });
            }
        });
    }

    private void OnChatReceived(ChatMessageServer msg)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ChatMessages.Add(new ChatMessage
            {
                Sender = msg.Sender,
                Text = msg.Text
            });
        });
    }

    #endregion

    #region State update

    private void UpdateFromGameState(GameStateDto state)
    {
        GameId = state.GameId;
        CurrentPhase = state.CurrentPhase;
        TurnNumber = state.TurnNumber;
        ActivePlayerId = state.ActivePlayerId;
        GameStatus = state.Status;
        IsMyTurn = state.ActivePlayerId == MyPlayerId;

        // Status message
        StatusMessage = GameStatus == GameStatus.WaitingForPlayers
            ? $"En attente de joueurs ({state.Players.Count} connectés)"
            : $"Tour {TurnNumber} — Phase : {CurrentPhase} — {(IsMyTurn ? "Votre tour" : $"Tour de {ActivePlayerId}")}";

        // Update my state
        var me = state.Players.FirstOrDefault(p => p.PlayerId == MyPlayerId);
        if (me != null)
        {
            Life = me.Life;
            LibraryCount = me.LibraryCount;
            ManaDisplay = FormatManaPool(me.ManaPool);

            MyBattlefield.Clear();
            foreach (var perm in me.Battlefield) MyBattlefield.Add(perm);

            MyGraveyard.Clear();
            foreach (var card in me.Graveyard) MyGraveyard.Add(card);

            MyCommandZone.Clear();
            foreach (var card in me.CommandZone) MyCommandZone.Add(card);
        }

        // Update opponents
        Opponents.Clear();
        foreach (var player in state.Players.Where(p => p.PlayerId != MyPlayerId))
        {
            Opponents.Add(new OpponentViewModel
            {
                PlayerId = player.PlayerId,
                PlayerName = player.PlayerName,
                Life = player.Life,
                HandCount = player.HandCount,
                LibraryCount = player.LibraryCount,
                IsEliminated = player.IsEliminated,
                Battlefield = new ObservableCollection<PermanentDto>(player.Battlefield),
                GraveyardCount = player.Graveyard.Count,
                ManaDisplay = FormatManaPool(player.ManaPool)
            });
        }

        // Update stack
        Stack.Clear();
        foreach (var item in state.Stack) Stack.Add(item);

        // Update action availability
        CanDeclareAttackers = IsMyTurn && CurrentPhase == Phase.DeclareAttackers;
        CanDeclareBlockers = !IsMyTurn && CurrentPhase == Phase.DeclareBlockers;
        UpdateCanPlayCard();
    }

    private void UpdateCanPlayCard()
    {
        CanPlayCard = SelectedHandCard != null && IsMyTurn &&
                      (CurrentPhase == Phase.MainPre || CurrentPhase == Phase.MainPost ||
                       SelectedHandCard.CardType == CardType.Instant);
    }

    private static string FormatManaPool(ManaPoolDto pool)
    {
        var parts = new List<string>();
        if (pool.White > 0) parts.Add($"W:{pool.White}");
        if (pool.Blue > 0) parts.Add($"U:{pool.Blue}");
        if (pool.Black > 0) parts.Add($"B:{pool.Black}");
        if (pool.Red > 0) parts.Add($"R:{pool.Red}");
        if (pool.Green > 0) parts.Add($"G:{pool.Green}");
        if (pool.Colorless > 0) parts.Add($"C:{pool.Colorless}");
        return parts.Count > 0 ? string.Join(" | ", parts) : "Vide";
    }

    #endregion

    #region Commands implementation

    private async Task PlayCardAsync()
    {
        if (SelectedHandCard == null) return;

        await _connection.SendAsync(new PlayCardMessage
        {
            Type = "play_card",
            GameId = GameId,
            CardInstanceId = SelectedHandCard.InstanceId,
            TargetId = SelectedTargetId
        });

        SelectedHandCard = null;
        SelectedTargetId = null;
    }

    private async Task PassPriorityAsync()
    {
        await _connection.SendAsync(new PassPriorityMessage
        {
            Type = "pass_priority",
            GameId = GameId
        });
    }

    private async Task DeclareAttackersAsync()
    {
        if (SelectedAttackerIds.Count == 0) return;

        // Default target: first opponent
        var defaultTarget = Opponents.FirstOrDefault(o => !o.IsEliminated)?.PlayerId;
        if (defaultTarget == null) return;

        var attackers = SelectedAttackerIds.Select(id => new AttackerDeclaration
        {
            CreatureId = id,
            DefendingPlayerId = SelectedTargetId ?? defaultTarget
        }).ToList();

        await _connection.SendAsync(new DeclareAttackersMessage
        {
            Type = "declare_attackers",
            GameId = GameId,
            Attackers = attackers
        });

        SelectedAttackerIds.Clear();
    }

    private async Task DeclareBlockersAsync()
    {
        // Blocker mappings stored as "blockerId:attackerId"
        var blockers = SelectedBlockerMappings
            .Select(m => m.Split(':'))
            .Where(parts => parts.Length == 2)
            .Select(parts => new BlockerDeclaration
            {
                BlockerId = parts[0],
                AttackerId = parts[1]
            }).ToList();

        await _connection.SendAsync(new DeclareBlockersMessage
        {
            Type = "declare_blockers",
            GameId = GameId,
            Blockers = blockers
        });

        SelectedBlockerMappings.Clear();
    }

    private async Task ActivateAbilityAsync()
    {
        if (SelectedBattlefieldCard == null) return;

        await _connection.SendAsync(new ActivateAbilityMessage
        {
            Type = "activate_ability",
            GameId = GameId,
            PermanentId = SelectedBattlefieldCard.InstanceId,
            AbilityIndex = 0,
            TargetId = SelectedTargetId
        });
    }

    private async Task StartGameAsync()
    {
        await _connection.SendAsync(new StartGameMessage
        {
            Type = "start_game",
            GameId = GameId
        });
    }

    private async Task SendChatAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatInput)) return;

        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            type = "chat",
            gameId = GameId,
            text = ChatInput
        });
        await _connection.SendRawAsync(json);

        ChatMessages.Add(new ChatMessage
        {
            Sender = MyPlayerId ?? "Moi",
            Text = ChatInput
        });
        ChatInput = string.Empty;
    }

    #endregion
}

/// <summary>
/// View model for an opponent's visible state.
/// </summary>
public sealed class OpponentViewModel : ObservableObject
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public int Life { get; set; }
    public int HandCount { get; set; }
    public int LibraryCount { get; set; }
    public bool IsEliminated { get; set; }
    public int GraveyardCount { get; set; }
    public string ManaDisplay { get; set; } = string.Empty;
    public ObservableCollection<PermanentDto> Battlefield { get; set; } = [];
}
