using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Hani.Utilities
{
    internal static class CryptoHashing
    {
        internal static string Password { set { set(value); } }
        internal const string PasswordChar = "●●●●●●●●●●●●●●●●";

        private static byte[] keyBytes;

        private static void set(string pass)
        {
            using (Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(pass, Encoding.UTF8.GetBytes(PCINFO.ProcessorId), 4096))
                keyBytes = key.GetBytes(16);
        }

        internal static bool IsEncrypted(string str)
        {
            if (str == PasswordChar) return true;
            else return false;
        }

        internal static string Encrypt(string str)
        {
            if (str.NullEmpty()) return string.Empty;

            byte[] bytes = Encoding.UTF8.GetBytes(str);
            str = string.Empty;

            try
            {
                using (TripleDES TDES = TripleDES.Create())
                {
                    TDES.Key = keyBytes;
                    byte[] MSArray = null;

                    using (MemoryStream MS = new MemoryStream())
                    {
                        using (CryptoStream CS = new CryptoStream(MS, TDES.CreateEncryptor(), CryptoStreamMode.Write))
                            CS.Write(bytes, 0, bytes.Length);

                        bytes = new byte[] { };
                        MSArray = MS.ToArray();
                    }

                    string encrypted1 = Convert.ToBase64String(TDES.IV);
                    string encrypted2 = Convert.ToBase64String(MSArray);

                    return encrypted1.Remove(0, 1).Insert(0, encrypted2[0].ToString()) + '(' + encrypted2.Remove(0, 1).Insert(0, encrypted1[0].ToString());
                }
            }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            return string.Empty;
        }

        internal static string Decrypt(string str)
        {
            if (str.NullEmpty()) return string.Empty;

            try
            {
                string[] splited = str.Split(new Char[] { '(' });
                byte[] decrypted = Convert.FromBase64String(splited[1].Remove(0, 1).Insert(0, splited[0][0].ToString()));

                using (TripleDES TDES = TripleDES.Create())
                {
                    TDES.Key = keyBytes;
                    TDES.IV = Convert.FromBase64String(splited[0].Remove(0, 1).Insert(0, splited[1][0].ToString()));

                    using (MemoryStream MS = new MemoryStream())
                    {
                        CryptoStream CS = new CryptoStream(MS, TDES.CreateDecryptor(), CryptoStreamMode.Write);
                        CS.Write(decrypted, 0, decrypted.Length);
                        CS.Close();

                        return Encoding.UTF8.GetString(MS.ToArray());
                    }
                }
            }
            catch (Exception exp) { ExceptionHelper.Log(exp); }

            return string.Empty;
        }
    }
}