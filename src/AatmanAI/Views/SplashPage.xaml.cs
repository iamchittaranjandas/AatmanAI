using AatmanAI.ViewModels;

namespace AatmanAI.Views;

public partial class SplashPage : ContentPage
{
    public SplashPage(SplashViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Delay(500);
        if (BindingContext is SplashViewModel vm)
            await vm.InitializeCommand.ExecuteAsync(null);
    }
}
