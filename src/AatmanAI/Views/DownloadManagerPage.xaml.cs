using AatmanAI.ViewModels;

namespace AatmanAI.Views;

public partial class DownloadManagerPage : ContentPage
{
    public DownloadManagerPage(DownloadManagerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void OnBackTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
