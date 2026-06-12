using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SlackBridge.Web.Services;

public interface ISlackRequestVerifier
{
    SlackRequestVerificationResult Verify(
        string requestBody,
        string? timestamp,
        string? signature,
        string signingSecret,
        DateTimeOffset utcNow);
}

public sealed class SlackRequestVerifier : ISlackRequestVerifier
{
    private static readonly TimeSpan MaxRequestAge = TimeSpan.FromMinutes(5);

    public SlackRequestVerificationResult Verify(
        string requestBody,
        string? timestamp,
        string? signature,
        string signingSecret,
        DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(signingSecret))
        {
            return SlackRequestVerificationResult.Failed("missing_signing_secret");
        }

        if (string.IsNullOrWhiteSpace(timestamp) ||
            !long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var timestampSeconds))
        {
            return SlackRequestVerificationResult.Failed("missing_or_invalid_timestamp");
        }

        var requestTime = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
        if ((utcNow - requestTime).Duration() > MaxRequestAge)
        {
            return SlackRequestVerificationResult.Failed("stale_timestamp");
        }

        if (string.IsNullOrWhiteSpace(signature) ||
            !signature.StartsWith("v0=", StringComparison.OrdinalIgnoreCase))
        {
            return SlackRequestVerificationResult.Failed("missing_or_invalid_signature");
        }

        var baseString = $"v0:{timestamp}:{requestBody}";
        var keyBytes = Encoding.UTF8.GetBytes(signingSecret);
        var baseBytes = Encoding.UTF8.GetBytes(baseString);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(baseBytes);
        var expectedSignature = "v0=" + Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()))
            ? SlackRequestVerificationResult.Success()
            : SlackRequestVerificationResult.Failed("signature_mismatch");
    }
}

public sealed record SlackRequestVerificationResult(bool IsValid, string? FailureReason)
{
    public static SlackRequestVerificationResult Success() => new(true, null);

    public static SlackRequestVerificationResult Failed(string reason) => new(false, reason);
}
