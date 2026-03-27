using MtgClient.Helpers;
using MtgClient.Models;
using MtgClient.Services;
using MtgEngine.Shared.Protocol;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;

namespace MtgClient.ViewModels;

/// <summary>
/// US-006 — Deck import
/// </summary>
public sealed class DeckImportViewModel : ObservableObject
{
    private readonly ServerConnection _connection;
    private readonly NavigationService _navigation;

    private string _deckText = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isValid;
    private int _cardCount;

    public string? GameIdToJoin { get; set; }

    public string DeckText
    {
        get => _deckText;
        set
        {
            if (SetProperty(ref _deckText, value))
                ValidateDeck();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsValid
    {
        get => _isValid;
        set => SetProperty(ref _isValid, value);
    }

    public int CardCount
    {
        get => _cardCount;
        set => SetProperty(ref _cardCount, value);
    }

    public ObservableCollection<string> ValidationErrors { get; } = [];

    public AsyncRelayCommand ConfirmDeckCommand { get; }
    public RelayCommand BackCommand { get; }

    public DeckImportViewModel(ServerConnection connection, NavigationService navigation)
    {
        _connection = connection;
        _navigation = navigation;

        ConfirmDeckCommand = new AsyncRelayCommand(ConfirmDeckAsync, () => IsValid && GameIdToJoin != null);
        BackCommand = new RelayCommand(() => _navigation.NavigateTo<LobbyViewModel>());
    }

    private DeckList? _parsedDeck;

    private void ValidateDeck()
    {
        ValidationErrors.Clear();

        if (string.IsNullOrWhiteSpace(DeckText))
        {
            IsValid = false;
            CardCount = 0;
            return;
        }

        // Use a permissive set — server will reject unknown cards
        HashSet<string> allNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Add basic lands always
        foreach (string? land in new[] { "Mountain", "Forest", "Plains", "Swamp", "Island" })
            _ = allNames.Add(land);

        // Parse all card names from the text to build the set
        string[] lines = DeckText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("//") || string.IsNullOrWhiteSpace(trimmed))
                continue;
            if (trimmed.Equals("COMMANDER", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("DECK", StringComparison.OrdinalIgnoreCase))
                continue;

            Match match = System.Text.RegularExpressions.Regex.Match(trimmed, @"^\s*\d+x?\s+(.+?)\s*$");
            if (match.Success)
                _ = allNames.Add(match.Groups[1].Value);
        }

        DeckImportService service = new DeckImportService(allNames);
        _parsedDeck = service.Parse(DeckText);
        CardCount = _parsedDeck.TotalCards;

        if (_parsedDeck.Errors.Count > 0)
        {
            foreach (string error in _parsedDeck.Errors)
                ValidationErrors.Add(error);
        }

        // Only check structural validity (card count, commander present)
        IsValid = _parsedDeck.CommanderId != null && _parsedDeck.TotalCards == 100;
        ErrorMessage = IsValid ? "Deck valide ✓" : $"{_parsedDeck.Errors.Count} erreur(s)";
    }

    private async Task ConfirmDeckAsync()
    {
        if (_parsedDeck == null || GameIdToJoin == null) return;

        List<string> deckCardIds = new List<string>();
        foreach (DeckEntry entry in _parsedDeck.Entries)
        {
            for (int i = 0; i < entry.Quantity; i++)
                deckCardIds.Add(entry.CardId);
        }

        await _connection.SendAsync(new JoinGameMessage
        {
            Type = "join_game",
            GameId = GameIdToJoin,
            DeckCardIds = deckCardIds,
            CommanderId = _parsedDeck.CommanderId!
        });

        // Listen for game state → navigate to game
        _connection.GameStateReceived += OnFirstGameState;
    }

    private void OnFirstGameState(GameStateMessage msg)
    {
        _connection.GameStateReceived -= OnFirstGameState;
        Application.Current.Dispatcher.Invoke(() =>
            _navigation.NavigateTo<GameViewModel>(vm => vm.Initialize(msg.State)));
    }
}
