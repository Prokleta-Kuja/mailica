using System.Buffers;
using System.IO.Pipelines;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace mailica.Smtp;

public class SecurableDuplexPipe
{
    readonly Action _disposeAction;
    Stream? _stream;
    bool _disposed;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="stream">The stream that the pipe is reading and writing to.</param>
    /// <param name="disposeAction">The action to execute when the stream has been disposed.</param>
    public SecurableDuplexPipe(Stream stream, Action disposeAction)
    {
        _stream = stream;
        _disposeAction = disposeAction;

        Input = PipeReader.Create(_stream);
        Output = PipeWriter.Create(_stream);
    }

    /// <summary>
    /// Upgrade to a secure pipeline.
    /// </summary>
    /// <param name="certificate">The X509Certificate used to authenticate the server.</param>
    /// <param name="protocols">The value that represents the protocol used for authentication.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that asynchronously performs the operation.</returns>
    public async Task UpgradeAsync(X509Certificate certificate, SslProtocols protocols, CancellationToken cancellationToken = default)
    {
        if (_stream == null)
            return;

        var stream = new SslStream(_stream, true);

        var opt = new SslServerAuthenticationOptions
        {
            ServerCertificate = certificate,
            ClientCertificateRequired = false,
            EnabledSslProtocols = protocols,
            CertificateRevocationCheckMode = X509RevocationMode.Offline
        };

        await stream.AuthenticateAsServerAsync(opt, cancellationToken).ConfigureAwait(false);
        //await stream.AuthenticateAsServerAsync(certificate, false, protocols, true).ConfigureAwait(false);

        _stream = stream;

        Input = PipeReader.Create(_stream);
        Output = PipeWriter.Create(_stream);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the stream and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    void Dispose(bool disposing)
    {
        if (_disposed == false)
        {
            if (disposing)
            {
                _disposeAction();
                _stream = null;
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
    }

    /// <summary>
    /// Gets the <see cref="T:System.IO.Pipelines.PipeReader" /> half of the duplex pipe.
    /// </summary>
    public PipeReader Input { get; private set; }

    /// <summary>
    /// Gets the <see cref="T:System.IO.Pipelines.PipeWriter" /> half of the duplex pipe.
    /// </summary>
    public PipeWriter Output { get; private set; }

    /// <summary>
    /// Returns a value indicating whether or not the current pipeline is secure.
    /// </summary>
    public bool IsSecure => _stream is SslStream;
}

public class ByteArraySegment : ReadOnlySequenceSegment<byte>
{
    internal ByteArraySegment(ReadOnlyMemory<byte> memory)
    {
        Memory = memory;
    }

    internal ByteArraySegment Append(ReadOnlyMemory<byte> memory)
    {
        var segment = new ByteArraySegment(memory)
        {
            RunningIndex = RunningIndex + Memory.Length
        };

        Next = segment;

        return segment;
    }
}

public class ByteArraySegmentList
{
    internal void Append(byte[] buffer)
    {
        var sequence = new ReadOnlySequence<byte>(buffer);

        Append(ref sequence);
    }

    internal void Append(ref ReadOnlySequence<byte> sequence)
    {
        var position = sequence.GetPosition(0);

        while (sequence.TryGet(ref position, out var memory))
        {
            if (Start == null)
            {
                Start = new ByteArraySegment(memory);
                End = Start;
            }
            else
            {
                End = End.Append(memory);
            }
        }
    }

    internal ReadOnlySequence<byte> Build()
    {
        return new ReadOnlySequence<byte>(Start, 0, End, End.Memory.Length);
    }

    internal ByteArraySegment Start { get; private set; } = null!;

    internal ByteArraySegment End { get; private set; } = null!;
}

public static class IoExtensions
{
    static readonly byte[] CRLF = { 13, 10 };
    static readonly byte[] DotBlock = { 13, 10, 46, 13, 10 };
    static readonly byte[] DotBlockStuffing = { 13, 10, 46, 46 };

    /// <summary>
    /// Read from the reader until the sequence is found.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="sequence">The sequence to find to terminate the read operation.</param>
    /// <param name="func">The callback to execute to process the buffer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The value that was read from the buffer.</returns>
    static async ValueTask ReadUntilAsync(PipeReader reader, byte[] sequence, Func<ReadOnlySequence<byte>, Task> func, CancellationToken cancellationToken)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        var read = await reader.ReadAsync(cancellationToken);
        var head = read.Buffer.Start;

        while (read.IsCanceled == false && read.IsCompleted == false && read.Buffer.IsEmpty == false)
        {
            if (read.Buffer.TryFind(sequence, ref head, out var tail))
            {
                try
                {
                    await func(read.Buffer.Slice(read.Buffer.Start, head));
                }
                finally
                {
                    reader.AdvanceTo(tail);
                }

                return;
            }

            reader.AdvanceTo(read.Buffer.Start, read.Buffer.End);

            read = await reader.ReadAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Reads a line from the reader.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="func">The action to process the buffer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that can be used to wait on the operation on complete.</returns>
    internal static ValueTask ReadLineAsync(this PipeReader reader, Func<ReadOnlySequence<byte>, Task> func, CancellationToken cancellationToken = default)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        return ReadUntilAsync(reader, CRLF, func, cancellationToken);
    }

    /// <summary>
    /// Reads a line from the reader.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that can be used to wait on the operation on complete.</returns>
    internal static ValueTask<string> ReadLineAsync(this PipeReader reader, CancellationToken cancellationToken = default)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        return reader.ReadLineAsync(Encoding.ASCII, cancellationToken);
    }

    /// <summary>
    /// Reads a line from the reader.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="encoding">The encoding to use when converting the input.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that can be used to wait on the operation on complete.</returns>
    internal static async ValueTask<string> ReadLineAsync(this PipeReader reader, Encoding encoding, CancellationToken cancellationToken = default)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        var text = string.Empty;

        await reader.ReadLineAsync(
            buffer =>
            {
                text = StringUtil.Create(buffer, encoding);

                return Task.CompletedTask;
            },
            cancellationToken);

        return text;
    }

    /// <summary>
    /// Reads a line from the reader.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="func">The action to process the buffer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The value that was read from the buffer.</returns>
    internal static async ValueTask ReadDotBlockAsync(this PipeReader reader, Func<ReadOnlySequence<byte>, Task> func, CancellationToken cancellationToken = default)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        await ReadUntilAsync(
            reader,
            DotBlock,
            buffer =>
            {
                buffer = Unstuff(buffer);

                return func(buffer);
            },
            cancellationToken);

        static ReadOnlySequence<byte> Unstuff(ReadOnlySequence<byte> buffer)
        {
            var head = buffer.GetPosition(0);
            var start = head;

            var segments = new ByteArraySegmentList();

            while (buffer.TryFind(DotBlockStuffing, ref head, out var tail))
            {
                var slice = buffer.Slice(start, buffer.GetPosition(3, head));

                segments.Append(ref slice);

                start = tail;
                head = tail;
            }

            var remaining = buffer.Slice(start);
            segments.Append(ref remaining);

            return segments.Build();
        }
    }

    /// <summary>
    /// Write a line of text to the pipe.
    /// </summary>
    /// <param name="writer">The writer to perform the operation on.</param>
    /// <param name="text">The text to write to the writer.</param>
    internal static void WriteLine(this PipeWriter writer, string text)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        WriteLine(writer, Encoding.ASCII, text);
    }

    /// <summary>
    /// Write a line of text to the writer.
    /// </summary>
    /// <param name="writer">The writer to perform the operation on.</param>
    /// <param name="encoding">The encoding to use for the text.</param>
    /// <param name="text">The text to write to the writer.</param>
    static unsafe void WriteLine(this PipeWriter writer, Encoding encoding, string text)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        fixed (char* ptr = text)
        {
            var count = encoding.GetByteCount(ptr, text.Length);

            fixed (byte* b = writer.GetSpan(count + 2))
            {
                encoding.GetBytes(ptr, text.Length, b, count);

                b[count + 0] = 13;
                b[count + 1] = 10;
            }

            writer.Advance(count + 2);
        }
    }

    /// <summary>
    /// Write a reply to the client.
    /// </summary>
    /// <param name="writer">The writer to perform the operation on.</param>
    /// <param name="response">The response to write.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task which performs the operation.</returns>
    public static ValueTask<FlushResult> WriteReplyAsync(this PipeWriter writer, Response response, CancellationToken cancellationToken)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        writer.WriteLine($"{(int)response.ReplyCode} {response.Message}");

        return writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Try to find the first occurrance of a sequence in the given buffer.
    /// </summary>
    /// <param name="source">The source to find the sequence in.</param>
    /// <param name="sequence">The sequence to find in the source.</param>
    /// <param name="head">The position that the sequence was found.</param>
    /// <param name="tail">The position that the sequence ended.</param>
    /// <returns>Returns true if the sequence could be found, false if not.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryFind(this ReadOnlySequence<byte> source, ReadOnlySpan<byte> sequence, ref SequencePosition head, out SequencePosition tail)
    {
        tail = default;

        // move to the first span
        var position = head;

        if (TryMoveNext(ref source, ref position, out var span) == false)
        {
            return false;
        }

        var index = span.IndexOf(sequence);

        if (index != -1)
        {
            head = source.GetPosition(index, head);
            tail = source.GetPosition(sequence.Length, head);

            return true;
        }

        if (source.IsSingleSegment)
        {
            // nothing else can be done here
            return false;
        }

        while (true)
        {
            tail = position;

            // move to the next span
            if (TryMoveNext(ref source, ref position, out var next) == false)
            {
                return false;
            }

            if (TryMatchAcrossBoundary(ref span, ref next, ref sequence, out index))
            {
                head = source.GetPosition(index, head);
                tail = source.GetPosition(sequence.Length - (span.Length - index), tail);

                return true;
            }

            span = next;
            head = tail;

            index = span.IndexOf(sequence);

            if (index != -1)
            {
                head = source.GetPosition(index, head);
                tail = source.GetPosition(sequence.Length, head);

                return true;
            }
        }
    }

    static bool TryMatchAcrossBoundary(ref ReadOnlySpan<byte> previous, ref ReadOnlySpan<byte> next, ref ReadOnlySpan<byte> sequence, out int index)
    {
        // we will only call this if a complete match in the previous span isnt found 
        // so we only need to start matching from one byte short of the full sequence
        var partial = sequence[..^1];

        if (TryMatchEnd(ref previous, ref partial, out index))
        {
            partial = sequence[index..];

            if (next.StartsWith(partial))
            {
                // adjust the index to the position it was found in the previous span
                index = previous.Length - index;
                return true;
            }
        }

        return false;
    }

    static bool TryMatchEnd(ref ReadOnlySpan<byte> span, ref ReadOnlySpan<byte> sequence, out int index)
    {
        var partial = sequence;

        while (partial.Length > 0)
        {
            if (span.EndsWith(partial))
            {
                index = partial.Length;
                return true;
            }

            partial = partial[..^1];
        }

        index = default;
        return false;
    }

    static bool TryMoveNext(scoped ref ReadOnlySequence<byte> source, scoped ref SequencePosition position, out ReadOnlySpan<byte> span)
    {
        while (source.TryGet(ref position, out var memory, advance: true))
        {
            if (memory.Length > 0)
            {
                span = memory.Span;
                return true;
            }
        }

        span = default;
        return false;
    }

    internal static bool CaseInsensitiveStringEquals(this ReadOnlySequence<byte> buffer, ref Span<char> text)
    {
        if (buffer.IsSingleSegment)
        {
            var span = buffer.First.Span;

            return CaseInsensitiveStringEquals(ref span, ref text, 0);
        }

        var i = 0;
        var position = buffer.GetPosition(0);

        while (buffer.TryGet(ref position, out var memory, advance: true))
        {
            var span = memory.Span;

            if (CaseInsensitiveStringEquals(ref span, ref text, i) == false)
            {
                return false;
            }

            i += span.Length;
        }

        return i > 0;
    }

    static bool CaseInsensitiveStringEquals(ref ReadOnlySpan<byte> span, ref Span<char> text, int offset)
    {
        if (text.Length - offset != span.Length)
        {
            return false;
        }

        for (var i = 0; i < span.Length; i++)
        {
            var ch = (char)span[i];

            if (char.ToUpper(ch) != char.ToUpper(text[i + offset]))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsHex(this ref ReadOnlySpan<byte> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            if ((buffer[i] < 'a' || buffer[i] > 'f') && (buffer[i] < 'A' || buffer[i] > 'F'))
            {
                return false;
            }
        }

        return true;
    }
}