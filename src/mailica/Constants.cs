using System;
using System.Globalization;
using System.IO;

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
    public static class Paths
    {
        public static string AppData => Path.Combine(Environment.CurrentDirectory, "data");
        public static string AppDataFor(string file) => Path.Combine(AppData, file);
        public static string Undelivered(int credId) => Path.Combine(AppDataFor("undelivered"), credId.ToString());
        public static readonly string AppDbConnectionString = $"Data Source={AppDataFor("app.db")}";
    }
    public static class Routes
    {
        public const string Root = "/";
        public const string Credentials = "/credentials";
        public const string Credential = "/credentials/{AliasId:guid}";
        public static string CredentialFor(Guid aliasId) => $"{Credentials}/{aliasId}";
    }
}