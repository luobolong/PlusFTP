using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Hani.Utilities
{
    static class SslClient
    {
        /*public class SampleEventArgs
        {
            public SampleEventArgs(string s)
            {
                Text = s;
            }
            public String Text { get; private set; } // readonly
        } */

        internal delegate void ValidateCertificateHandler(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors);
        internal static event ValidateCertificateHandler OnValidateCertificate;

        internal static void ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (OnValidateCertificate != null)
                new ValidateCertificateHandler(OnValidateCertificate)(sender, certificate, chain, sslPolicyErrors);
        }

        internal static async Task<SslStream> ConnectAsync(Stream s, string server, SslProtocols prot)
        {
            try
            {
                X509CertificateCollection clientCertColl = new X509CertificateCollection();
                SslStream sslStream = new SslStream(s, false, new RemoteCertificateValidationCallback(validate), null);
                await sslStream.AuthenticateAsClientAsync(server, clientCertColl, prot, false);
                return sslStream;
            }
            catch (Exception exp) { ExceptionHelper.Log(exp); }
            return null;
        }

        private static bool validate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                ValidateCertificate(sender, certificate, chain, sslPolicyErrors);
                //InfoMsg("Certificate error: " + tmpResponsed, MessageType.Error);
            }

            return true;
        }
    }
}