using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace AatmanAI.Platforms.Android;

[Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeDataSync, Exported = false)]
public class DownloadForegroundService : Service
{
    private const int NotificationId = 9001;
    private const string ChannelId = "aatman_download";
    private PowerManager.WakeLock? _wakeLock;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();
        var notification = BuildNotification("Downloading AI model...");
        StartForeground(NotificationId, notification, global::Android.Content.PM.ForegroundService.TypeDataSync);
        AcquireWakeLock();
        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        ReleaseWakeLock();
        base.OnDestroy();
    }

    private void AcquireWakeLock()
    {
        if (_wakeLock != null) return;
        var pm = (PowerManager?)GetSystemService(PowerService);
        _wakeLock = pm?.NewWakeLock(WakeLockFlags.Partial, "AatmanAI::DownloadWakeLock");
        _wakeLock?.Acquire();
    }

    private void ReleaseWakeLock()
    {
        if (_wakeLock?.IsHeld == true)
            _wakeLock.Release();
        _wakeLock = null;
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var channel = new NotificationChannel(ChannelId, "Model Downloads",
            NotificationImportance.Low)
        {
            Description = "Shows progress while downloading AI models"
        };
        var nm = (NotificationManager?)GetSystemService(NotificationService);
        nm?.CreateNotificationChannel(channel);
    }

    private Notification BuildNotification(string text)
    {
        return new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("Aatman AI")
            .SetContentText(text)
            .SetSmallIcon(global::Android.Resource.Drawable.StatSysDownload)
            .SetOngoing(true)
            .SetProgress(0, 0, true)
            .Build();
    }

    public void UpdateProgress(string modelName, int percent)
    {
        var notification = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle($"Downloading {modelName}")
            .SetContentText($"{percent}% complete")
            .SetSmallIcon(global::Android.Resource.Drawable.StatSysDownload)
            .SetOngoing(true)
            .SetProgress(100, percent, false)
            .Build();

        var nm = (NotificationManager?)GetSystemService(NotificationService);
        nm?.Notify(NotificationId, notification);
    }
}
