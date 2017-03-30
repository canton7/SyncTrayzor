using NLog;
using SyncTrayzor.Syncthing;
using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.Ipc
{
    public interface IIpcCommsServer : IDisposable
    {
        void StartServer();
        void StopServer();
    }

    public class IpcCommsServer : IIpcCommsServer
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly ISyncthingManager syncthingManager;
        private readonly IApplicationWindowState windowState;

        private CancellationTokenSource cts;

        public string PipeName =>  $"SyncTrayzor-{Process.GetCurrentProcess().Id}";

        public IpcCommsServer(ISyncthingManager syncthingManager, IApplicationWindowState windowState)
        {
            this.syncthingManager = syncthingManager;
            this.windowState = windowState;
        }

        public void StartServer()
        {
            if (this.cts != null)
                return;

            this.cts = new CancellationTokenSource();
            this.StartInternal(cts.Token);
        }

        public void StopServer()
        {
            if (this.cts != null)
                this.cts.Cancel();
        }

        private async void StartInternal(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[256];
            var commandBuilder = new StringBuilder();

            var serverStream = new NamedPipeServerStream(this.PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 0, 0);

            using (cancellationToken.Register(() => serverStream.Close()))
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Factory.FromAsync(serverStream.BeginWaitForConnection, serverStream.EndWaitForConnection, TaskCreationOptions.None);

                    int read = await serverStream.ReadAsync(buffer, 0, buffer.Length);
                    commandBuilder.Append(Encoding.ASCII.GetString(buffer, 0, read));

                    while (!serverStream.IsMessageComplete)
                    {
                        read = serverStream.Read(buffer, 0, buffer.Length);
                        commandBuilder.Append(Encoding.ASCII.GetString(buffer, 0, read));
                    }

                    var response = this.HandleReceivedCommand(commandBuilder.ToString());
                    var responseBytes = Encoding.ASCII.GetBytes(response);
                    serverStream.Write(responseBytes, 0, responseBytes.Length);

                    serverStream.WaitForPipeDrain();
                    serverStream.Disconnect();

                    commandBuilder.Clear();
                }
            }
        }

        private string HandleReceivedCommand(string command)
        {
            switch (command)
            {
                case "ShowMainWindow":
                    this.ShowMainWindow();
                    return "OK";

                case "StartSyncthing":
                    this.StartSyncthing();
                    return "OK";

                case "StopSyncthing":
                    this.StopSyncthing();
                    return "OK";

                default:
                    return "UnknownCommand";
            }
        }

        private void ShowMainWindow()
        {
            this.windowState.EnsureInForeground();
        }

        private async void StartSyncthing()
        {
            if (this.syncthingManager.State == SyncthingState.Stopped)
            {
                logger.Debug("IPC client requested Syncthing start, so starting");

                try
                {
                    await this.syncthingManager.StartAsync();
                }
                catch (Exception e)
                {
                    logger.Error(e, "Failed to start syncthing");
                }
            }
            else
            {
                logger.Debug($"IPC client requested Syncthing start, but its state is {this.syncthingManager.State}, so not starting");
            }
        }

        private async void StopSyncthing()
        {
            if (this.syncthingManager.State == SyncthingState.Running)
            {
                logger.Debug("IPC client requested Syncthing stop, so stopping");

                try
                {
                    await this.syncthingManager.StopAsync();
                }
                catch (Exception e)
                {
                    logger.Error(e, "Failed to stop Syncthing");
                }
            }
            else
            {
                logger.Debug($"IPC client requested Syncthing stop, but its state is {this.syncthingManager.State}, so not stopping");
            }
        }

        public void Dispose()
        {
            this.StopServer();
        }
    }
}
