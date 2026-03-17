using AatmanAI.ViewModels;

namespace AatmanAI.Views;

public partial class ChatPage : ContentPage
{
    public ChatPage(ChatViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;

        Shell.SetNavBarIsVisible(this, false);
        Shell.SetTabBarIsVisible(this, false);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Shell.SetNavBarIsVisible(this, false);
        Shell.SetTabBarIsVisible(this, false);

#if ANDROID
        var activity = Platform.CurrentActivity;
        if (activity?.Window != null)
        {
            activity.Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#0A0E14"));
            activity.Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#0A0E14"));
        }

        // Hide Shell AppBarLayout that causes white line
        var rootView = activity?.FindViewById<Android.Views.ViewGroup>(Android.Resource.Id.Content);
        HideAppBarLayouts(rootView);

        // Listen for keyboard to adjust header
        var decorView = activity?.Window?.DecorView;
        if (decorView != null)
        {
            decorView.ViewTreeObserver!.GlobalLayout += OnGlobalLayout;
        }
#endif
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

#if ANDROID
        var activity = Platform.CurrentActivity;
        var decorView = activity?.Window?.DecorView;
        if (decorView != null)
        {
            decorView.ViewTreeObserver!.GlobalLayout -= OnGlobalLayout;
        }

        // Restore colors for other pages
        if (activity?.Window != null)
        {
            activity.Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#0B0C15"));
            activity.Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#0B0C15"));
        }
#endif
    }

#if ANDROID
    private void OnGlobalLayout(object? sender, EventArgs e)
    {
        var activity = Platform.CurrentActivity;
        if (activity == null) return;

        var rootView = activity.FindViewById<Android.Views.View>(Android.Resource.Id.Content);
        if (rootView == null) return;

        var rect = new Android.Graphics.Rect();
        rootView.GetWindowVisibleDisplayFrame(rect);

        int screenHeight = rootView.Height;
        int visibleHeight = rect.Bottom - rect.Top;
        int keyboardHeight = screenHeight - visibleHeight;

        bool keyboardOpen = keyboardHeight > 200;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Reduce header padding when keyboard is open to keep it visible
            HeaderGrid.Padding = keyboardOpen
                ? new Thickness(16, 8, 16, 8)
                : new Thickness(16, 12, 16, 12);

            // Hide footer when keyboard is open
            FooterLabel.IsVisible = !keyboardOpen;
        });
    }

    private static void HideAppBarLayouts(Android.Views.ViewGroup? viewGroup)
    {
        if (viewGroup == null) return;
        for (int i = 0; i < viewGroup.ChildCount; i++)
        {
            var child = viewGroup.GetChildAt(i);
            if (child is Google.Android.Material.AppBar.AppBarLayout appBar)
            {
                appBar.Visibility = Android.Views.ViewStates.Gone;
            }
            else if (child is AndroidX.AppCompat.Widget.Toolbar toolbar)
            {
                toolbar.Visibility = Android.Views.ViewStates.Gone;
                if (toolbar.Parent is Google.Android.Material.AppBar.AppBarLayout parentAppBar)
                    parentAppBar.Visibility = Android.Views.ViewStates.Gone;
            }
            else if (child is Android.Views.ViewGroup childGroup)
            {
                HideAppBarLayouts(childGroup);
            }
        }
    }
#endif

    private async void OnBackTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
