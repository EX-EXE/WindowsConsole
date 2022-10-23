
using Internal;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using System.Threading.Channels;
using static Internal.NativeMethod;

public static partial class WindowsConsole
{
    private static readonly uint BufferSize = 128;
    public static async ValueTask<char> ReadAsync(
        bool echo,
        CancellationToken cancellationToken = default)
    {
        // Token
        cancellationToken.ThrowIfCancellationRequested();
        var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var linkedCancellationToken = linkedCancellationTokenSource.Token;

        // Channel
        var channelOption = new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
        };
        var channel = Channel.CreateBounded<InputKeyInfo>(channelOption);
        var input = (char)0;
        var readTask = Task.Run(async () =>
        {
            try
            {
                while (await channel.Reader.WaitToReadAsync(linkedCancellationToken).ConfigureAwait(false))
                {
                    await foreach (var keyInfo in channel.Reader.ReadAllAsync(linkedCancellationToken).ConfigureAwait(false))
                    {
                        linkedCancellationToken.ThrowIfCancellationRequested();
                        if (keyInfo.InputKey != '\0')
                        {
                            if (echo)
                            {
                                Console.Write(keyInfo.InputKey);
                            }
                            input = keyInfo.InputKey;
                            return;
                        }
                    }
                }
            }
            finally
            {
                linkedCancellationTokenSource.Cancel();
            }
        });
        var writeTask = CoreReadKeyAsync(channel.Writer, linkedCancellationToken);

        await Task.WhenAll(readTask, writeTask).ConfigureAwait(false);
        return input;
    }

    public static async ValueTask<string> ReadLineAsync(
        bool echo,
        Func<InputKeyInfo, bool>? cancelInputFunc = null,
        IProgress<InputKeyInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Token
        cancellationToken.ThrowIfCancellationRequested();
        var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var linkedCancellationToken = linkedCancellationTokenSource.Token;

        // Channel
        var channelOption = new BoundedChannelOptions((int)BufferSize)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
        };
        var channel = Channel.CreateBounded<InputKeyInfo>(channelOption);
        var builder = new StringBuilder();
        var readTask = Task.Run(async () =>
        {
            try
            {
                while (await channel.Reader.WaitToReadAsync(linkedCancellationToken).ConfigureAwait(false))
                {
                    await foreach (var keyInfo in channel.Reader.ReadAllAsync(linkedCancellationToken).ConfigureAwait(false))
                    {
                        linkedCancellationToken.ThrowIfCancellationRequested();

                        if (cancelInputFunc != null && cancelInputFunc.Invoke(keyInfo))
                        {
                            continue;
                        }
                        progress?.Report(keyInfo);

                        if (keyInfo.InputKey != '\0')
                        {
                            if (echo)
                            {
                                Console.Write(keyInfo.InputKey);
                            }

                            if (keyInfo.InputKey == '\r')
                            {
                                return;
                            }
                            else
                            {
                                builder.Append(keyInfo.InputKey);
                            }
                        }
                    }
                }
            }
            finally
            {
                linkedCancellationTokenSource.Cancel();
            }
        });
        var writeTask = CoreReadKeyAsync(channel.Writer, linkedCancellationToken);

        await Task.WhenAll(readTask, writeTask).ConfigureAwait(false);

        return builder.ToString();
    }

    private static async Task CoreReadKeyAsync(ChannelWriter<InputKeyInfo> channelWriter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var handle = NativeMethod.GetStdHandle((int)NativeMethod.StandardHandle.STD_INPUT_HANDLE);
        NativeMethod.SetConsoleMode(handle, 0);

        var bufferRecord = new INPUT_RECORD[BufferSize];
        uint readSize = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (NativeMethod.PeekConsoleInput(handle, bufferRecord, BufferSize, out readSize) && 0 < readSize)
            {
                if (NativeMethod.ReadConsoleInput(handle, bufferRecord, BufferSize, out readSize))
                {
                    foreach (var record in bufferRecord.Take((int)readSize))
                    {
#if DEBUG
                        //Console.WriteLine($"ReadEvent : KeyDown({record.KeyEvent.bKeyDown}) EventType({record.EventType}) UnicodeChar({Convert.ToInt32(record.KeyEvent.UnicodeChar):X}) VirtualKeyCode({record.KeyEvent.wVirtualKeyCode})");
#endif
                        if (record.KeyEvent.bKeyDown && record.EventType == 1)
                        {
                            var keyInfo = new InputKeyInfo(record.KeyEvent.UnicodeChar, record.KeyEvent.wVirtualKeyCode, record.KeyEvent.dwControlKeyState);
                            await channelWriter.WriteAsync(keyInfo, cancellationToken).ConfigureAwait(false);
                        }
                    }

                }
            }
            Thread.Yield();
        }
    }
}