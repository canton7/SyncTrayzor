using NLog;
using System;
using System.Diagnostics;

namespace SyncTrayzor.Services
{
    public interface ISingleApplicationInstanceManager : IDisposable
    {
        bool ShouldExit();
        void StartServer();
    }

    public class SingleApplicationInstanceManager : ISingleApplicationInstanceManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IIpcCommsServer server;
        private readonly IIpcCommsClient client;
        private readonly IAssemblyProvider assemblyProvider;
        private readonly IApplicationWindowState windowState;

        public SingleApplicationInstanceManager(IIpcCommsServer server, IIpcCommsClient client, IAssemblyProvider assemblyProvider, IApplicationWindowState windowState)
        {
            this.server = server;
            this.client = client;
            this.assemblyProvider = assemblyProvider;
            this.windowState = windowState;
        }

        public bool ShouldExit()
        {
            var ourLocation = this.assemblyProvider.Location;
            var ourId = Process.GetCurrentProcess().Id;
            var processes = Process.GetProcessesByName("SyncTrayzor");
            logger.Info("Checking for other SyncTrayzor processes");
            foreach (var process in processes)
            {
                try
                {
                    if (String.Equals(process.MainModule.FileName, ourLocation, StringComparison.OrdinalIgnoreCase) && process.Id != ourId)
                    {
                        logger.Info("Found process with ID {0} and location {1}. Asking it to show its main window...", process.Id, process.MainModule.FileName);
                        try
                        {
                            this.client.RequestShowMainWindow(process.Id);
                            logger.Info("Process responded successfully. Indicating close");
                            return true;
                        }
                        catch (Exception e)
                        {
                            logger.Error("Process produced an error", e);
                        }
                    }
                    else if (process.Id != ourId)
                    {
                        logger.Info("Found process with ID {0} and location {1}, but it's a different exe (we are {2})", process.Id, process.MainModule.FileName, ourLocation);
                    }
                }
                catch (Exception e)
                {
                    logger.Error($"Error accessing information for process with PID { process.Id}", e);
                }
            }

            logger.Info("Did not find any other processes, or they all responded with an error. Indicating no close");
            return false;
        }


        public void StartServer()
        {
            this.server.MainWindowShowRequested += this.MainWindowShowRequested;
            this.server.StartServer();
        }

        private void MainWindowShowRequested(object sender, EventArgs e)
        {
            this.windowState.EnsureInForeground();
        }

        public void Dispose()
        {
            if (this.server != null)
                this.server.Dispose();
        }
    }
}
