using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AatmanAI.ViewModels;

public partial class SearchViewModel : BaseViewModel
{
    [ObservableProperty] private string? _searchQuery;
    [ObservableProperty] private string _selectedFilter = "All";
    [ObservableProperty] private int _resultCount;

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;
        if (!SetBusyAndCheck()) return;
        try
        {
            await Task.Delay(500); // Simulate search
            ResultCount = 3; // Stub
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SetFilter(string filter)
    {
        SelectedFilter = filter;
    }
}
