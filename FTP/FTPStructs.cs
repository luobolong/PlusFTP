namespace Hani.FTP
{
    internal enum FTPTransferMode : ushort { ASCII = 0, Binary = 1 };
    internal enum FTPSystemType : ushort { Unknown = 0, UNIX = 1, Windows = 2 };
    internal enum EncryptionType : ushort { PlainType = 0, ExplicitType = 1, ImplicitType = 2 }
    internal enum FTPSslProtocol : ushort { None = 0, SSL = 1, TLS = 2 }

    internal sealed class FTPEncryption
    {
        internal EncryptionType Type;
        internal FTPSslProtocol Protocol;

        internal FTPEncryption(EncryptionType type, FTPSslProtocol protocol)
        {
            Type = type;
            Protocol = protocol;
        }
    }
}