using System;

namespace mailica.Extensions;

public static class DateTimeExtensions
{
    public static string Display(this DateTime? dt, string format = "g") => dt.HasValue ? Display(dt, format) : string.Empty;
    public static string Display(this DateTime dt, string format = "g")
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(dt, C.TZ);
        return local.ToString(format, C.Locale.DateTimeFormat);
    }
}