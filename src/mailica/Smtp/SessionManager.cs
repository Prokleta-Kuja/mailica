namespace mailica.Smtp;

public class SessionManager
{
    readonly HashSet<SessionHandle> _sessions = new();
    readonly object _sessionsLock = new();

    internal void Run(SessionContext sessionContext, CancellationToken cancellationToken)
    {
        var handle = new SessionHandle(new Session(sessionContext), sessionContext);
        Add(handle);

        handle.CompletionTask = RunAsync(handle, cancellationToken);

        handle.CompletionTask.ContinueWith(task =>
            {
                Remove(handle);
            }, cancellationToken);
    }

    static async Task RunAsync(SessionHandle handle, CancellationToken cancellationToken)
    {
        try
        {
            await UpgradeAsync(handle, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            await handle.Session.RunAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
        }
        finally
        {
            await handle.SessionContext.Pipe!.Input.CompleteAsync();

            handle.SessionContext.Dispose();
        }
    }

    static async Task UpgradeAsync(SessionHandle handle, CancellationToken cancellationToken)
    {
        var endpoint = handle.SessionContext.EndpointDefinition;

        if (endpoint.IsSecure && endpoint.ServerCertificate != null)
        {
            await handle.SessionContext.Pipe!.UpgradeAsync(endpoint.ServerCertificate, endpoint.SupportedSslProtocols, cancellationToken).ConfigureAwait(false);
        }
    }

    internal Task WaitAsync()
    {
        IReadOnlyList<Task> tasks;

        lock (_sessionsLock)
        {
            tasks = _sessions.Select(session => session.CompletionTask!).ToList();
        }

        return Task.WhenAll(tasks);
    }

    void Add(SessionHandle handle)
    {
        lock (_sessionsLock)
        {
            _sessions.Add(handle);
        }
    }

    void Remove(SessionHandle handle)
    {
        lock (_sessionsLock)
        {
            _sessions.Remove(handle);
        }
    }

    class SessionHandle
    {
        public SessionHandle(Session session, SessionContext sessionContext)
        {
            Session = session;
            SessionContext = sessionContext;
        }

        public Session Session { get; }

        public SessionContext SessionContext { get; }

        public Task? CompletionTask { get; set; }
    }
}