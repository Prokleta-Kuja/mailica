using System.Collections;
using mailica.Smtp.Commands;

namespace mailica.Smtp;

public enum StateId
{
    None = 0,
    Initialized = 1,
    WaitingForMail = 2,
    WaitingForMailSecure = 3,
    WithinTransaction = 4,
    CanAcceptData = 5,
}
public class State : IEnumerable
{
    internal State(StateId stateId)
    {
        StateId = stateId;
    }

    internal void Add(string command)
    {
        Transitions.Add(command, new StateTransition(context => true, context => StateId));
    }

    internal void Add(string command, StateId state)
    {
        Transitions.Add(command, new StateTransition(context => true, context => state));
    }

    internal void Add(string command, Func<SessionContext, StateId> transitionDelegate)
    {
        Transitions.Add(command, new StateTransition(context => true, transitionDelegate));
    }

    internal void Add(string command, Func<SessionContext, bool> canAcceptDelegate)
    {
        Transitions.Add(command, new StateTransition(canAcceptDelegate, context => StateId));
    }

    internal void Add(string command, Func<SessionContext, bool> canAcceptDelegate, StateId state)
    {
        Transitions.Add(command, new StateTransition(canAcceptDelegate, context => state));
    }

    internal void Add(string command, Func<SessionContext, bool> canAcceptDelegate, Func<SessionContext, StateId> transitionDelegate)
    {
        Transitions.Add(command, new StateTransition(canAcceptDelegate, transitionDelegate));
    }

    // this is just here for the collection initializer syntax to work
    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

    internal StateId StateId { get; }

    internal IDictionary<string, StateTransition> Transitions { get; } = new Dictionary<string, StateTransition>(StringComparer.OrdinalIgnoreCase);
}

public class StateTable : IEnumerable
{
    internal static readonly StateTable Shared = new()
        {
            new State(StateId.Initialized)
            {
                { NoopCommand.Command },
                { RsetCommand.Command },
                { QuitCommand.Command },
                { HeloCommand.Command, WaitingForMailSecureWhenSecure },
                { EhloCommand.Command, WaitingForMailSecureWhenSecure }
            },
            new State(StateId.WaitingForMail)
            {
                { NoopCommand.Command },
                { RsetCommand.Command },
                { QuitCommand.Command },
                { StartTlsCommand.Command, CanAcceptStartTls, StateId.WaitingForMailSecure },
                { AuthCommand.Command, context => context.EndpointDefinition.AllowUnsecureAuthentication && context.IsAuthenticated == false },
                { HeloCommand.Command, StateId.WaitingForMail },
                { EhloCommand.Command, StateId.WaitingForMail },
                { MailCommand.Command, StateId.WithinTransaction }
            },
            new State(StateId.WaitingForMailSecure)
            {
                { NoopCommand.Command },
                { RsetCommand.Command },
                { QuitCommand.Command },
                { AuthCommand.Command, context => context.IsAuthenticated == false },
                { HeloCommand.Command, StateId.WaitingForMailSecure },
                { EhloCommand.Command, StateId.WaitingForMailSecure },
                { MailCommand.Command, StateId.WithinTransaction }
            },
            new State(StateId.WithinTransaction)
            {
                { NoopCommand.Command },
                { RsetCommand.Command, WaitingForMailSecureWhenSecure },
                { QuitCommand.Command },
                { RcptCommand.Command, StateId.CanAcceptData },
            },
            new State(StateId.CanAcceptData)
            {
                { NoopCommand.Command },
                { RsetCommand.Command, WaitingForMailSecureWhenSecure },
                { QuitCommand.Command },
                { RcptCommand.Command },
                { DataCommand.Command, StateId.WaitingForMail },
            }
        };

    static StateId WaitingForMailSecureWhenSecure(SessionContext context)
    {
        return context.Pipe!.IsSecure ? StateId.WaitingForMailSecure : StateId.WaitingForMail;
    }

    static bool CanAcceptStartTls(SessionContext context)
    {
        return context.EndpointDefinition.ServerCertificate != null && context.Pipe?.IsSecure == false;
    }

    readonly IDictionary<StateId, State> _states = new Dictionary<StateId, State>();

    internal State this[StateId stateId] => _states[stateId];

    /// <summary>
    /// Add the state to the table.
    /// </summary>
    /// <param name="state"></param>
    void Add(State state)
    {
        _states.Add(state.StateId, state);
    }

    /// <summary>
    /// Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        // this is just here for the collection initializer syntax to work
        throw new NotImplementedException();
    }
}

public class StateTransition
{
    readonly Func<SessionContext, bool> _canAcceptDelegate;
    readonly Func<SessionContext, StateId> _transitionDelegate;

    internal StateTransition(Func<SessionContext, bool> canAcceptDelegate, Func<SessionContext, StateId> transitionDelegate)
    {
        _canAcceptDelegate = canAcceptDelegate;
        _transitionDelegate = transitionDelegate;
    }

    internal bool CanAccept(SessionContext context)
    {
        return _canAcceptDelegate(context);
    }

    internal StateId Transition(SessionContext context)
    {
        return _transitionDelegate(context);
    }
}

public class StateMachine
{
    readonly SessionContext _context;
    State _state;
    StateTransition? _transition;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="context">The SMTP server session context.</param>
    internal StateMachine(SessionContext context)
    {
        _state = StateTable.Shared[StateId.Initialized];
        _context = context;
    }

    /// <summary>
    /// Try to accept the command given the current state.
    /// </summary>
    /// <param name="command">The command to accept.</param>
    /// <param name="errorResponse">The error response to display if the command was not accepted.</param>
    /// <returns>true if the command could be accepted, false if not.</returns>
    public bool TryAccept(Command command, out Response? errorResponse)
    {
        errorResponse = null;

        if (_state.Transitions.TryGetValue(command.Name, out var transition) == false || transition.CanAccept(_context) == false)
        {
            var commands = _state.Transitions.Where(t => t.Value.CanAccept(_context)).Select(t => t.Key);

            errorResponse = new Response(ReplyCode.SyntaxError, $"expected {string.Join("/", commands)}");
            return false;
        }

        _transition = transition;
        return true;
    }

    /// <summary>
    /// Accept the state and transition to the new state.
    /// </summary>
    /// <param name="context">The session context to use for accepting session based transitions.</param>
    public void Transition(SessionContext context)
    {
        _state = StateTable.Shared[_transition!.Transition(context)];
    }
}