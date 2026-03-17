using AatmanAI.ViewModels;

namespace AatmanAI.Views;

public partial class MarketplacePage : ContentPage
{
    private readonly MarketplaceViewModel _vm;

    public MarketplacePage(MarketplaceViewModel vm)
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
