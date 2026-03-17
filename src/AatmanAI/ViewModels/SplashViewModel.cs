using AatmanAI.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AatmanAI.ViewModels;

public partial class SplashViewModel : BaseViewModel
{
    private readonly IModelService _modelService;

    [ObservableProperty] private string _statusText = "Loading...";

    public SplashViewModel(IModelService modelService)
    {
        _modelService = modelService;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        StatusText = "Checking models...";
        var isFirstLaunch = await _modelService.IsFirstLaunchAsync();

        if (isFirstLaunch)
            await Shell.Current.GoToAsync("//firstlaunch");
        else
            await Shell.Current.GoToAsync("//main/home");
    }
}
