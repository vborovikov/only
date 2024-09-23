#if NET8_0_OR_GREATER

namespace Only;

internal class SingleInstanceRemoteService : SingleInstanceService
{
    public SingleInstanceRemoteService(string appId)
    {
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
    }
}

#endif