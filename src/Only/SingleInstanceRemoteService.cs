#if NET462

namespace Only;

using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Windows;
using System.Windows.Threading;

/// <summary>
/// Remoting service class which is exposed by the server i.e the first instance and called by the second instance
/// to cause the first instance to activate itself.
/// </summary>
internal class SingleInstanceRemoteService : MarshalByRefObject, IDisposable
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

    private bool isDisposed = false;
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

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~SingleInstanceRemoteService()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(false);
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
    public static void SignalFirstInstance(string appId, string[] args)
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

    /// <summary>
    /// Remoting Object's ease expires after every 5 minutes by default. We need to override the InitializeLifetimeService class
    /// to ensure that lease never expires.
    /// </summary>
    /// <returns>Always null.</returns>
    public override object InitializeLifetimeService()
    {
        return null!;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Activates the first instance of the application.
    /// </summary>
    public void InvokeFirstInstance(string[] args)
    {
        // Do an asynchronous call to ActivateFirstInstance function
        Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Normal, 
            new DispatcherOperationCallback(ActivateFirstInstanceCallback), args);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.isDisposed)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects).
            }

            if (this.channel != null)
            {
                ChannelServices.UnregisterChannel(this.channel);
                this.channel = null!;
            }

            this.isDisposed = true;
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

    /// <summary>
    /// Callback for activating first instance of the application.
    /// </summary>
    /// <param name="arg">Callback argument.</param>
    /// <returns>Always null.</returns>
    private static object ActivateFirstInstanceCallback(object arg)
    {
        // Get command line args to be passed to first instance
        var args = (string[])arg;
        ActivateFirstInstance(args);
        return null!;
    }

    /// <summary>
    /// Activates the first instance of the application with arguments from a second instance.
    /// </summary>
    /// <param name="args">List of arguments to supply the first instance of the application.</param>
    private static void ActivateFirstInstance(string[] args)
    {
        // Set main window state and process command line args
        if (Application.Current == null)
        {
            return;
        }

        (Application.Current as InstanceAwareApp)?.NotifyInstantiated(args);
    }
}

#endif // .NET 4.6.2