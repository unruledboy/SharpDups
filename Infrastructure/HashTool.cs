using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Xnlab.SharpDups.Infrastructure
{
    public class HashTool
    {
        public static byte[] HashFileBytes(string file)
        {
            try
            {
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    using (var md5 = new MD5CryptoServiceProvider())
                        return md5.ComputeHash(fs);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static byte[] HashFileBytes(string file, int start, int count)
        {
            try
            {
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    var bytes = new byte[count];
                    fs.Seek(start, SeekOrigin.Begin);
                    fs.Read(bytes, 0, count);
                    using (var md5 = new MD5CryptoServiceProvider())
                        return md5.ComputeHash(bytes);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GetHashText(byte[] hashBytes)
        {
            var sb = new StringBuilder(hashBytes.Length * 2);
            foreach (var t in hashBytes)
            {
                sb.Append(t.ToString("x2"));
            }
            return sb.ToString();
        }

        public static string HashFile(string file)
        {
            var result = HashFileBytes(file);
            return result != null ? GetHashText(result) : null;
        }

        public static string HashFile(string file, int start, int count)
        {
            var result = HashFileBytes(file, start, count);
            return result != null ? GetHashText(result) : null;
        }
    }
}
