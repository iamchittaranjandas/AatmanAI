using AatmanAI.Data.Database;
using AatmanAI.Services;
using AatmanAI.Services.Interfaces;
using AatmanAI.ViewModels;
using AatmanAI.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace AatmanAI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                // Remove Entry underline on Android
                Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
                {
                    handler.PlatformView.BackgroundTintList =
                        Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
                });

                // Remove Shell toolbar shadow/elevation line on Android
                Microsoft.Maui.Handlers.PageHandler.Mapper.AppendToMapping("NoToolbarShadow", (handler, view) =>
                {
                    if (handler.PlatformView?.RootView is Android.Views.ViewGroup root)
                        RemoveAppBarElevation(root);
                });
#endif
            });

        // Database
        builder.Services.AddSingleton<AppDatabase>();

        // HttpClient
        builder.Services.AddSingleton<HttpClient>();

        // Services (singletons for stateful services)
        builder.Services.AddSingleton<INetworkAuditService, NetworkAuditService>();
        builder.Services.AddSingleton<IModelService, ModelService>();
        builder.Services.AddSingleton<IDeviceService, DeviceService>();
        builder.Services.AddSingleton<IDownloadService, DownloadService>();
        builder.Services.AddSingleton<IInferenceService, InferenceService>();
        builder.Services.AddSingleton<ICustomPromptService, CustomPromptService>();
        builder.Services.AddSingleton<IChatService, ChatService>();

        // Stub services (Phase 2/3)
        builder.Services.AddSingleton<IVoiceService, VoiceService>();
        builder.Services.AddSingleton<ITranslationService, TranslationService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IVaultService, VaultService>();

        // ViewModels
        builder.Services.AddTransient<SplashViewModel>();
        builder.Services.AddTransient<FirstLaunchViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<ChatViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<MarketplaceViewModel>();
        builder.Services.AddTransient<CustomPromptsViewModel>();
        builder.Services.AddTransient<StorageViewModel>();
        builder.Services.AddTransient<NetworkAuditViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<ProfileSetupViewModel>();
        builder.Services.AddTransient<ProfileLicenseViewModel>();
        builder.Services.AddTransient<ModelDetailViewModel>();
        builder.Services.AddTransient<VaultViewModel>();
        builder.Services.AddTransient<DownloadManagerViewModel>();
        builder.Services.AddTransient<SearchViewModel>();

        // Pages
        builder.Services.AddTransient<SplashPage>();
        builder.Services.AddTransient<FirstLaunchPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<ChatPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<MarketplacePage>();
        builder.Services.AddTransient<CustomPromptsPage>();
        builder.Services.AddTransient<StoragePage>();
        builder.Services.AddTransient<NetworkAuditPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<ProfileSetupPage>();
        builder.Services.AddTransient<ProfileLicensePage>();
        builder.Services.AddTransient<ModelDetailPage>();
        builder.Services.AddTransient<VaultPage>();
        builder.Services.AddTransient<DownloadManagerPage>();
        builder.Services.AddTransient<SearchPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

#if ANDROID
    private static void RemoveAppBarElevation(Android.Views.View? view)
    {
        if (view == null) return;

        if (view is Google.Android.Material.AppBar.AppBarLayout appBar)
        {
            appBar.Elevation = 0;
            appBar.StateListAnimator = null;
            appBar.OutlineProvider = null;
            appBar.SetBackgroundColor(Android.Graphics.Color.ParseColor("#0B0C15"));
        }

        if (view is Android.Views.ViewGroup viewGroup)
        {
            for (int i = 0; i < viewGroup.ChildCount; i++)
            {
                RemoveAppBarElevation(viewGroup.GetChildAt(i));
            }
        }
    }
#endif
}
