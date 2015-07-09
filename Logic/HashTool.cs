using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Xnlab.SharpDups.Logic
{
    public class HashTool
    {
        public static byte[] HashFileBytes(string file)
        {
            try
            {
                byte[] hashBytes;
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    var md5 = new MD5CryptoServiceProvider();
                    hashBytes = md5.ComputeHash(fs);
                    fs.Close();
                }
                return hashBytes;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GetHashText(byte[] hashBytes)
        {
            var sb = new StringBuilder();
            foreach (var t in hashBytes)
            {
                sb.Append(t.ToString("x2"));
            }
            return sb.ToString();
        }

        public static string HashFile(string file)
        {
            var result = HashFileBytes(file);
            if (result != null)
                return GetHashText(result);
            return null;
        }
    }
}
