using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AatmanAI.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _password;
    [ObservableProperty] private bool _isPasswordVisible;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password)) return;
        if (!SetBusyAndCheck()) return;

        try
        {
            // For now, skip auth and go to main
            await Shell.Current.GoToAsync("//main");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateAccountAsync()
    {
        await Shell.Current.GoToAsync("profilesetup");
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private async Task FaceIdLoginAsync()
    {
        // Stub: biometric auth
        await Shell.Current.GoToAsync("//main");
    }
}
