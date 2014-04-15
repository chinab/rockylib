using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace System.Web
{
    public class ValidateGenerator : IValidateCode
    {
        public static readonly IValidateCode Generator = new ValidateGenerator();
        private const string validateKey = "ValidateCode";

        private char[] codeSerial = new char[] 
        { 
            '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', 
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 
            'h', 'i', 'j', 'k', 'l', 'm', 'n', 
            'o', 'p', 'q', 
            'r', 's', 't', 
            'u', 'v', 'w', 
            'x', 'y', 'z',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 
            'H', 'I', 'J', 'K', 'L', 'M', 'N', 
            'O', 'P', 'Q', 
            'R', 'S', 'T', 
            'U', 'V', 'W', 
            'X', 'Y', 'Z' 
        };
        private Font[] codeFonts = new Font[] 
        { 
            new Font("Arial", 12, FontStyle.Regular), 
            new Font("Verdana", 12, FontStyle.Italic), 
            new Font("Tahoma", 12, FontStyle.Regular), 
        };

        protected ValidateGenerator()
        {

        }

        public Image Generate(string id, int length, Image backgroundImage)
        {
            StringBuilder codeGenerator = new StringBuilder();
            Random rnd = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < length; i++)
            {
                int code = rnd.Next(0, 32);
                codeGenerator.Append(codeSerial[code]);
            }
            string validateCode = codeGenerator.ToString();
            HttpContext context = HttpContext.Current;
            Dictionary<string, string> dict = (Dictionary<string, string>)context.Session[validateKey];
            if (dict == null)
            {
                context.Session[validateKey] = dict = new Dictionary<string, string>();
            }
            dict[id] = validateCode;

            const int wordWidth = 18;
            int width = validateCode.Length * wordWidth, height = 22;

            Bitmap img = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(img);

            if (backgroundImage != null)
            {
                g.DrawImage(backgroundImage, 0, 0, width, height);
            }

            Brush brush = new LinearGradientBrush(new Rectangle(0, 0, width, height), Color.Tomato, Color.Blue, rnd.Next(30, 180), true);
            for (int j = 0; j < length; j++)
            {
                g.DrawString(validateCode[j].ToString(), codeFonts[rnd.Next(0, codeFonts.Length)], brush, j * wordWidth + rnd.Next(2, 3), rnd.Next(0, 3), StringFormat.GenericTypographic);
            }

            int linenum = 10;
            int linespan = (int)width / linenum;
            for (int i = 0; i < linenum; i++)
            {
                img.SetPixel(rnd.Next(width), rnd.Next(height), Color.Blue);
            }

            return img;
        }

        public bool Validate(string id, string value)
        {
            HttpContext context = HttpContext.Current;
            Dictionary<string, string> dict = (Dictionary<string, string>)context.Session[validateKey];
            string sValue;
            if (dict != null && dict.TryGetValue(id, out sValue))
            {
                dict.Remove(id);
                return string.Compare(sValue, value, true) == 0;
            }
            return false;
        }
    }

    public interface IValidateCode
    {
        Image Generate(string id, int length, Image backgroundImage);
        bool Validate(string id, string value);
    }
}