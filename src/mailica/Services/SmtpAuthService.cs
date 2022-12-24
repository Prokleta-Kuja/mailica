using mailica.Entities;
using Microsoft.EntityFrameworkCore;
using SmtpServer;
using SmtpServer.Authentication;

namespace mailica.Services;
public class SmtpAuthService : IUserAuthenticator
{
    public Task<bool> AuthenticateAsync(ISessionContext context, string user, string password, CancellationToken token)
    {
        var factory = context.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = factory.CreateDbContext();
        var users = db.Users.ToList();
        Console.WriteLine("Auth 1, User={0} Password={1}", user, password);

        return Task.FromResult(true);
    }
}