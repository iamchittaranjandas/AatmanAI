using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AatmanAI.ViewModels;

[QueryProperty(nameof(ModelName), "modelName")]
[QueryProperty(nameof(ModelDescription), "modelDescription")]
[QueryProperty(nameof(ModelParameters), "modelParameters")]
[QueryProperty(nameof(ModelPrice), "modelPrice")]
public partial class ModelDetailViewModel : BaseViewModel
{
    [ObservableProperty] private string _modelName = "Power Ghost V2";
    [ObservableProperty] private string _modelDescription = "Optimized for complex reasoning and creative tasks directly on-device. Zero data latency.";
    [ObservableProperty] private string _modelParameters = "7 Billion";
    [ObservableProperty] private string _contextWindow = "32k Tks";
    [ObservableProperty] private string _modelPrice = "$14.99";

    [RelayCommand]
    private async Task UnlockIntelligenceAsync()
    {
        if (!SetBusyAndCheck()) return;
        try
        {
            // Stub: purchase flow
            await Task.Delay(1000);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task WatchAdAsync()
    {
        // Stub: rewarded ad
        await Task.Delay(500);
    }
}
