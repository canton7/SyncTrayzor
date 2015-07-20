using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Services
{
    public interface IIpcCommsServer : IDisposable
    {
        event EventHandler MainWindowShowRequested;
        void StartServer();
        void StopServer();
    }

    public class IpcCommsServer : IIpcCommsServer
    {
        private CancellationTokenSource cts;

        private const int CmdShowMainWindow = 2;

        public event EventHandler MainWindowShowRequested;

        public string PipeName
        {
            get { return $"SyncTrayzor-{Process.GetCurrentProcess().Id}"; }
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
                    this.OnMainWindowShowRequested();
                    return "OK";

                default:
                    return "UnknownCommand";
            }
        }

        private void OnMainWindowShowRequested()
        {
            this.MainWindowShowRequested?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            this.StopServer();
        }
    }
}
