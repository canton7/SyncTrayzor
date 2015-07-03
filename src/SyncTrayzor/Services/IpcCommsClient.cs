using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services
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
        void RequestShowMainWindow(int pid);
    }

    public class IpcCommsClient : IIpcCommsClient
    {
        private string PipeNameForPid(int pid)
        {
            return String.Format("SyncTrayzor-{0}", pid);
        }

        public void RequestShowMainWindow(int pid)
        {
            this.SendCommand(pid, "ShowMainWindow");
        }

        private void SendCommand(int pid, string command)
        {
            var clientStream = new NamedPipeClientStream(".", this.PipeNameForPid(pid), PipeDirection.InOut, PipeOptions.Asynchronous);
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

            throw new UnknownIpcCommandException(String.Format("Remote side replied with {0}", response));
        }
    }
}
