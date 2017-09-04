using NLog;
using System;
using System.Diagnostics;

namespace SyncTrayzor.Services.Ipc
{
    public interface IIpcCommsClientFactory
    {
        IIpcCommsClient TryCreateClient();
    }

    public class IpcCommsClientFactory : IIpcCommsClientFactory
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly IAssemblyProvider assemblyProvider;

        public IpcCommsClientFactory(IAssemblyProvider assemblyProvider)
        {
            this.assemblyProvider = assemblyProvider;
        }

        public IIpcCommsClient TryCreateClient()
        {
            var ourLocation = this.assemblyProvider.Location;
            var ourProcess = Process.GetCurrentProcess();
            var processes = Process.GetProcessesByName("SyncTrayzor");
            logger.Debug("Checking for other SyncTrayzor processes");
            foreach (var process in processes)
            {
                try
                {
                    // Only care if the process came from our session: allow multiple instances under different users
                    if (String.Equals(process.MainModule.FileName, ourLocation, StringComparison.OrdinalIgnoreCase) && process.SessionId == ourProcess.SessionId && process.Id != ourProcess.Id)
                    {
                        logger.Info("Found process with ID {0} and location {1}", process.Id, process.MainModule.FileName);
                        return new IpcCommsClient(process.Id);
                    }
                    else if (process.Id != ourProcess.Id)
                    {
                        logger.Debug("Found process with ID {0} and location {1}, but it's a different exe (we are {2})", process.Id, process.MainModule.FileName, ourLocation);
                    }
                }
                catch (Exception e)
                {
                    logger.Warn(e, $"Error accessing information for process with PID {process.Id}");
                }
            }

            logger.Debug("Did not find any other processes, or they all responded with an error");
            return null;
        }
    }
}
