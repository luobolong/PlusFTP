using System;
using System.Threading.Tasks;
using Hani.Utilities;

namespace Hani.FTP
{
    internal sealed partial class FTPClient
    {
        private async Task<int> _commandUserAsync(string user)
        {
            return await executeCommandAsync("USER", user);
        }

        private async Task<bool> _commandPassAsync(string password)
        {
            return (await executeCommandAsync("PASS", CryptoHashing.Decrypt(password)) == 230);
        }

        private async Task _commandQuitAsync(bool getResponse = true)
        {
            await commandAbortAsync();

            if (ControlStream != null)
            {
                ControlStream.WriteTimeout = 500;
                ControlStream.ReadTimeout = 500;
            }

            if (IsChild) parent.BufferSize = BufferSize;

            await executeCommandAsync("QUIT", string.Empty, getResponse, true);
        }

        private async Task<bool> _commandPwdAsync()
        {
            if (!HomePath.NullEmpty()) return true;

            storeResponse = true;
            if (await executeCommandAsync("PWD") == 257)
            {
                try
                {
                    string path = tmpResponsed.Split('"')[1].Trim();
                    tmpResponsed = null;

                    if (!path.NullEmpty())
                    {
                        PathHelper.AddEndningSlash(ref path);
                        BrowsedPath = HomePath = path;
                        return true;
                    }
                }
                catch (Exception exp) { ExceptionHelper.Log(exp); }
            }

            return false;
        }

        private async Task<bool> _commandFeatAsync()
        {
            if (serverFeat != null) return (serverFeat.Length > 0);

            storeResponse = true;
            if (await executeCommandAsync("FEAT") == 211) serverFeat = FTPCommandsHelper.GetFeat(tmpResponsed);
            tmpResponsed = null;

            return (serverFeat.Length > 0);
        }

        /*private async Task _commandClntAsync()
        {
            if (isFeat("CLNT")) await executeCommandAsync("CLNT", CLIENT_NAME);
        }*/

        private async Task<bool> _commandOptsAsync(string letter)
        {
            return (await executeCommandAsync("OPTS", letter) == 200);
        }

        private async Task<bool> _commandAuthAsync(string protocol)
        {
            return (await executeCommandAsync("AUTH " + protocol) == 234);
        }

        private async Task<bool> _commandPbszAsync()
        {
            return (await executeCommandAsync("PBSZ 0") == 200);
        }

        private async Task<bool> _commandProtAsync()
        {
            return (await executeCommandAsync("PROT P") == 200);
        }

        private async Task<bool> _commandModezAsync()
        {
            return (isFeat("MODE Z") && (await executeCommandAsync("MODE Z") == 200));
        }

        private async Task _commandTypeAsync(FTPTransferMode transferMode)
        {
            await executeCommandAsync("TYPE", transferMode == FTPTransferMode.ASCII ? "A" : "I");
        }

        private async Task<int> commandListAsync(string path)
        {
            return await setupDataConnectionAsync() ? await executeCommandAsync("LIST", path) : 0;
        }

        private async Task<int> commandMlsdAsync(string path)
        {
            return await setupDataConnectionAsync() ? await executeCommandAsync("MLSD", path) : 0;
        }

        /*private async Task commandMlstAsync(string fileName)
        {
            //long size = 0;

            storeResponse = true;
            if (await executeCommandAsync("MLST", fileName) == 250)
            {
                //
            }
            tmpResponsed = null;
        }*/

        /*private async Task<int> _commandNlstAsync(string dirName)
        {
            if (SetupDataConnection()) return await ExecuteCommandAsync("NLST", dirName);

            return 0;
        }*/

        private async Task<bool> _commandCwdAsync(string path, bool force = false, bool setCurrentPath = true)
        {
            if (!force && (path == BrowsedPath)) return true;

            if (await executeCommandAsync("CWD", path) == 250)
            {
                if (setCurrentPath) BrowsedPath = path;
                return true;
            }

            return false;
        }

        private async Task commandAbortAsync()
        {
            if (needToAbort)
            {
                await executeCommandAsync("ABOR", null, true, true);
                closeDataConnection();
            }
        }

        private async Task<bool> _commandPasvAsync()
        {
            storeResponse = true;
            return (await executeCommandAsync("PASV") == 227);
        }

        private async Task<bool> _commandEpsvAsync()
        {
            storeResponse = true;
            return (await executeCommandAsync("EPSV") == 229);
        }

        private async Task<bool> _commandDeleAsync(string fileName)
        {
            int code = await executeCommandAsync("DELE", fileName);

            return ((code == 250) || (code == 226));
        }

        private async Task<bool> _commandRmdAsync(string dirName)
        {
            return (await executeCommandAsync("RMD", dirName) == 250);
        }

        private async Task<bool> _commandRmdaAsync(string dirName)
        {
            return (await executeCommandAsync("RMDA", dirName) == 250);
        }

        private async Task<bool> _commandStorAsync(string fileName)
        {
            if (await setupDataConnectionAsync())
            {
                int code = await executeCommandAsync("STOR", fileName);
                return ((code == 150) || (code == 125));
            }

            return false;
        }

        private async Task<long> _commandAppeAsync(string fileName)
        {
            long size = 0;

            storeResponse = true;
            if (await executeCommandAsync("SIZE", fileName) == 213)
                size = FTPCommandsHelper.GetSize(tmpResponsed);

            tmpResponsed = null;

            if ((size > 0) && await setupDataConnectionAsync())
            {
                int code = await executeCommandAsync("APPE", fileName);
                if ((code == 150) || (code == 125)) return size;
                else await commandAbortAsync();
            }

            return 0;
        }

        private async Task<bool> _commandRestAsync(long position)
        {
            return (await executeCommandAsync("REST", position.ToString()) == 350);
        }

        private async Task<int> _commandRetrAsync(string fileName, long position)
        {
            if (await setupDataConnectionAsync())
            {
                if ((position > 0) && !await _commandRestAsync(position)) return 0;
                return await executeCommandAsync("RETR", fileName);
            }

            return 0;
        }

        private async Task<bool> _commandMkdAsync(string dirName)
        {
            return (await executeCommandAsync("MKD", dirName) == 257);
        }

        private async Task<bool> commandRnfrAsync(string fileName)
        {
            return (await executeCommandAsync("RNFR", fileName) == 350);
        }

        private async Task<bool> commandRntoAsync(string fileName)
        {
            return (await executeCommandAsync("RNTO", fileName) == 250);
        }

        private async Task<bool> _commandChmodAsync(string fileName, string permission)
        {
            return (await executeCommandAsync("SITE CHMOD", permission + ' ' + fileName) == 200);
        }
    }
}