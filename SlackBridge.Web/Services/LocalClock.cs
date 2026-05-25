using System.Globalization;

namespace SlackBridge.Web.Services;

public interface ILocalClock
{
    string Format(DateTimeOffset value);
}

public sealed class LocalClock : ILocalClock
{
    public static readonly TimeZoneInfo SwedishTimeZone = ResolveSwedishTimeZone();

    private static readonly CultureInfo SwedishCulture = CultureInfo.GetCultureInfo("sv-SE");

    public string Format(DateTimeOffset value)
    {
        var local = TimeZoneInfo.ConvertTime(value, SwedishTimeZone);
        return local.ToString("g", SwedishCulture);
    }

    private static TimeZoneInfo ResolveSwedishTimeZone()
    {
        foreach (var id in new[] { "Europe/Stockholm", "W. Europe Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Local;
    }
}
