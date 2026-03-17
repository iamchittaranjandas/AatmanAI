using AatmanAI.ViewModels;

namespace AatmanAI.Views;

public partial class StoragePage : ContentPage
{
    private readonly StorageViewModel _vm;

    public StoragePage(StorageViewModel vm)
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
