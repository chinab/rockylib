using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace System.Net
{
    /// <summary>
    /// System.Net.Mime.MediaTypeNames
    /// </summary>
    public static class FileValidator
    {
        public static bool ValidateImage(byte[] buffer)
        {
            if (buffer.Length < 4)
            {
                return false;
            }
            if (ValidateHeader(buffer, FileHeader.Gif, FileHeader.Png))
            {
                return true;
            }
            //JpgOrJpeg
            int len = buffer.Length;
            if (ValidateHeader(buffer, FileHeader.Jpeg) && buffer[len - 2] == 0xff && buffer[len - 1] == 0xd9)
            {
                return true;
            }
            return false;
        }

        public static bool ValidateHeader(Stream stream, params FileHeader[] kind)
        {
            byte[] buffer = new byte[2];
            long pos = stream.Position;
            stream.Position = 0L;
            stream.Read(buffer, 0, buffer.Length);
            stream.Position = pos;
            return ValidateHeader(buffer, kind);
        }
        public static bool ValidateHeader(byte[] buffer, params FileHeader[] kinds)
        {
            if (buffer.Length < 2)
            {
                return false;
            }
            string headerValue = buffer[0].ToString() + buffer[1].ToString();
            return kinds.Contains((FileHeader)int.Parse(headerValue));
        }
    }

    public enum FileHeader
    {
        Jpeg = 255216,
        Gif = 7173,
        Png = 13780,
        Swf = 6787,
        Zip = 8075,
        Rar = 8297,
        _7z = 55122
    }
}