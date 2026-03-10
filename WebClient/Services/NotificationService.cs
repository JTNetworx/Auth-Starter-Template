namespace WebClient.Services;

public enum AppNotificationSeverity { Info, Success, Warning, Error }

public record AppNotification(
    string Title,
    string Message,
    AppNotificationSeverity Severity,
    DateTime CreatedAt);

/// <summary>
/// In-memory notification store for the bell icon / alert panel.
/// Call Add() from anywhere via DI. Components subscribe via OnChange.
/// </summary>
public sealed class NotificationService
{
    private readonly List<AppNotification> _notifications = [];

    public IReadOnlyList<AppNotification> Notifications => _notifications.AsReadOnly();

    public int UnreadCount => _notifications.Count;

    public event Action? OnChange;

    public void Add(string title, string message, AppNotificationSeverity severity = AppNotificationSeverity.Info)
    {
        _notifications.Insert(0, new AppNotification(title, message, severity, DateTime.Now));
        OnChange?.Invoke();
    }

    public void Remove(AppNotification notification)
    {
        _notifications.Remove(notification);
        OnChange?.Invoke();
    }

    public void Clear()
    {
        _notifications.Clear();
        OnChange?.Invoke();
    }
}
