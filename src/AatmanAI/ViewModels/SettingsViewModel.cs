using AatmanAI.Core.Constants;
using AatmanAI.Core.Enums;
using AatmanAI.Data.Database;
using AatmanAI.Data.Models;
using AatmanAI.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AatmanAI.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly AppDatabase _db;
    private readonly IDeviceService _deviceService;
    private readonly IModelService _modelService;
    private readonly IInferenceService _inferenceService;

    [ObservableProperty] private double _temperature = AppConstants.DefaultTemperature;
    [ObservableProperty] private int _maxTokens = AppConstants.DefaultMaxTokens;
    [ObservableProperty] private double _topP = AppConstants.DefaultTopP;
    [ObservableProperty] private int _contextLength = AppConstants.DefaultContextLength;
    [ObservableProperty] private string _powerModeDisplay = "Auto";
    [ObservableProperty] private string _activeModelName = "None";
    [ObservableProperty] private string _deviceTier = "Unknown";
    [ObservableProperty] private string _ramInfo = "Unknown";
    [ObservableProperty] private string _storageInfo = "Unknown";

    private PowerMode _currentPowerMode = PowerMode.Auto;

    public SettingsViewModel(AppDatabase db, IDeviceService deviceService, IModelService modelService, IInferenceService inferenceService)
    {
        _db = db;
        _deviceService = deviceService;
        _modelService = modelService;
        _inferenceService = inferenceService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        Temperature = await _db.GetSettingAsync(AppConstants.KeyDefaultTemperature, AppConstants.DefaultTemperature);
        MaxTokens = await _db.GetSettingAsync(AppConstants.KeyDefaultMaxTokens, AppConstants.DefaultMaxTokens);
        TopP = await _db.GetSettingAsync("default_top_p", AppConstants.DefaultTopP);
        ContextLength = await _db.GetSettingAsync("default_context_length", AppConstants.DefaultContextLength);

        var powerModeStr = await _db.GetSettingAsync(AppConstants.KeyPowerMode);
        if (Enum.TryParse<PowerMode>(powerModeStr, out var pm))
            _currentPowerMode = pm;
        PowerModeDisplay = _currentPowerMode.DisplayName();

        var activeModel = await _modelService.GetActiveModelAsync();
        ActiveModelName = activeModel?.Name ?? "No model loaded";

        var benchmark = await _deviceService.RunBenchmarkAsync();
        DeviceTier = benchmark.DeviceTier;
        RamInfo = $"{benchmark.AvailableRamMb}MB available / {benchmark.TotalRamMb}MB total";
        StorageInfo = $"{benchmark.FreeStorageMb}MB free";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await _db.SetSettingAsync(AppConstants.KeyDefaultTemperature, Temperature.ToString());
        await _db.SetSettingAsync(AppConstants.KeyDefaultMaxTokens, MaxTokens.ToString());
        await _db.SetSettingAsync("default_top_p", TopP.ToString());
        await _db.SetSettingAsync("default_context_length", ContextLength.ToString());
        _inferenceService.UpdateSamplingParams(Temperature, TopP);

        await Shell.Current.DisplayAlert("Saved", "Settings saved successfully.", "OK");
    }

    [RelayCommand]
    private async Task CyclePowerModeAsync()
    {
        _currentPowerMode = _currentPowerMode switch
        {
            PowerMode.Auto => PowerMode.Efficient,
            PowerMode.Efficient => PowerMode.Performance,
            PowerMode.Performance => PowerMode.Auto,
            _ => PowerMode.Auto
        };

        PowerModeDisplay = _currentPowerMode.DisplayName();
        _inferenceService.SetPowerMode(_currentPowerMode);
        await _db.SetSettingAsync(AppConstants.KeyPowerMode, _currentPowerMode.ToString());
    }

    [RelayCommand]
    private async Task OpenMarketplaceAsync() => await Shell.Current.GoToAsync("marketplace");

    [RelayCommand]
    private async Task OpenStorageAsync() => await Shell.Current.GoToAsync("storage");

    [RelayCommand]
    private async Task OpenCustomPromptsAsync() => await Shell.Current.GoToAsync("customprompts");

    [RelayCommand]
    private async Task OpenNetworkAuditAsync() => await Shell.Current.GoToAsync("networkaudit");
}
