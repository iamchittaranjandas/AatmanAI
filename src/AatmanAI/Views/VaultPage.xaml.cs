using AatmanAI.ViewModels;

namespace AatmanAI.Views;

public partial class VaultPage : ContentPage
{
    private readonly VaultViewModel _vm;

    public VaultPage(VaultViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }
}
