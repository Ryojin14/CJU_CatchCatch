using CJUCatch.Server.Models;
using CJUCatch.Server.Options;
using CJUCatch.Server.Services;
using CJUCatch.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace CJUCatch.Server.Hubs;

public sealed class PresenceHub(
    InstanceRegistry registry,
    AttemptLimiter attemptLimiter,
    IOptions<SecurityOptions> securityOptions) : Hub
{
    private const string InstanceCodeKey = "InstanceCode";
    private const string SessionIdKey = "SessionId";

    public Task<string> CreateInstance(CreateInstanceRequest request)
    {
        EnforceAttemptLimit("create", securityOptions.Value.CreateAttemptsPerWindow);
        ValidateCreateRequest(request);

        var instance = registry.CreateInstance(request);
        return Task.FromResult(instance.InstanceCode);
    }

    public async Task<IReadOnlyCollection<ParticipantSnapshot>> JoinInstance(JoinInstanceRequest request)
    {
        EnforceAttemptLimit("join", securityOptions.Value.JoinAttemptsPerWindow);
        ValidateJoinRequest(request);

        if (!registry.TryGet(request.InstanceCode, out var instance) || instance is null)
        {
            throw new HubException("Instance not found.");
        }

        var participant = new ParticipantRecord
        {
            SessionId = request.SessionId.Trim(),
            DisplayName = InputRules.NormalizeDisplayName(request.DisplayName),
            ConnectionId = Context.ConnectionId,
            State = PresenceState.Idle,
            PositionX = 0.5,
            PositionY = 0.5,
        };

        lock (instance)
        {
            instance.Participants[participant.SessionId] = participant;
        }

        Context.Items[InstanceCodeKey] = instance.InstanceCode;
        Context.Items[SessionIdKey] = participant.SessionId;

        await Groups.AddToGroupAsync(Context.ConnectionId, instance.InstanceCode);
        await Clients.OthersInGroup(instance.InstanceCode).SendAsync("ParticipantJoined", ToSnapshot(participant));

        lock (instance)
        {
            return instance.Participants.Values.Select(ToSnapshot).ToArray();
        }
    }

    public async Task UpdatePresence(PresenceUpdate update)
    {
        if (!TryGetCurrentParticipant(out var instance, out var participant))
        {
            return;
        }

        participant.PositionX = Math.Clamp(update.PositionX, 0.0, 1.0);
        participant.PositionY = Math.Clamp(update.PositionY, 0.0, 1.0);
        participant.State = update.State;

        await Clients.OthersInGroup(instance.InstanceCode)
            .SendAsync("PresenceUpdated", ToSnapshot(participant));
    }

    public async Task UpdateSpeechBubble(string? text)
    {
        if (!TryGetCurrentParticipant(out var instance, out var participant))
        {
            return;
        }

        var normalized = string.IsNullOrWhiteSpace(text)
            ? null
            : InputRules.NormalizeSpeechBubble(text);

        if (!string.IsNullOrWhiteSpace(normalized) && !InputRules.IsValidSpeechBubble(normalized))
        {
            throw new HubException("Speech bubble text is invalid.");
        }

        await Clients.OthersInGroup(instance.InstanceCode)
            .SendAsync("SpeechBubbleUpdated", new SpeechBubbleUpdate(participant.SessionId, normalized));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? emptiedInstanceCode = null;

        if (TryGetCurrentParticipant(out var instance, out var participant))
        {
            lock (instance)
            {
                instance.Participants.Remove(participant.SessionId);
                if (instance.Participants.Count == 0)
                {
                    emptiedInstanceCode = instance.InstanceCode;
                }
            }

            await Clients.OthersInGroup(instance.InstanceCode)
                .SendAsync("ParticipantLeft", participant.SessionId);
        }

        if (!string.IsNullOrWhiteSpace(emptiedInstanceCode))
        {
            registry.RemoveIfEmpty(emptiedInstanceCode);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private bool TryGetCurrentParticipant(out InstanceRecord instance, out ParticipantRecord participant)
    {
        instance = null!;
        participant = null!;

        if (!Context.Items.TryGetValue(InstanceCodeKey, out var rawInstanceCode) ||
            !Context.Items.TryGetValue(SessionIdKey, out var rawSessionId))
        {
            return false;
        }

        var instanceCode = rawInstanceCode as string;
        var sessionId = rawSessionId as string;
        if (string.IsNullOrWhiteSpace(instanceCode) || string.IsNullOrWhiteSpace(sessionId))
        {
            return false;
        }

        if (!registry.TryGet(instanceCode, out var foundInstance) || foundInstance is null)
        {
            return false;
        }

        lock (foundInstance)
        {
            if (!foundInstance.Participants.TryGetValue(sessionId, out var foundParticipant))
            {
                return false;
            }

            instance = foundInstance;
            participant = foundParticipant;
            return true;
        }
    }

    private static ParticipantSnapshot ToSnapshot(ParticipantRecord participant)
    {
        return new ParticipantSnapshot(
            participant.SessionId,
            participant.DisplayName,
            participant.PositionX,
            participant.PositionY,
            participant.State);
    }

    private void EnforceAttemptLimit(string scope, int maxAttempts)
    {
        var window = TimeSpan.FromSeconds(securityOptions.Value.AttemptWindowSeconds);
        var actorKey = GetActorKey();

        if (attemptLimiter.TryConsume(scope, actorKey, maxAttempts, window, out var retryAfter))
        {
            return;
        }

        var retrySeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        throw new HubException($"Too many {scope} attempts. Try again in {retrySeconds} seconds.");
    }

    private string GetActorKey()
    {
        var remoteIp = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            return remoteIp;
        }

        return $"connection:{Context.ConnectionId}";
    }

    private static void ValidateCreateRequest(CreateInstanceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new HubException("Display name is required.");
        }

        if (!InputRules.IsValidDisplayName(request.DisplayName))
        {
            throw new HubException("Display name must be within the allowed length.");
        }
    }

    private static void ValidateJoinRequest(JoinInstanceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InstanceCode))
        {
            throw new HubException("Instance code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new HubException("Display name is required.");
        }

        if (!InputRules.IsValidInstanceCode(request.InstanceCode))
        {
            throw new HubException("Instance code format is invalid.");
        }

        if (!InputRules.IsValidDisplayName(request.DisplayName))
        {
            throw new HubException("Display name must be within the allowed length.");
        }

        if (!InputRules.IsValidSessionId(request.SessionId))
        {
            throw new HubException("Session ID is invalid.");
        }
    }
}
