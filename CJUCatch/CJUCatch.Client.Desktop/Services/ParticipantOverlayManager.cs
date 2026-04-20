using CJUCatch.Client.Desktop.Views;
using CJUCatch.Shared;
using System.Windows;

namespace CJUCatch.Client.Desktop.Services;

internal sealed class ParticipantOverlayManager : IDisposable
{
    private readonly string _localSessionId;
    private readonly Dictionary<string, ParticipantAvatarWindow> _windows = new(StringComparer.Ordinal);
    private bool _othersVisible = true;

    public ParticipantOverlayManager(string localSessionId)
    {
        _localSessionId = localSessionId;
    }

    public void Upsert(ParticipantSnapshot snapshot)
    {
        if (snapshot.SessionId == _localSessionId)
        {
            return;
        }

        if (!_windows.TryGetValue(snapshot.SessionId, out var window))
        {
            window = new ParticipantAvatarWindow();
            _windows[snapshot.SessionId] = window;
            window.Visibility = _othersVisible ? Visibility.Visible : Visibility.Hidden;
            window.Show();
        }

        window.ApplySnapshot(snapshot);
    }

    public void Remove(string sessionId)
    {
        if (!_windows.Remove(sessionId, out var window))
        {
            return;
        }

        window.Close();
    }

    public void UpdateSpeechBubble(string sessionId, string? text)
    {
        if (_windows.TryGetValue(sessionId, out var window))
        {
            window.UpdateSpeechBubble(text);
        }
    }

    public void SetOthersVisible(bool visible)
    {
        _othersVisible = visible;
        foreach (var (_, window) in _windows)
        {
            window.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }
    }

    public void Clear()
    {
        foreach (var (_, window) in _windows)
        {
            window.Close();
        }

        _windows.Clear();
    }

    public void Dispose()
    {
        Clear();
    }
}
