using System.Security.Cryptography;
using System.Text;

namespace SlackBridge.Web.Services;

public interface IApiKeyGenerator
{
    string Generate();
    string Hash(string apiKey);
    string Prefix(string apiKey);
}

public sealed class ApiKeyGenerator : IApiKeyGenerator
{
    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return $"sb_{Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=')}";
    }

    public string Hash(string apiKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(hash);
    }

    public string Prefix(string apiKey) => apiKey.Length <= 12 ? apiKey : apiKey[..12];
}
