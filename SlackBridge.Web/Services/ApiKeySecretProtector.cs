using Microsoft.AspNetCore.DataProtection;

namespace SlackBridge.Web.Services;

public interface IApiKeySecretProtector
{
    string Protect(string apiKey);
    string? Unprotect(string? encryptedApiKey);
}

public sealed class ApiKeySecretProtector(IDataProtectionProvider dataProtectionProvider) : IApiKeySecretProtector
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("SlackBridge.ApiKeys.v1");

    public string Protect(string apiKey) => _protector.Protect(apiKey);

    public string? Unprotect(string? encryptedApiKey)
    {
        if (string.IsNullOrWhiteSpace(encryptedApiKey))
        {
            return null;
        }

        try
        {
            return _protector.Unprotect(encryptedApiKey);
        }
        catch
        {
            return null;
        }
    }
}
