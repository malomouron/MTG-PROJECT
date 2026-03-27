namespace MtgClient.Helpers;

public interface INavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
    void NavigateTo<TViewModel>(Action<TViewModel> configure) where TViewModel : ObservableObject;
}

public class NavigationService : ObservableObject, INavigationService
{
    private readonly Func<Type, ObservableObject> _viewModelFactory;
    private ObservableObject? _currentView;

    public ObservableObject? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public NavigationService(Func<Type, ObservableObject> viewModelFactory)
    {
        _viewModelFactory = viewModelFactory;
    }

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        CurrentView = _viewModelFactory(typeof(TViewModel));
    }

    public void NavigateTo<TViewModel>(Action<TViewModel> configure) where TViewModel : ObservableObject
    {
        TViewModel vm = (TViewModel)_viewModelFactory(typeof(TViewModel));
        configure(vm);
        CurrentView = vm;
    }
}
