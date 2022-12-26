namespace mailica.Smtp.Commands;

public class CommandFactory
{
    /// <summary>
    /// Create a HELO command.
    /// </summary>
    /// <param name="domainOrAddress">The domain name or address literal.</param>
    /// <returns>The HELO command.</returns>
    public virtual Command CreateHelo(string domainOrAddress)
    {
        return new HeloCommand(domainOrAddress);
    }

    /// <summary>
    /// Create a EHLO command.
    /// </summary>
    /// <param name="domainOrAddress">The domain name or address literal.</param>
    /// <returns>The EHLO command.</returns>
    public virtual Command CreateEhlo(string domainOrAddress)
    {
        return new EhloCommand(domainOrAddress);
    }

    /// <summary>
    /// Create a MAIL command.
    /// </summary>
    /// <param name="address">The Mailbox address that the message is from.</param>
    /// <param name="parameters">The optional parameters for the message.</param>
    /// <returns>The MAIL command.</returns>
    public virtual Command CreateMail(EmailAddress address, IReadOnlyDictionary<string, string> parameters)
    {
        return new MailCommand(address, parameters);
    }

    /// <summary>
    /// Create a RCPT command.
    /// </summary>
    /// <param name="address">The address that the mail is to.</param>
    /// <returns>The RCPT command.</returns>
    public virtual Command CreateRcpt(EmailAddress address)
    {
        return new RcptCommand(address);
    }

    /// <summary>
    /// Create a DATA command.
    /// </summary>
    /// <returns>The DATA command.</returns>
    public virtual Command CreateData()
    {
        return new DataCommand();
    }

    /// <summary>
    /// Create a QUIT command.
    /// </summary>
    /// <returns>The QUITcommand.</returns>
    public virtual Command CreateQuit()
    {
        return new QuitCommand();
    }

    /// <summary>
    /// Create a NOOP command.
    /// </summary>
    /// <returns>The NOOP command.</returns>
    public virtual Command CreateNoop()
    {
        return new NoopCommand();
    }

    /// <summary>
    /// Create a RSET command.
    /// </summary>
    /// <returns>The RSET command.</returns>
    public virtual Command CreateRset()
    {
        return new RsetCommand();
    }

    /// <summary>
    /// Create a STARTTLS command.
    /// </summary>
    /// <returns>The STARTTLS command.</returns>
    public virtual Command CreateStartTls()
    {
        return new StartTlsCommand();
    }

    /// <summary>
    /// Create a AUTH command.
    /// </summary>
    /// <param name="method">The authentication method.</param>
    /// <param name="parameter">The authentication parameter.</param>
    /// <returns>The AUTH command.</returns>
    public Command CreateAuth(AuthenticationMethod method, string? parameter)
    {
        return new AuthCommand(method, parameter);
    }
}