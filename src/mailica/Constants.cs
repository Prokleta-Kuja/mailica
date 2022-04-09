using System;
using System.IO;

namespace mailica;

public static class C
{
    public static class Paths
    {
        public static string AppData => Path.Combine(Environment.CurrentDirectory, "data");
        public static string AppDataFor(string file) => Path.Combine(AppData, file);
        public static string Undelivered(int credId) => Path.Combine(AppDataFor("undelivered"), credId.ToString());
        public static readonly string AppDbConnectionString = $"Data Source={AppDataFor("app.db")}";
    }
    public static class Routes
    { }
}