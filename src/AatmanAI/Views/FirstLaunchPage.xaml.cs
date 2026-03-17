using AatmanAI.ViewModels;

namespace AatmanAI.Views;

public partial class FirstLaunchPage : ContentPage
{
    public FirstLaunchPage(FirstLaunchViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
