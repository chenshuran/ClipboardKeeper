using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace ClipboardKeeper
{
    internal static class ProtectedDataHelper
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ClipboardKeeper.v1");

        public static string ProtectString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            byte[] plain = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(ProtectBytes(plain));
        }

        public static string UnprotectString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            try
            {
                byte[] protectedBytes = Convert.FromBase64String(value);
                byte[] plain = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plain);
            }
            catch
            {
                return value;
            }
        }

        public static byte[] ProtectBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return bytes;
            }

            return ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
        }

        public static byte[] UnprotectBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return bytes;
            }

            try
            {
                return ProtectedData.Unprotect(bytes, Entropy, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException)
            {
                return bytes;
            }
        }
    }
}
