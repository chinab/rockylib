using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace System.Web
{
    public class VerifyCode : IHttpHandler, System.Web.SessionState.IRequiresSessionState
    {
        #region 字段
        private const string Prefix = "_VerifyCode";
        #endregion

        #region 属性
        /// <summary>
        /// 验证码长度(默认6个验证码的长度)
        /// </summary>
        public int Length { get; set; }
        /// <summary>
        /// 验证码字体大小(为了显示扭曲效果，默认48像素，可以自行修改)
        /// </summary>
        public int FontSize { get; set; }
        /// <summary>
        /// 边框补(默认2像素)
        /// </summary>
        public int Padding { get; set; }
        /// <summary>
        /// 是否输出燥点(默认不输出)
        /// </summary>
        public bool UsingChaos { get; set; }
        /// <summary>
        /// 输出燥点的颜色(默认灰色)
        /// </summary>
        public Color ChaosColor { get; set; }
        /// <summary>
        /// 自定义背景色(默认白色)
        /// </summary>
        public Color BackgroundColor { get; set; }
        /// <summary>
        /// 自定义随机颜色数组
        /// </summary>
        public Color[] Colors { get; set; }
        /// <summary>
        /// 自定义字体数组
        /// </summary>
        public string[] Fonts { get; set; }
        public bool UsingTwist { get; set; }
        #endregion

        #region 公共方法
        public VerifyCode()
        {
            Length = 6;
            FontSize = 48;
            Padding = 2;
            UsingChaos = true;
            ChaosColor = Color.LightGray;
            BackgroundColor = Color.White;
            Colors = new Color[] { Color.Black, Color.Red, Color.DarkBlue, Color.Green, Color.Orange, Color.Brown, Color.DarkCyan, Color.Purple };
            Fonts = new string[] { "Arial", "Georgia" };
        }

        public string Generate()
        {
            string resultCode = String.Empty;
            Random random = new Random();
            int number;
            char code;
            for (int i = 0; i < this.Length; i++)
            {
                number = random.Next();
                if (number % 2 == 0)
                {
                    code = (char)('0' + (char)(number % 10));
                }
                else
                {
                    code = (char)('A' + (char)(number % 26));
                }
                resultCode += code.ToString();
            }
            HttpContext context = HttpContext.Current;
            context.Response.ClearContent();
            context.Response.ContentType = "image/gif";
            using (Bitmap image = CreateImageCode(resultCode))
            {
                image.Save(context.Response.OutputStream, ImageFormat.Gif);
            }
            context.Session[Prefix] = resultCode;
            return resultCode;
        }

        public static bool Validate(string value)
        {
            HttpContext context = HttpContext.Current;
            object code = context.Session[Prefix];
            return code != null ? code.ToString().Equals(value, StringComparison.OrdinalIgnoreCase) : false;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 生成校验码图片
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private Bitmap CreateImageCode(string code)
        {
            int fSize = FontSize;
            int fWidth = fSize + Padding;
            int imageWidth = (int)(code.Length * fWidth) + 4 + Padding * 2;
            int imageHeight = fSize * 2 + Padding;
            Bitmap image = new Bitmap(imageWidth, imageHeight);
            Graphics g = Graphics.FromImage(image);
            g.Clear(BackgroundColor);
            Random rand = new Random();
            //给背景添加随机生成的燥点
            if (this.UsingChaos)
            {
                Pen pen = new Pen(ChaosColor, 0);
                int c = Length * 10;
                for (int i = 0; i < c; i++)
                {
                    int x = rand.Next(image.Width);
                    int y = rand.Next(image.Height);
                    g.DrawRectangle(pen, x, y, 1, 1);
                }
            }
            int left = 0, top = 0, top1 = 1, top2 = 1;
            int n1 = (imageHeight - FontSize - Padding * 2);
            int n2 = n1 / 4;
            top1 = n2;
            top2 = n2 * 2;
            Font f;
            Brush b;
            int cindex, findex;
            //随机字体和颜色的验证码字符
            for (int i = 0; i < code.Length; i++)
            {
                cindex = rand.Next(Colors.Length - 1);
                findex = rand.Next(Fonts.Length - 1);
                f = new System.Drawing.Font(Fonts[findex], fSize, System.Drawing.FontStyle.Bold);
                b = new System.Drawing.SolidBrush(Colors[cindex]);
                if (i % 2 == 1)
                {
                    top = top2;
                }
                else
                {
                    top = top1;
                }
                left = i * fWidth;
                g.DrawString(code.Substring(i, 1), f, b, left, top);
            }
            //画一个边框 边框颜色为Color.Gainsboro
            g.DrawRectangle(new Pen(Color.Gainsboro, 0), 0, 0, image.Width - 1, image.Height - 1);
            g.Dispose();
            //产生波形
            if (UsingTwist)
            {
                image = GDIHelper.TwistImage(image, true, 8, 4);
            }
            return image;
        }
        #endregion

        #region IHttpHandler 成员

        bool IHttpHandler.IsReusable
        {
            get { return true; }
        }

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            this.Length = 4;
            this.FontSize = 12;
            this.Padding = 0;
            this.Generate();
        }

        #endregion
    }
}