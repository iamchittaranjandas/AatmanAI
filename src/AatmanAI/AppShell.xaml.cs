using AatmanAI.Views;

namespace AatmanAI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for non-tab pages
        Routing.RegisterRoute("chat", typeof(ChatPage));
        Routing.RegisterRoute("marketplace", typeof(MarketplacePage));
        Routing.RegisterRoute("customprompts", typeof(CustomPromptsPage));
        Routing.RegisterRoute("storage", typeof(StoragePage));
        Routing.RegisterRoute("networkaudit", typeof(NetworkAuditPage));
        Routing.RegisterRoute("login", typeof(LoginPage));
        Routing.RegisterRoute("profilesetup", typeof(ProfileSetupPage));
        Routing.RegisterRoute("profilelicense", typeof(ProfileLicensePage));
        Routing.RegisterRoute("modeldetail", typeof(ModelDetailPage));
        Routing.RegisterRoute("vault", typeof(VaultPage));
        Routing.RegisterRoute("downloadmanager", typeof(DownloadManagerPage));
        Routing.RegisterRoute("search", typeof(SearchPage));
    }
}
