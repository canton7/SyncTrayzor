using System;
using System.IO.Pipes;
using System.Text;

namespace SyncTrayzor.Services.Ipc
{
    public class UnknownIpcCommandException : Exception
    {
        public UnknownIpcCommandException(string message)
            : base(message)
        {
        }
    }

    public interface IIpcCommsClient
    {
        void ShowMainWindow();
        void StartSyncthing();
        void StopSyncthing();
    }

    public class IpcCommsClient : IIpcCommsClient
    {
        private readonly int pid;

        private string PipeName => $"SyncTrayzor-{this.pid}";

        public IpcCommsClient(int pid)
        {
            this.pid = pid;
        }

        public void ShowMainWindow()
        {
            this.SendCommand("ShowMainWindow");
        }

        public void StartSyncthing()
        {
            this.SendCommand("StartSyncthing");
        }

        public void StopSyncthing()
        {
            this.SendCommand("StopSyncthing");
        }

        private void SendCommand(string command)
        {
            var clientStream = new NamedPipeClientStream(".", this.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            clientStream.Connect(500);
            clientStream.ReadMode = PipeTransmissionMode.Message;
            var commandBytes = Encoding.ASCII.GetBytes(command);
            byte[] buffer = new byte[256];
            var responseBuilder = new StringBuilder();

            clientStream.Write(commandBytes, 0, commandBytes.Length);

            int read = clientStream.Read(buffer, 0, buffer.Length);
            responseBuilder.Append(Encoding.ASCII.GetString(buffer, 0, read));

            while (!clientStream.IsMessageComplete)
            {
                read = clientStream.Read(buffer, 0, buffer.Length);
                responseBuilder.Append(Encoding.ASCII.GetString(buffer, 0, read));
            }

            this.ProcessResponse(responseBuilder.ToString());
        }

        private void ProcessResponse(string response)
        {
            if (response == "OK")
                return;

            throw new UnknownIpcCommandException($"Remote side replied with {response}");
        }

        public override string ToString()
        {
            return $"<IpcCommsClient PID={this.pid}>";
        }
    }
}
