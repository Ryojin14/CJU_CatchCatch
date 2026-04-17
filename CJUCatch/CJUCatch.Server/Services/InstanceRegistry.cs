using System.Collections.Concurrent;
using System.Text;
using CJUCatch.Server.Models;
using CJUCatch.Shared;

namespace CJUCatch.Server.Services;

public sealed class InstanceRegistry
{
    private const string CodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private readonly ConcurrentDictionary<string, InstanceRecord> _instances = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<InstanceSummary> ListInstances()
    {
        return _instances.Values
            .Select(instance => new InstanceSummary(
                instance.InstanceCode,
                instance.Participants.Count))
            .OrderBy(instance => instance.InstanceCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal InstanceRecord CreateInstance(CreateInstanceRequest request)
    {
        var record = new InstanceRecord
        {
            InstanceCode = GenerateCode(),
        };

        _instances[record.InstanceCode] = record;
        return record;
    }

    internal bool TryGet(string instanceCode, out InstanceRecord? instance)
    {
        var found = _instances.TryGetValue(instanceCode.Trim(), out var value);
        instance = value;
        return found;
    }

    internal void RemoveIfEmpty(string instanceCode)
    {
        if (!_instances.TryGetValue(instanceCode.Trim(), out var instance) || instance is null)
        {
            return;
        }

        lock (instance)
        {
            if (instance.Participants.Count > 0)
            {
                return;
            }
        }

        _instances.TryRemove(instanceCode.Trim(), out _);
    }

    private string GenerateCode()
    {
        while (true)
        {
            var codeBuilder = new StringBuilder(InputRules.InstanceCodeLength);
            for (var i = 0; i < InputRules.InstanceCodeLength; i++)
            {
                codeBuilder.Append(CodeAlphabet[Random.Shared.Next(CodeAlphabet.Length)]);
            }

            var code = codeBuilder.ToString();
            if (!_instances.ContainsKey(code))
            {
                return code;
            }
        }
    }
}
