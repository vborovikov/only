namespace Only;

using System;
using System.Threading;
#if NET462
using System.Deployment.Application;
#else
using System.Web;
#endif
using System.Windows;

/// <summary>
/// Encapsulates an application which can be run as a single instance.
/// </summary>
public class InstanceAwareApp : Application, IDisposable
{
    private bool isDisposed = false; // To detect redundant calls

    private Mutex? singleInstanceMutex;
    private SingleInstanceService? singleInstanceRemoteService;

    /// <summary>
    /// Disposes managed and unmanaged resources.
    /// </summary>
    ~InstanceAwareApp()
    {
        Dispose(disposing: false);
    }

    /// <summary>
    /// Runs the application as a single instance.
    /// </summary>
    /// <param name="window"></param>
    /// <returns></returns>
    public int RunSingle(Window? window = null)
    {
        var applicationIdentifier = $"{this.GetType().Assembly.GetName().Name}:{Environment.UserName}";

        // Create mutex based on unique application Id to check if this is the first instance of the application.
        this.singleInstanceMutex = new Mutex(true, applicationIdentifier, out var firstInstance);
        if (firstInstance)
        {
            this.singleInstanceRemoteService = SingleInstanceService.Create(applicationIdentifier);
            return Run(window);
        }
        else
        {
            var commandLineArgs = GetCommandLineArgs();
            SingleInstanceService.SignalFirstInstance(applicationIdentifier, commandLineArgs);
            return 0;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    internal void NotifyInstantiated(string[] args)
    {
        OnInstantiated(args);
    }

    /// <summary>
    /// Disposes managed and unmanaged resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.isDisposed)
        {
            if (this.singleInstanceMutex != null)
            {
                this.singleInstanceMutex.Dispose();
                this.singleInstanceMutex = null;
            }
            if (this.singleInstanceRemoteService != null)
            {
                this.singleInstanceRemoteService.Dispose();
                this.singleInstanceRemoteService = null;
            }

            this.isDisposed = true;
        }
    }

    /// <summary>
    /// Handles the activation of the first instance of the application.
    /// </summary>
    /// <param name="args"></param>
    protected virtual void OnInstantiated(string[] args)
    {
        if (this.MainWindow.IsVisible == false)
        {
            this.MainWindow.Show();
        }

        if (this.MainWindow.WindowState == WindowState.Minimized)
        {
            this.MainWindow.WindowState = WindowState.Normal;
        }

        this.MainWindow.Activate();
        if (this.MainWindow.Topmost == false)
        {
            this.MainWindow.Topmost = true;
            this.MainWindow.Topmost = false;
        }
        this.MainWindow.Focus();
    }

    /// <summary>
    /// Gets command line arguments.
    /// </summary>
    /// <returns>List of command line argument values.</returns>
    /// <remarks>
    /// For ClickOnce deployed applications, command line arguments may not be passed directly, they have to be retrieved.
    /// </remarks>
    private static string[] GetCommandLineArgs()
    {
        string[] args = [];

#if NET462
        if (AppDomain.CurrentDomain.ActivationContext == null)
        {
            // The application was not ClickOnce deployed, get arguments from standard APIs
            // Skip the executable file name
            args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        }
        else if (ApplicationDeployment.IsNetworkDeployed)
        {
            // The application was ClickOnce deployed
            // ClickOnce deployed apps cannot receive traditional command line arguments

            if (AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null)
            {
                args = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
            }
        }
#else
        if (Environment.GetEnvironmentVariable("ClickOnce_IsNetworkDeployed") is string isNetworkDeployedVar &&
            bool.TryParse(isNetworkDeployedVar, out var isNetworkDeployed) && isNetworkDeployed)
        {
            // The application was ClickOnce deployed
            // ClickOnce deployed apps cannot receive traditional command line arguments

            if (Environment.GetEnvironmentVariable("ClickOnce_ActivationUri") is string activationUriVar &&
                Uri.TryCreate(activationUriVar, UriKind.Absolute, out var activationUri) &&
                !string.IsNullOrWhiteSpace(activationUri.Query))
            {
                var queryParams = HttpUtility.ParseQueryString(activationUri.Query);
                var argList = new List<string>();
                foreach (var key in queryParams.AllKeys)
                {
                    if (key is null)
                        continue;

                    argList.Add(key);

                    var values = queryParams.GetValues(key);
                    if (values is null)
                        continue;

                    argList.AddRange(values);
                }

                args = [.. argList];
            }
        }
        else
        {
            // The application was not ClickOnce deployed, get arguments from standard APIs
            // Skip the executable file name
            args = Environment.GetCommandLineArgs()[1..];
        }
#endif

        return args;
    }
}