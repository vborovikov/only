#if NET8_0_OR_GREATER

namespace Only;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;

internal class SingleInstanceRemoteService : SingleInstanceService
{
    private readonly string channelName;
    private NamedPipeServerStream serverStream;

    public SingleInstanceRemoteService(string appId)
    {
        this.channelName = GetChannelName(appId);
        Restart();
    }

    [MemberNotNull(nameof(serverStream))]
    private void Restart()
    {
        Debug.WriteLine($"Restart: {this.channelName}");

        this.serverStream?.Close();
        this.serverStream?.Dispose();

        this.serverStream = new NamedPipeServerStream(this.channelName,
            PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte);
        this.serverStream.BeginWaitForConnection(HandleClientConnected, this);
    }

    protected override void DisposeManaged()
    {
        this.serverStream.Dispose();
    }

    /// <summary>
    /// Calls a function of the remoting service class from the second instance to the first and cause it to activate itself.
    /// </summary>
    /// <param name="appId">Application identifier</param>
    /// <param name="args">Command line arguments for the second instance, passed to the first instance to take appropriate action.</param>
    public new static void SignalFirstInstance(string appId, string[] args)
    {
        Debug.WriteLine($"SignalFirstInstance: {appId} [{string.Join(" ", args)}]");

        using var client = new NamedPipeClientStream(".", GetChannelName(appId), PipeDirection.Out);
        client.Connect();
        var channel = new ArgsChannel(client);
        channel.WriteArgs(args);
        client.Close();
    }

    private static void HandleClientConnected(IAsyncResult ar)
    {
        if (ar.AsyncState is not SingleInstanceRemoteService service)
            throw new InvalidOperationException($"AsyncState is not {nameof(SingleInstanceRemoteService)}");

        try
        {
            service.serverStream.EndWaitForConnection(ar);
            service.OnClientConnected();
        }
        finally
        {
            service.Restart();
        }
    }

    private void OnClientConnected()
    {
        var channel = new ArgsChannel(this.serverStream);
        var args = channel.ReadArgs();
        Debug.WriteLine($"OnClientConnected: [{string.Join(" ", args)}]");
        InvokeFirstInstance(args);
    }

    private static string GetChannelName(string appId)
    {
        return string.Concat(appId, "-", "Pipe");
    }

    private readonly struct ArgsChannel
    {
        private readonly PipeStream stream;

        public ArgsChannel(PipeStream stream)
        {
            this.stream = stream;
        }

        public string[] ReadArgs()
        {
            var args = new List<string>();

            using var reader = new StreamReader(this.stream);
            while (reader.Peek() > 0 && reader.ReadLine() is { Length: > 0 } arg)
            {
                args.Add(arg);
            }

            return args.ToArray();
        }

        public void WriteArgs(string[] args)
        {
            using var writer = new StreamWriter(this.stream);
            foreach (var arg in args)
            {
                writer.WriteLine(arg);
            }
            writer.WriteLine();
            writer.Flush();
            this.stream.WaitForPipeDrain();
        }
    }
}

#endif