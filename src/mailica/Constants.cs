using System.Globalization;

namespace mailica;

public static class C
{
    public static readonly bool IsDebug;
    public const string CRT_FILE = "cert.crt";
    public const string KEY_FILE = "cert.key";
    public static readonly string Hostname;
    public static readonly string MasterUser;
    public static readonly string MasterSecret;
    public static readonly TimeZoneInfo TZ;
    public static readonly CultureInfo Locale;
    static C()
    {
        IsDebug = Environment.GetEnvironmentVariable("DEBUG") == "1";
        Hostname = Environment.GetEnvironmentVariable("HOSTNAME") ?? string.Empty;
        MasterUser = Environment.GetEnvironmentVariable("MASTER_USER") ?? string.Empty;
        MasterSecret = Environment.GetEnvironmentVariable("MASTER_SECRET") ?? string.Empty;

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
        static string Root => IsDebug ? "./data" : string.Empty;
        public static readonly string MailData = $"{Root}/mail";
        public static string MailDataFor(string username) => Path.Combine(MailData, username.ToLower());
        public static readonly string ConfigData = $"{Root}/config";
        public static string ConfigDataFor(string file) => Path.Combine(ConfigData, file);
        public static readonly string CertData = $"{Root}/certs";
        public static string CertDataFor(string file) => Path.Combine(CertData, file);
        // Order matters
        public static readonly string CertCrt = CertDataFor(CRT_FILE);
        public static readonly string CertKey = CertDataFor(KEY_FILE);
        public static readonly string AppDb = ConfigDataFor("app.db");
        public static readonly string AppDbConnectionString = $"Data Source={AppDb}";
    }
    public static class Routes
    {
        public const string Root = "/";
        public const string Credentials = "/credentials";
        public const string Credential = "/credentials/{AliasId:guid}";
        public static string CredentialFor(Guid aliasId) => $"{Credentials}/{aliasId}";
    }
}