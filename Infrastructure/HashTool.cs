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

		public static byte[] HashFileBytes(string file, long start, int count, int bufferSize)
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

		public static byte[] HashBytes(byte[] contentBytes)
		{
			using (var md5 = new MD5CryptoServiceProvider())
				return md5.ComputeHash(contentBytes);
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
			return GetHashText(result);
		}

		public static string HashBytesText(byte[] contentBytes)
		{
			var result = HashBytes(contentBytes);
			return GetHashText(result);
		}

		public static string HashFile(string file, long start, int count, int bufferSize)
		{
			var result = HashFileBytes(file, start, count, bufferSize);
			return GetHashText(result);
		}
	}
}
