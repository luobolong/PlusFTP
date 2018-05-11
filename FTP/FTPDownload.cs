using System.IO;
using System.Threading.Tasks;
using Hani.Utilities;

namespace Hani.FTP
{
    internal sealed partial class FTPClient
    {
        protected override async Task _getFileAsync()
        {
            FileStream fs = null;
            if (TransferEvent.Item.Status == ItemStatus.Resuming)
                fs = FileHelper.OpenWrite(TransferEvent.Item.Destination + TransferEvent.Item.ItemName);
            else fs = FileHelper.Create(TransferEvent.Item.Destination + TransferEvent.Item.ItemName);

            if (fs == null) { TransferEvent.Item.HasError = true; closeDataConnection(); return; }

            byte[] buffer;
            long tbytes = 0;
            int bytes = 0;

            if (TransferEvent.Item.Status == ItemStatus.Resuming)
            {
                if (FileHelper.Seek(fs, tbytes)) tbytes = fs.Length;
                else
                {
                    TransferEvent.Item.HasError = true;
                    if (fs != null) fs.Dispose();
                    closeDataConnection(); return;
                }
            }

            int code = await _commandRetrAsync(TransferEvent.Item.FullName, tbytes);
            if ((code == 0) && await retryAsync()) await _getFileAsync();
            if ((code != 150) && (code != 125) && (code != 226))
            {
                TransferEvent.Item.HasError = true;
                closeDataConnection();
                if (fs != null) fs.Dispose();
                if (tbytes == 0) FileHelper.Delete(TransferEvent.Item.Destination + TransferEvent.Item.ItemName);
                return;
            }

            await setupDataStreamAsync(true);

            try
            {
                while (true)
                {
                    if (Paused) while (Paused && !IsCanceled) { await Task.Delay(200); }
                    if (IsCanceled || FlagSkipIt) break;

                    int _bufferSize = BufferSize;
                    buffer = new byte[_bufferSize];

                    bytes = 0;

                    bytes = await DataStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytes == 0) break;
                    await fs.WriteAsync(buffer, 0, bytes);

                    tbytes += bytes;
                    TransferEvent.ItemSent = tbytes;
                    TransferEvent.Item.Transferred = TransferEvent.Item.Length - tbytes;
                }
            }
            catch { }

            if (fs != null) fs.Dispose();

            if (FlagSkipIt)
            {
                TransferEvent.Item.Status = ItemStatus.Ignored;
                FlagSkipIt = false;
                await commandAbortAsync();
                return;
            }
            else closeDataConnection();

            TransferEvent.Item.HasError = !((code == 226) || (await getResponseAsync() == 226));
            if (TransferEvent.Item.HasError && await retryAsync())
            {
                if (tbytes > 0)
                {
                    TransferEvent.Item.Status = ItemStatus.Resuming;
                    if (TransferEvent.Item.ItemFolder == BrowsedPath) TransferEvents.ItemStatusChanged(TransferEvent.Item);
                }
                await _getFileAsync();
            }
        }
    }
}