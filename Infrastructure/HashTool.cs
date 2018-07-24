using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Xnlab.SharpDups.Infrastructure
{
    public class HashTool
    {
        public static byte[] HashFileBytes(string file, int bufferSize)
        {
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize))
            {
                using (var md5 = new MD5CryptoServiceProvider())
                    return md5.ComputeHash(fs);
            }
        }

        public static byte[] HashFileBytes(string file, int start, int count, int bufferSize)
        {
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize))
            {
                var bytes = new byte[count];
                fs.Seek(start, SeekOrigin.Begin);
                fs.Read(bytes, 0, count);
                using (var md5 = new MD5CryptoServiceProvider())
                    return md5.ComputeHash(bytes);
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

        public static string HashFile(string file, int bufferSize)
        {
            var result = HashFileBytes(file, bufferSize);
            return result != null ? GetHashText(result) : null;
        }

        public static string HashFile(string file, int start, int count, int bufferSize)
        {
            var result = HashFileBytes(file, start, count, bufferSize);
            return result != null ? GetHashText(result) : null;
        }
    }
}
