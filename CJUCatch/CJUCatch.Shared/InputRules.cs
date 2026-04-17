using System.Text.RegularExpressions;

namespace CJUCatch.Shared;

public static partial class InputRules
{
    public const int DisplayNameMaxUnits = 12;
    public const int SpeechBubbleMaxUnits = 40;
    public const int InstanceCodeLength = 8;
    public const int SessionIdMaxLength = 64;

    [GeneratedRegex("^[A-Z0-9]+$")]
    private static partial Regex InstanceCodeRegex();

    public static string NormalizeDisplayName(string value)
    {
        return TrimToUnitLength(value.Trim(), DisplayNameMaxUnits);
    }

    public static bool IsValidInstanceCode(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        return normalized.Length == InstanceCodeLength && InstanceCodeRegex().IsMatch(normalized);
    }

    public static bool IsValidSessionId(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= SessionIdMaxLength;
    }

    public static bool IsValidDisplayName(string value)
    {
        var trimmed = value.Trim();
        return !string.IsNullOrWhiteSpace(trimmed) && GetDisplayNameUnitCount(trimmed) <= DisplayNameMaxUnits;
    }

    public static string NormalizeSpeechBubble(string value)
    {
        return TrimToUnitLength(value.Trim(), SpeechBubbleMaxUnits);
    }

    public static bool IsValidSpeechBubble(string value)
    {
        var trimmed = value.Trim();
        return !string.IsNullOrWhiteSpace(trimmed) && GetDisplayNameUnitCount(trimmed) <= SpeechBubbleMaxUnits;
    }

    public static int GetDisplayNameUnitCount(string value)
    {
        var total = 0;
        foreach (var character in value)
        {
            total += GetDisplayNameUnits(character);
        }

        return total;
    }

    private static int GetDisplayNameUnits(char character)
    {
        return character <= sbyte.MaxValue ? 1 : 2;
    }

    private static string TrimToUnitLength(string value, int maxUnits)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var unitCount = 0;
        var buffer = new List<char>(value.Length);
        foreach (var character in value)
        {
            var nextUnits = unitCount + GetDisplayNameUnits(character);
            if (nextUnits > maxUnits)
            {
                break;
            }

            unitCount = nextUnits;
            buffer.Add(character);
        }

        return new string([.. buffer]);
    }
}
