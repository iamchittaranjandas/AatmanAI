using AatmanAI.ViewModels;

namespace AatmanAI.Views;

public partial class ModelDetailPage : ContentPage
{
    public ModelDetailPage(ModelDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
