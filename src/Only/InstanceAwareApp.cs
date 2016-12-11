namespace Only
{
    using System;
    using System.Collections.Generic;
    using System.Deployment.Application;
    using System.Linq;
    using System.Threading;
    using System.Windows;

    public class InstanceAwareApp : Application, IDisposable
    {
        private bool isDisposed = false; // To detect redundant calls

        private Mutex singleInstanceMutex;
        private SingleInstanceRemoteService singleInstanceRemoteService;

        ~InstanceAwareApp()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(disposing: false);
        }

        public int RunSingle(Window window = null)
        {
            var applicationIdentifier = this.GetType().Assembly.GetName().Name + Environment.UserName;

            // Create mutex based on unique application Id to check if this is the first instance of the application.
            bool firstInstance;
            this.singleInstanceMutex = new Mutex(true, applicationIdentifier, out firstInstance);
            if (firstInstance)
            {
                this.singleInstanceRemoteService = new SingleInstanceRemoteService(applicationIdentifier);
                return Run(window);
            }
            else
            {
                var commandLineArgs = GetCommandLineArgs();
                SingleInstanceRemoteService.SignalFirstInstance(applicationIdentifier, commandLineArgs);
                return 0;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void NotifyInstantiated(string[] args)
        {
            OnInstantiated(args);
        }

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
        /// Gets command line arguments - for ClickOnce deployed applications,
        /// command line arguments may not be passed directly, they have to be retrieved.
        /// </summary>
        /// <returns>List of command line argument strings.</returns>
        private static string[] GetCommandLineArgs()
        {
            IEnumerable<string> args = null;
            if (AppDomain.CurrentDomain.ActivationContext == null)
            {
                // The application was not ClickOnce deployed, get arguments from standard APIs
                // Skip the executable file name
                args = Environment.GetCommandLineArgs().Skip(1);
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

            if (args == null)
            {
                args = Enumerable.Empty<string>();
            }

            return args.ToArray();
        }
    }
}