using System.Globalization;

namespace mailica;

public static class C
{
    public static readonly bool IsDebug;
    public static readonly TimeZoneInfo TZ;
    public static readonly CultureInfo Locale;
    static C()
    {
        IsDebug = Environment.GetEnvironmentVariable("DEBUG") == "1";

        try
        {
            TZ = TimeZoneInfo.FindSystemTimeZoneById(Environment.GetEnvironmentVariable("TZ") ?? "Europe/Zagreb");
        }
        catch (Exception)
        {
            TZ = TimeZoneInfo.Local;
        }

        try
        {
            Locale = CultureInfo.GetCultureInfo(Environment.GetEnvironmentVariable("LOCALE") ?? "en-UK");
        }
        catch (System.Exception)
        {
            Locale = CultureInfo.InvariantCulture;
        }
    }
}