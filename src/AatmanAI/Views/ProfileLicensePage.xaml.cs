using AatmanAI.ViewModels;

namespace AatmanAI.Views;

public partial class ProfileLicensePage : ContentPage
{
    public ProfileLicensePage(ProfileLicenseViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
