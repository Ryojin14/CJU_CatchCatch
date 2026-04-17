using System.Net.Http;
using CJUCatch.Shared;
using Microsoft.AspNetCore.SignalR.Client;

namespace CJUCatch.Client.Desktop.Services;

internal sealed class PresenceClient : IAsyncDisposable
{
    private readonly HttpClient _httpClient = new();
    private HubConnection? _hubConnection;

    public event Action<ParticipantSnapshot>? ParticipantJoined;
    public event Action<ParticipantSnapshot>? ParticipantUpdated;
    public event Action<string>? ParticipantLeft;
    public event Action<SpeechBubbleUpdate>? SpeechBubbleUpdated;

    public async Task<string> CreateInstanceAsync(string serverBaseUrl, CreateInstanceRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(serverBaseUrl, cancellationToken);
        return await _hubConnection!.InvokeAsync<string>("CreateInstance", request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ParticipantSnapshot>> JoinInstanceAsync(string serverBaseUrl, JoinInstanceRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(serverBaseUrl, cancellationToken);
        return await _hubConnection!.InvokeAsync<IReadOnlyCollection<ParticipantSnapshot>>("JoinInstance", request, cancellationToken);
    }

    public async Task UpdatePresenceAsync(PresenceUpdate update, CancellationToken cancellationToken = default)
    {
        if (_hubConnection is null)
        {
            return;
        }

        await _hubConnection.InvokeAsync("UpdatePresence", update, cancellationToken);
    }

    public async Task UpdateSpeechBubbleAsync(string? text, CancellationToken cancellationToken = default)
    {
        if (_hubConnection is null)
        {
            return;
        }

        await _hubConnection.InvokeAsync("UpdateSpeechBubble", text, cancellationToken);
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection is null)
        {
            return;
        }

        try
        {
            await _hubConnection.StopAsync();
        }
        finally
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    private async Task EnsureConnectedAsync(string serverBaseUrl, CancellationToken cancellationToken)
    {
        var baseUrl = NormalizeBaseUrl(serverBaseUrl);
        var hubUrl = $"{baseUrl}/hubs/presence";

        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            return;
        }

        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<ParticipantSnapshot>("ParticipantJoined", snapshot => ParticipantJoined?.Invoke(snapshot));
        _hubConnection.On<ParticipantSnapshot>("PresenceUpdated", snapshot => ParticipantUpdated?.Invoke(snapshot));
        _hubConnection.On<string>("ParticipantLeft", sessionId => ParticipantLeft?.Invoke(sessionId));
        _hubConnection.On<SpeechBubbleUpdate>("SpeechBubbleUpdated", update => SpeechBubbleUpdated?.Invoke(update));

        await _hubConnection.StartAsync(cancellationToken);
    }

    private static string NormalizeBaseUrl(string serverBaseUrl)
    {
        return serverBaseUrl.Trim().TrimEnd('/');
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();

        _httpClient.Dispose();
    }
}
