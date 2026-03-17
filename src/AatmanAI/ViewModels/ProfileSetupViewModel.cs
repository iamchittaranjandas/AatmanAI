using AatmanAI.Core.Constants;
using AatmanAI.Data.Database;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AatmanAI.ViewModels;

public partial class ProfileSetupViewModel : BaseViewModel
{
    private readonly AppDatabase _db;

    public ProfileSetupViewModel(AppDatabase db)
    {
        _db = db;
    }

    [ObservableProperty] private string? _fullName;
    [ObservableProperty] private string? _lifeGoal;
    [ObservableProperty] private double _privacyLevel = 0.8;
    [ObservableProperty] private string _privacyLevelText = "Maximum Fortress";

    // Interest chips
    [ObservableProperty] private bool _mindfulnessSelected;
    [ObservableProperty] private bool _deepTechSelected;
    [ObservableProperty] private bool _philosophySelected;
    [ObservableProperty] private bool _artHistorySelected;
    [ObservableProperty] private bool _offlineAiSelected;
    [ObservableProperty] private bool _financeSelected;
    [ObservableProperty] private bool _biotechSelected;

    partial void OnPrivacyLevelChanged(double value)
    {
        PrivacyLevelText = value switch
        {
            >= 0.8 => "Maximum Fortress",
            >= 0.5 => "High Security",
            >= 0.3 => "Balanced",
            _ => "Essential"
        };
    }

    [RelayCommand]
    private async Task SealProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName)) return;

        // Persist profile to local DB
        await _db.SetSettingAsync(AppConstants.KeyProfileName, FullName.Trim());
        await _db.SetSettingAsync(AppConstants.KeyProfileLifeGoal, LifeGoal?.Trim() ?? string.Empty);

        var interests = new List<string>();
        if (MindfulnessSelected) interests.Add("Mindfulness");
        if (DeepTechSelected) interests.Add("Deep Tech");
        if (PhilosophySelected) interests.Add("Philosophy");
        if (ArtHistorySelected) interests.Add("Art History");
        if (OfflineAiSelected) interests.Add("Offline AI");
        if (FinanceSelected) interests.Add("Finance");
        if (BiotechSelected) interests.Add("Biotech");
        await _db.SetSettingAsync(AppConstants.KeyProfileInterests, string.Join(", ", interests));

        await Shell.Current.GoToAsync("//main/home");
    }
}
