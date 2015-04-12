using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Utils
{
    public struct CopyToAsyncProgress
    {
        public long BytesRead { get; private set; }
        public long TotalBytesToRead { get; private set; }
        public int ProgressPercent { get; private set; }

        public CopyToAsyncProgress(long bytesRead, long totalBytesToRead)
            : this()
        {
            this.BytesRead = bytesRead;
            this.TotalBytesToRead = totalBytesToRead;

            if (this.TotalBytesToRead > 0)
                this.ProgressPercent = (int)((this.BytesRead * 100) / this.TotalBytesToRead);
            else
                this.ProgressPercent = -1;
        }
    }

    public static class StreamExtensions
    {
        public static async Task CopyToAsync(this Stream source, Stream destination, IProgress<CopyToAsyncProgress> progress)
        {
            var buffer = new byte[81920];
            var totalBytesToRead = source.CanSeek ? source.Length : -1;
            long totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                progress.Report(new CopyToAsyncProgress(totalBytesRead, totalBytesToRead));
            }
        }
    }
}
