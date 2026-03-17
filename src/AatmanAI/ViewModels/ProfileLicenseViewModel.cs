using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AatmanAI.ViewModels;

public partial class ProfileLicenseViewModel : BaseViewModel
{
    [ObservableProperty] private string _membershipTier = "MASTER KEY MEMBER";
    [ObservableProperty] private string _statusText = "Premium Status Active";
    [ObservableProperty] private string _deviceIdentity = "A7X-99-Q2";
    [ObservableProperty] private string _modelVersion = "Titan v4.2";
    [ObservableProperty] private string _lastSync = "Today, 08:42 AM";

    [RelayCommand]
    private async Task RestoreLicensesAsync()
    {
        if (!SetBusyAndCheck()) return;
        try
        {
            await Task.Delay(1500); // Simulate restore
        }
        finally
        {
            IsBusy = false;
        }
    }
}
