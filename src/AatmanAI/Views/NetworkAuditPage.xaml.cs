using AatmanAI.ViewModels;

namespace AatmanAI.Views;

public partial class NetworkAuditPage : ContentPage
{
    private readonly NetworkAuditViewModel _vm;

    public NetworkAuditPage(NetworkAuditViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }

    private async void OnBackTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
