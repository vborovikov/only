namespace Only;

using System;
using System.Windows;
using System.Windows.Threading;

abstract class SingleInstanceService : MarshalByRefObject, IDisposable
{
    /// <summary>
    /// Creates a remote service for communication.
    /// </summary>
    /// <param name="appId">Application identifier</param>
    public static SingleInstanceService Create(string appId)
    {
        return new SingleInstanceRemoteService(appId);
    }

    /// <summary>
    /// Calls a function of the remoting service class from the second instance to the first and cause it to activate itself.
    /// </summary>
    /// <param name="appId">Application identifier</param>
    /// <param name="args">
    /// Command line arguments for the second instance, passed to the first instance to take appropriate action.
    /// </param>
    public static void SignalFirstInstance(string appId, string[] args)
    {
        SingleInstanceRemoteService.SignalFirstInstance(appId, args);
    }

    /// <summary>
    /// Activates the first instance of the application.
    /// </summary>
    protected void InvokeFirstInstance(string[] args)
    {
        // Do an asynchronous call to ActivateFirstInstance function
        Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
            (object args) => ActivateFirstInstance((string[])args), args);
    }

    /// <summary>
    /// Activates the first instance of the application with arguments from a second instance.
    /// </summary>
    /// <param name="args">List of arguments to supply the first instance of the application.</param>
    private static void ActivateFirstInstance(string[] args)
    {
        // Set main window state and process command line args
        if (Application.Current is InstanceAwareApp app)
        {
            app.NotifyInstantiated(args);
        }
    }

    /// <summary>
    /// Remoting Object's ease expires after every 5 minutes by default. We need to override the InitializeLifetimeService class
    /// to ensure that lease never expires.
    /// </summary>
    /// <returns>Always null.</returns>
    [Obsolete]
    public sealed override object InitializeLifetimeService()
    {
        return null!;
    }

    #region IDisposable

    private bool isDisposed;

    ~SingleInstanceService()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed state (managed objects).
    /// </summary>
    protected virtual void DisposeManaged() { }

    /// <summary>
    /// Frees unmanaged resources (unmanaged objects). Sets large fields to null.
    /// </summary>
    protected virtual void DisposeUnmanaged() { }

    private void Dispose(bool disposing)
    {
        if (!this.isDisposed)
        {
            if (disposing)
            {
                DisposeManaged();
            }
            DisposeUnmanaged();

            this.isDisposed = true;
        }
    }

    #endregion IDisposable
}
