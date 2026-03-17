using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace AatmanAI;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, WindowSoftInputMode = SoftInput.AdjustResize, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Dark status bar matching app background #0B0F19
        if (Window is not null)
        {
            Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#0B0C15"));
            Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#0B0C15"));
            // Remove the divider line above the navigation bar
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                Window.NavigationBarDividerColor = Android.Graphics.Color.ParseColor("#0B0C15");
            }
        }
    }
}
