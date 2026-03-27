using System;
using System.Windows;
using MtgClient.Helpers;
using MtgClient.Services;
using MtgClient.ViewModels;

namespace MtgClient;

public partial class App : Application
{
    private readonly ServerConnection _connection = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        NavigationService navigation = null!;

        navigation = new NavigationService(type =>
        {
            if (type == typeof(LoginViewModel))
                return new LoginViewModel(_connection, navigation);
            if (type == typeof(LobbyViewModel))
                return new LobbyViewModel(_connection, navigation);
            if (type == typeof(DeckImportViewModel))
                return new DeckImportViewModel(_connection, navigation);
            if (type == typeof(GameViewModel))
                return new GameViewModel(_connection, navigation);
            if (type == typeof(GameEndViewModel))
                return new GameEndViewModel(navigation);
            throw new ArgumentException($"Unknown ViewModel type: {type}");
        });

        var mainViewModel = new MainViewModel(navigation, _connection);

        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };
        mainWindow.Show();
    }
}
