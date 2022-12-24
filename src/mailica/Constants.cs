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
        catch (Exception)
        {
            Locale = CultureInfo.InvariantCulture;
        }
    }
    public static class Paths
    {
        public static readonly string AppDbConnectionString = $"Data Source={AppDataFor("app.db")}";
        public static string AppData => IsDebug ? Path.Combine(Environment.CurrentDirectory, "data") : "/data";
        public static string AppDataFor(string file) => Path.Combine(AppData, file);
        public static string MailData => AppDataFor("mail");
        public static string MailDataFor(string username) => Path.Combine(MailData, username.ToLower());
        public static string ConfigData => AppDataFor("config");
        public static string ConfigDataFor(string file) => Path.Combine(ConfigData, file);
        public static string CertData => AppDataFor("certs");
        public static string CertDataFor(string file) => Path.Combine(CertData, file);
    }
    public static class Routes
    {
        public const string Root = "/";
        public const string Credentials = "/credentials";
        public const string Credential = "/credentials/{AliasId:guid}";
        public static string CredentialFor(Guid aliasId) => $"{Credentials}/{aliasId}";
    }
}