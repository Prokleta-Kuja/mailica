using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace mailica.Smtp;

public static class StringUtil
{
    internal static string? Create(ReadOnlySequence<byte> sequence)
    {
        return Create(sequence, Encoding.ASCII);
    }

    internal static unsafe string? Create(ReadOnlySequence<byte> sequence, Encoding encoding)
    {
        if (sequence.Length == 0)
        {
            return null;
        }

        if (sequence.IsSingleSegment)
        {
            var span = sequence.First.Span;

            fixed (byte* ptr = span)
            {
                return encoding.GetString(ptr, span.Length);
            }
        }
        else
        {
            Span<byte> buffer = stackalloc byte[(int)sequence.Length];

            var i = 0;
            var position = sequence.GetPosition(0);

            while (sequence.TryGet(ref position, out var memory))
            {
                var span = memory.Span;
                for (var j = 0; j < span.Length; i++, j++)
                {
                    buffer[i] = span[j];
                }
            }

            fixed (byte* ptr = buffer)
            {
                return encoding.GetString(ptr, buffer.Length);
            }
        }
    }

    internal static string Create(ref ReadOnlySpan<byte> buffer)
    {
        return Create(ref buffer, Encoding.ASCII);
    }

    internal static unsafe string Create(ref ReadOnlySpan<byte> buffer, Encoding encoding)
    {
        fixed (byte* ptr = buffer)
        {
            return encoding.GetString(ptr, buffer.Length);
        }
    }
}
public enum TokenKind
{
    /// <summary>
    /// No token has been defined.
    /// </summary>
    None = 0,

    /// <summary>
    /// A text.
    /// </summary>
    Text,

    /// <summary>
    /// A number.
    /// </summary>
    Number,

    /// <summary>
    /// A single space character.
    /// </summary>
    Space,

    /// <summary>
    /// -
    /// </summary>
    Hyphen,

    /// <summary>
    /// .
    /// </summary>
    Period,

    /// <summary>
    /// [
    /// </summary>
    LeftBracket,

    /// <summary>
    /// ]
    /// </summary>
    RightBracket,

    /// <summary>
    /// :
    /// </summary>
    Colon,

    /// <summary>
    /// >
    /// </summary>
    GreaterThan,

    /// <summary>
    /// <
    /// </summary>
    LessThan = 10,

    /// <summary>
    /// ,
    /// </summary>
    Comma,

    /// <summary>
    /// @
    /// </summary>
    At,

    /// <summary>
    /// "
    /// </summary>
    Quote,

    /// <summary>
    /// =
    /// </summary>
    Equal,

    /// <summary>
    /// /
    /// </summary>
    Slash,

    /// <summary>
    /// \
    /// </summary>
    Backslash,

    /// <summary>
    /// +
    /// </summary>
    Plus,

    /// <summary>
    /// Unknown.
    /// </summary>
    Other,
}

[DebuggerDisplay("[{Kind}] {Text}")]
public readonly ref struct Token
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="kind">The token kind.</param>
    /// <param name="text">The text that the token represents.</param>
    public Token(TokenKind kind, ReadOnlySpan<byte> text = default)
    {
        Kind = kind;
        Text = text;
    }

    /// <summary>
    /// Returns a value indicating whether or not the given byte is considered a text character.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>true if the value is considered a text character, false if not.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsText(byte value)
    {
        return IsBetween(value, 'a', 'z') || IsBetween(value, 'A', 'Z') || IsUtf8(value);
    }

    /// <summary>
    /// Returns a value indicating whether or not the given byte is a UTF-8 encoded character.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>true if the value is considered a UTF-8 character, false if not.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUtf8(byte value)
    {
        return value >= 0x80;
    }

    /// <summary>
    /// Returns a value indicating whether or not the given byte is considered a digit character.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>true if the value is considered a digit character, false if not.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNumber(byte value)
    {
        return IsBetween(value, '0', '9');
    }

    /// <summary>
    /// Returns a value indicating whether or not the given byte is considered a whitespace.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>true if the value is considered a whitespace character, false if not.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWhiteSpace(byte value)
    {
        return value == 32 || IsBetween(value, 9, 13);
    }

    /// <summary>
    /// Returns a value indicating whether or not the given value is inclusively between a given range.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="low">The lower value of the range.</param>
    /// <param name="high">The higher value of the range.</param>
    /// <returns>true if the value is between the range, false if not.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsBetween(byte value, char low, char high)
    {
        return value >= low && value <= high;
    }

    /// <summary>
    /// Returns a value indicating whether or not the given value is inclusively between a given range.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="low">The lower value of the range.</param>
    /// <param name="high">The higher value of the range.</param>
    /// <returns>true if the value is between the range, false if not.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsBetween(byte value, byte low, byte high)
    {
        return value >= low && value <= high;
    }

    /// <summary>
    /// Indicates whether this instance and a specified object are equal.
    /// </summary>
    /// <param name="other">Another object to compare to. </param>
    /// <returns>true if <paramref name="other"/> and this instance are the same type and represent the same value; otherwise, false. </returns>
    public bool Equals(Token other)
    {
        // TODO: need a faster comparisson implementation
        return Kind == other.Kind && Text.ToString().Equals(other.Text.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns a value indicating the equality of the two objects.
    /// </summary>
    /// <param name="left">The left hand side of the comparisson.</param>
    /// <param name="right">The right hand side of the comparisson.</param>
    /// <returns>true if the left and right side are equal, false if not.</returns>
    public static bool operator ==(Token left, Token right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns a value indicating the inequality of the two objects.
    /// </summary>
    /// <param name="left">The left hand side of the comparisson.</param>
    /// <param name="right">The right hand side of the comparisson.</param>
    /// <returns>false if the left and right side are equal, true if not.</returns>
    public static bool operator !=(Token left, Token right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Returns the string representation of the token.
    /// </summary>
    /// <returns>The string representation of the token.</returns>
    public override string ToString()
    {
        return $"[{Kind}] {Text.ToString()}";
    }

    /// <summary>
    /// Returns the Text selection as a string.
    /// </summary>
    /// <returns>The string that was created from the selection.</returns>
    public string ToText()
    {
        var text = Text;

        return StringUtil.Create(ref text);
    }

    /// <summary>
    /// Gets the token kind.
    /// </summary>
    public TokenKind Kind { get; }

    /// <summary>
    /// Returns the text representation of the token.
    /// </summary>
    public ReadOnlySpan<byte> Text { get; }

    public override bool Equals(object? obj)
    {// wasn't there originally
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {// wasn't there originally
        throw new NotImplementedException();
    }
}

public ref struct TokenReader
{
    /// <summary>
    /// Delegate for the TryMake function.
    /// </summary>
    /// <param name="reader">The token reader to use for the operation.</param>
    /// <returns>true if the make operation was a success, false if not.</returns>
    public delegate bool TryMakeDelegate(ref TokenReader reader);

    /// <summary>
    /// Delegate for the TryMake function to allow for "out" parameters.
    /// </summary>
    /// <typeparam name="TOut">The type of the out parameter.</typeparam>
    /// <param name="reader">The token reader to use for the operation.</param>
    /// <param name="value">The out parameter that was found during the make operation.</param>
    /// <returns>true if the make operation found a parameter, false if not.</returns>
    public delegate bool TryMakeDelegate<TOut>(ref TokenReader reader, out TOut value);

    /// <summary>
    /// Delegate for the TryMake function to allow for "out" parameters.
    /// </summary>
    /// <typeparam name="TOut1">The type of the first out parameter.</typeparam>
    /// <typeparam name="TOut2">The type of the second out parameter.</typeparam>
    /// <param name="reader">The token reader to use for the operation.</param>
    /// <param name="value1">The first out parameter that was found during the make operation.</param>
    /// <param name="value2">The second out parameter that was found during the make operation.</param>
    /// <returns>true if the make operation found a parameter, false if not.</returns>
    public delegate bool TryMakeDelegate<TOut1, TOut2>(ref TokenReader reader, out TOut1 value1, out TOut2 value2);

    readonly ReadOnlySequence<byte> _buffer;
    Token _peek;
    bool _hasPeeked;
    SequencePosition _spanPosition;
    ReadOnlySpan<byte> _span;
    int _spanIndex;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="buffer">The buffer to read from.</param>
    public TokenReader(ReadOnlySequence<byte> buffer)
    {
        _buffer = buffer;
        _spanPosition = _buffer.GetPosition(0);
        _span = default;
        _spanIndex = 0;
        _peek = default;
        _hasPeeked = false;
    }

    /// <summary>
    /// Try to make a callback in a transactional way.
    /// </summary>
    /// <param name="delegate">The callback to perform the match.</param>
    /// <returns>true if the match could be made, false if not.</returns>
    public bool TryMake(TryMakeDelegate @delegate)
    {
        var span = _span;
        var spanIndex = _spanIndex;
        var spanPosition = _spanPosition;

        if (@delegate(ref this) == false)
        {
            _span = span;
            _spanIndex = spanIndex;
            _spanPosition = spanPosition;

            _hasPeeked = false;

            return false;
        }

        return true;
    }

    /// <summary>
    /// Try to make a callback in a transactional way.
    /// </summary>
    /// <param name="delegate">The callback to perform the match.</param>
    /// <param name="buffer">The buffer that was made.</param>
    /// <returns>true if the match could be made, false if not.</returns>
    public bool TryMake(TryMakeDelegate @delegate, out ReadOnlySequence<byte> buffer)
    {
        buffer = default;

        var span = _span;
        var spanIndex = _spanIndex;
        var spanPosition = _spanPosition;

        if (@delegate(ref this) == false)
        {
            _span = span;
            _spanIndex = spanIndex;
            _spanPosition = spanPosition;

            _hasPeeked = false;

            return false;
        }

        buffer = _buffer.Slice(spanIndex, _spanIndex - spanIndex);
        return true;
    }

    /// <summary>
    /// Try to make a callback in a transactional way.
    /// </summary>
    /// <param name="delegate">The callback to perform the match.</param>
    /// <param name="found">The parameter that was returned from the matching function.</param>
    /// <returns>true if the match could be made, false if not.</returns>
    public bool TryMake<TOut>(TryMakeDelegate<TOut> @delegate, out TOut found)
    {
        var span = _span;
        var spanIndex = _spanIndex;
        var spanPosition = _spanPosition;

        if (@delegate(ref this, out found) == false)
        {
            _span = span;
            _spanIndex = spanIndex;
            _spanPosition = spanPosition;

            _hasPeeked = false;

            return false;
        }

        return true;
    }

    /// <summary>
    /// Try to make a callback in a transactional way.
    /// </summary>
    /// <param name="delegate">The callback to perform the match.</param>
    /// <param name="value1">The first out parameter that was found during the make operation.</param>
    /// <param name="value2">The second out parameter that was found during the make operation.</param>
    /// <returns>true if the match could be made, false if not.</returns>
    public bool TryMake<TOut1, TOut2>(TryMakeDelegate<TOut1, TOut2> @delegate, out TOut1 value1, out TOut2 value2)
    {
        if (_buffer.IsSingleSegment)
        {
            var index = _spanIndex;

            if (@delegate(ref this, out value1, out value2) == false)
            {
                _spanIndex = index;
                _hasPeeked = false;

                return false;
            }

            return true;
        }

        throw new NotImplementedException();
    }

    /// <summary>
    /// Peek at the next token in the sequence.
    /// </summary>
    /// <returns>The next token in the sequence.</returns>
    public Token Peek()
    {
        if (_hasPeeked == false)
        {
            _peek = ReadToken();
            _hasPeeked = true;
        }

        return _peek;
    }

    /// <summary>
    /// Take the next token from the sequence.
    /// </summary>
    /// <returns>The next token from the sequence.</returns>
    public Token Take()
    {
        if (_hasPeeked)
        {
            _hasPeeked = false;
            _spanIndex += _peek.Text.Length;

            return _peek;
        }

        var token = ReadToken();

        _spanIndex += token.Text.Length;

        return token;
    }

    /// <summary>
    /// Skip the tokens.
    /// </summary>
    /// <param name="kind">The token kind to skip.</param>
    public void Skip(TokenKind kind)
    {
        while (Peek().Kind == kind)
        {
            Take();
        }
    }

    /// <summary>
    /// Skip the tokens.
    /// </summary>
    /// <param name="predicate">The predicate to determine whether to skip the tokens.</param>
    public void Skip(Func<TokenKind, bool> predicate)
    {
        while (predicate(Peek().Kind))
        {
            Take();
        }
    }

    /// <summary>
    /// Read a token from the current position in the sequence.
    /// </summary>
    /// <returns>The token that was read from the sequence.</returns>
    Token ReadToken()
    {
        if (_spanIndex >= _span.Length && MoveToNextSpan() == false)
        {
            return default;
        }

        switch (_span[_spanIndex])
        {
            case { } ch when Token.IsText(ch):
                return new Token(TokenKind.Text, ReadWhile(Token.IsText));

            case { } ch when Token.IsNumber(ch):
                return new Token(TokenKind.Number, ReadWhile(Token.IsNumber));

            case { } ch when Token.IsWhiteSpace(ch):
                return new Token(TokenKind.Space, ReadOne());

            case { } ch when ch == '-':
                return new Token(TokenKind.Hyphen, ReadOne());

            case { } ch when ch == '.':
                return new Token(TokenKind.Period, ReadOne());

            case { } ch when ch == '[':
                return new Token(TokenKind.LeftBracket, ReadOne());

            case { } ch when ch == ']':
                return new Token(TokenKind.RightBracket, ReadOne());

            case { } ch when ch == ':':
                return new Token(TokenKind.Colon, ReadOne());

            case { } ch when ch == '>':
                return new Token(TokenKind.GreaterThan, ReadOne());

            case { } ch when ch == '<':
                return new Token(TokenKind.LessThan, ReadOne());

            case { } ch when ch == ',':
                return new Token(TokenKind.Comma, ReadOne());

            case { } ch when ch == '@':
                return new Token(TokenKind.At, ReadOne());

            case { } ch when ch == '"':
                return new Token(TokenKind.Quote, ReadOne());

            case { } ch when ch == '=':
                return new Token(TokenKind.Equal, ReadOne());

            case { } ch when ch == '\\':
                return new Token(TokenKind.Backslash, ReadOne());

            case { } ch when ch == '/':
                return new Token(TokenKind.Slash, ReadOne());

            case { } ch when ch == '+':
                return new Token(TokenKind.Plus, ReadOne());
        }

        return new Token(TokenKind.Other, ReadOne());
    }

    /// <summary>
    /// Move to the next span in the sequence.
    /// </summary>
    /// <returns>true if the reader could be moved to the next span, false if not.</returns>
    bool MoveToNextSpan()
    {
        while (_buffer.TryGet(ref _spanPosition, out var memory))
        {
            _span = memory.Span;
            _spanIndex = 0;

            if (_span.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Reads a continual sequence whilst the predicate is matched.
    /// </summary>
    /// <param name="predicate">The predicate to match against the characters in the buffer.</param>
    /// <returns>The span that was matched.</returns>
    ReadOnlySpan<byte> ReadWhile(Func<byte, bool> predicate)
    {
        var count = 0;

        while (_spanIndex + count < _span.Length && predicate(_span[_spanIndex + count]))
        {
            count++;
        }

        return _span.Slice(_spanIndex, count);
    }

    /// <summary>
    /// Read a single character from the span.
    /// </summary>
    /// <returns>The span that was matched.</returns>
    ReadOnlySpan<byte> ReadOne()
    {
        return _span.Slice(_spanIndex, 1);
    }
}