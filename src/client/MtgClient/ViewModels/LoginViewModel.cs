using System.Windows;
using MtgClient.Helpers;
using MtgClient.Services;

namespace MtgClient.ViewModels;

/// <summary>
/// US-001 / US-002 — Login &amp; Registration
/// </summary>
public sealed class LoginViewModel : ObservableObject
{
    private readonly ServerConnection _connection;
    private readonly NavigationService _navigation;

    private string _playerName = string.Empty;
    private string _serverAddress = "ws://localhost:5000/ws";
    private string _errorMessage = string.Empty;
    private bool _isLoading;

    public string PlayerName
    {
        get => _playerName;
        set => SetProperty(ref _playerName, value);
    }

    public string ServerAddress
    {
        get => _serverAddress;
        set => SetProperty(ref _serverAddress, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public AsyncRelayCommand ConnectCommand { get; }

    public LoginViewModel(ServerConnection connection, NavigationService navigation)
    {
        _connection = connection;
        _navigation = navigation;

        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !IsLoading && !string.IsNullOrWhiteSpace(PlayerName));
    }

    private async Task ConnectAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            await _connection.ConnectAsync(ServerAddress, PlayerName);
            Application.Current.Dispatcher.Invoke(() => _navigation.NavigateTo<LobbyViewModel>());
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Connexion échouée : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
