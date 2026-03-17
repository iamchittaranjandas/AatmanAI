using CommunityToolkit.Mvvm.ComponentModel;

namespace AatmanAI.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _errorMessage;

    protected bool SetBusyAndCheck()
    {
        if (IsBusy) return false;
        IsBusy = true;
        ErrorMessage = null;
        return true;
    }
}
