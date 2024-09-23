#if NET462

namespace Only;

using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;

/// <summary>
/// Remoting service class which is exposed by the server i.e the first instance and called by the second instance
/// to cause the first instance to activate itself.
/// </summary>
internal class SingleInstanceRemoteService : SingleInstanceService
{
    /// <summary>
    /// Remote service name.
    /// </summary>
    private const string RemoteServiceName = "SingleInstanceApplicationService";

    /// <summary>
    /// IPC protocol used (string).
    /// </summary>
    private const string IpcProtocol = "ipc://";

    /// <summary>
    /// Suffix to the channel name.
    /// </summary>
    private const string ChannelNameSuffix = "SingeInstanceIpcChannel";

    /// <summary>
    /// String delimiter used in channel names.
    /// </summary>
    private const string Delimiter = ":";

    private IpcServerChannel channel;

    /// <summary>
    /// Creates a remote service for communication.
    /// </summary>
    /// <param name="appId">Application identifier</param>
    public SingleInstanceRemoteService(string appId)
    {
        // Application's IPC channel name
        var channelName = GetChannelName(appId);
        var serverProvider = new BinaryServerFormatterSinkProvider
        {
            TypeFilterLevel = TypeFilterLevel.Full
        };
        var props = new Dictionary<string, string>
        {
            ["name"] = channelName,
            ["portName"] = channelName,
            ["exclusiveAddressUse"] = "false"
        };

        // Create the IPC Server channel with the channel properties
        this.channel = new IpcServerChannel(props, serverProvider);
        // Register the channel with the channel services
        ChannelServices.RegisterChannel(channel, true);
        // Expose the remote service with the REMOTE_SERVICE_NAME
        RemotingServices.Marshal(this, RemoteServiceName);
    }

    /// <summary>
    /// Creates a client channel and obtains a reference to the remoting service exposed by the server -
    /// in this case, the remoting service exposed by the first instance. Calls a function of the remoting service
    /// class from the second instance to the first and cause it to activate itself.
    /// </summary>
    /// <param name="appId">Application identifier</param>
    /// <param name="args">
    /// Command line arguments for the second instance, passed to the first instance to take appropriate action.
    /// </param>
    public new static void SignalFirstInstance(string appId, string[] args)
    {
        var secondInstanceChannel = new IpcClientChannel();
        ChannelServices.RegisterChannel(secondInstanceChannel, true);

        var channelName = GetChannelName(appId);
        var remotingServiceUrl = IpcProtocol + channelName + "/" + RemoteServiceName;
        // Obtain a reference to the remoting service exposed by the server i.e the first instance of the application
        var firstInstanceRemoteServiceReference = (SingleInstanceRemoteService)RemotingServices.Connect(typeof(SingleInstanceRemoteService), remotingServiceUrl);

        // Check that the remote service exists, in some cases the first instance may not yet have created one, in which case
        // the second instance should just exit
        if (firstInstanceRemoteServiceReference != null)
        {
            // Invoke a method of the remote service exposed by the first instance passing on the command line
            // arguments and causing the first instance to activate itself
            firstInstanceRemoteServiceReference.InvokeFirstInstance(args);
        }
    }

    protected override void DisposeUnmanaged()
    {
        if (this.channel != null)
        {
            ChannelServices.UnregisterChannel(this.channel);
            this.channel = null!;
        }
    }

    /// <summary>
    /// Gets application's IPC channel name.
    /// </summary>
    /// <param name="appId">Application identifier</param>
    /// <returns>Application's IPC channel name.</returns>
    private static string GetChannelName(string appId)
    {
        return String.Concat(appId, Delimiter, ChannelNameSuffix);
    }
}

#endif // .NET 4.6.2