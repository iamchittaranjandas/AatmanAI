using AatmanAI.ViewModels;

namespace AatmanAI.Views;

public partial class ProfileSetupPage : ContentPage
{
    public ProfileSetupPage(ProfileSetupViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
