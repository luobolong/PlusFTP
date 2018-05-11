using System.IO;
using System.Threading.Tasks;
using Hani.Utilities;

namespace Hani.FTP
{
    internal sealed partial class FTPClient
    {
        protected override async Task _sendFileAsync(string source)
        {
            FileStream fs = null;
            fs = FileHelper.OpenRead(source);
            if (fs == null) { TransferEvent.Item.HasError = true; return; }

            byte[] buffer;
            long tbytes = 0;
            int bytes = 0;
            bool connectionOpend = false;

            if (TransferEvent.Item.Status == ItemStatus.Resuming)
            {
                tbytes = await _commandAppeAsync(TransferEvent.Item.FullName);
                connectionOpend = (tbytes > 0);

                if (connectionOpend && !FileHelper.Seek(fs, tbytes))
                {
                    TransferEvent.Item.HasError = true;
                    if (fs != null) fs.Dispose(); return;
                }
            }
            else connectionOpend = await _commandStorAsync(TransferEvent.Item.FullName);

            if (connectionOpend)
            {
                await setupDataStreamAsync(false);
                try
                {
                    while (true)
                    {
                        if (Paused) while (Paused && !IsCanceled) { await Task.Delay(200); }
                        if (IsCanceled || FlagSkipIt) break;

                        int _bufferSize = BufferSize;
                        buffer = new byte[_bufferSize];

                        bytes = 0;

                        bytes = await fs.ReadAsync(buffer, 0, buffer.Length);
                        if (bytes == 0) break;
                        await DataStream.WriteAsync(buffer, 0, bytes);

                        tbytes += bytes;
                        TransferEvent.ItemSent = tbytes;
                        TransferEvent.Item.Transferred = tbytes;

                        if (tbytes == fs.Length) break;
                    }
                }
                catch { }
            }
            if (fs != null) fs.Dispose();

            if (FlagSkipIt)
            {
                TransferEvent.Item.Status = ItemStatus.Ignored;
                FlagSkipIt = false;
                await commandAbortAsync();
                return;
            }
            else closeDataConnection();

            TransferEvent.Item.HasError = (!connectionOpend || !(await getResponseAsync() == 226));
            if (TransferEvent.Item.HasError && await retryAsync())
            {
                if (tbytes > 0)
                {
                    TransferEvent.Item.Status = ItemStatus.Resuming;
                    if (TransferEvent.Item.Destination == BrowsedPath) TransferEvents.ItemStatusChanged(TransferEvent.Item);
                }
                await _sendFileAsync(source);
            }
        }
    }
}