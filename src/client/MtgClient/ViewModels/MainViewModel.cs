using MtgClient.Helpers;
using MtgClient.Services;
using System.Windows;

namespace MtgClient.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly NavigationService _navigation;
    private readonly ServerConnection _connection;
    private string _statusText = "Déconnecté";
    private bool _isConnected;

    public NavigationService Navigation => _navigation;

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public MainViewModel(NavigationService navigation, ServerConnection connection)
    {
        _navigation = navigation;
        _connection = connection;

        _connection.Connected += () => Application.Current.Dispatcher.Invoke(() =>
        {
            IsConnected = true;
            StatusText = "Connecté";
        });

        _connection.Disconnected += reason => Application.Current.Dispatcher.Invoke(() =>
        {
            IsConnected = false;
            StatusText = $"Déconnecté : {reason}";
        });

        _connection.ErrorReceived += error => Application.Current.Dispatcher.Invoke(() =>
        {
            StatusText = $"Erreur : {error.Message}";
        });

        // Start at login
        _navigation.NavigateTo<LoginViewModel>();
    }
}
