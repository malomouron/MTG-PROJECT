using MtgClient.Helpers;
using MtgClient.Models;

namespace MtgClient.ViewModels;

/// <summary>
/// US-015 — Game end + statistics
/// </summary>
public sealed class GameEndViewModel : ObservableObject
{
    private readonly NavigationService _navigation;

    private string _winnerId = string.Empty;
    private bool _isWin;
    private string _resultMessage = string.Empty;

    public string WinnerId
    {
        get => _winnerId;
        set
        {
            _ = SetProperty(ref _winnerId, value);
            ResultMessage = IsWin ? "Victoire !" : $"Défaite — Gagnant : {value}";
        }
    }

    public bool IsWin
    {
        get => _isWin;
        set
        {
            _ = SetProperty(ref _isWin, value);
            ResultMessage = value ? "Victoire !" : $"Défaite — Gagnant : {WinnerId}";
        }
    }

    public string ResultMessage
    {
        get => _resultMessage;
        set => SetProperty(ref _resultMessage, value);
    }

    public PlayerStats Stats { get; set; } = new();

    public RelayCommand BackToLobbyCommand { get; }

    public GameEndViewModel(NavigationService navigation)
    {
        _navigation = navigation;
        BackToLobbyCommand = new RelayCommand(() => _navigation.NavigateTo<LobbyViewModel>());
    }
}
