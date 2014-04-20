using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace System
{
    public static class GDIHelper
    {
        #region Helper
        public static bool IsGif(Image srcImage)
        {
            return srcImage.RawFormat == ImageFormat.Gif;
        }

        /// <summary>
        /// 按等比例缩小
        /// </summary>
        /// <param name="maxSize">需要缩小到的大小</param>
        /// <param name="srcSize">原始大小</param>
        /// <returns>返回按等比例缩小后的大小</returns>
        public static SizeF GetProportionSize(SizeF maxSize, SizeF srcSize)
        {
            float w = 0F, h = 0F;
            float sw = srcSize.Width, sh = srcSize.Height;
            float mw = maxSize.Width, mh = maxSize.Height;
            if (sw < mw && sh < mh)
            {
                w = sw;
                h = sh;
            }
            else if ((sw / sh) > (mw / mh))
            {
                w = maxSize.Width;
                h = (w * sh) / sw;
            }
            else
            {
                h = maxSize.Height;
                w = (h * sw) / sh;
            }
            return new SizeF(w, h);
        }
        #endregion

        #region Thumbnail
        public static Bitmap GetCropImage(Image srcImage, int x, int y, int width, int height)
        {
            Bitmap destImage = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(destImage))
            {
                g.DrawImage(srcImage, new Rectangle(Point.Empty, destImage.Size), new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
            }
            return destImage;
        }

        public static Bitmap GetThumbnailImage(Image srcImage, SizeF thumSize, ThumbnailMode mode)
        {
            RectangleF thumRect = new RectangleF(PointF.Empty, thumSize);
            RectangleF srcRect = new RectangleF(PointF.Empty, srcImage.Size);
            switch (mode)
            {
                case ThumbnailMode.Zoom:
                    thumRect.Size = GetProportionSize(thumSize, srcRect.Size);
                    break;
                case ThumbnailMode.ZoomWidth:
                    thumRect.Width = srcRect.Width * thumRect.Height / srcRect.Height;
                    break;
                case ThumbnailMode.ZoomHeight:
                    thumRect.Height = srcRect.Height * thumRect.Width / srcRect.Width;
                    break;
                case ThumbnailMode.Cut:
                    if (srcRect.Width / srcRect.Height > thumRect.Width / thumRect.Height)
                    {
                        srcRect.Width = srcRect.Height * thumRect.Width / thumRect.Height;
                        srcRect.X = (srcImage.Width - srcRect.Width) / 2;
                    }
                    else
                    {
                        srcRect.Height = srcRect.Width * thumRect.Height / thumRect.Width;
                        srcRect.Y = (srcImage.Height - srcRect.Height) / 2;
                    }
                    break;
            }
            Bitmap destImage = new Bitmap((int)thumRect.Width, (int)thumRect.Height);
            using (Graphics g = Graphics.FromImage(destImage))
            {
                g.CompositingQuality = CompositingQuality.HighSpeed;
                g.InterpolationMode = InterpolationMode.Low;
                g.SmoothingMode = SmoothingMode.HighSpeed;
                g.DrawImage(srcImage, thumRect, srcRect, GraphicsUnit.Pixel);
            }
            return destImage;
        }

        public static void MakeThumbnailImage(string inImgPath, string outImgPath, int thumWidth, int thumHeight, ThumbnailMode mode)
        {
            Image srcImage = Image.FromFile(inImgPath);
            GetThumbnailImage(srcImage, new SizeF(thumWidth, thumHeight), mode).Save(outImgPath);
            srcImage.Dispose();
        }
        #endregion

        #region WaterMark
        public static void MakeWaterMark(Image srcImage, string text, Font textFont, Color textColor, ContentAlignment alignment, float margin)
        {
            using (Graphics g = Graphics.FromImage(srcImage))
            {
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                SizeF textSizeF = g.MeasureString(text, textFont);
                float x = margin, y = margin;
                switch (alignment)
                {
                    case ContentAlignment.TopLeft:
                        break;
                    case ContentAlignment.TopCenter:
                        x = ((float)srcImage.Width - textSizeF.Width) / 2;
                        break;
                    case ContentAlignment.TopRight:
                        x = (float)srcImage.Width - textSizeF.Width - x;
                        break;
                    case ContentAlignment.MiddleLeft:
                        y = ((float)srcImage.Height - textSizeF.Height) / 2;
                        break;
                    case ContentAlignment.MiddleCenter:
                        x = ((float)srcImage.Width - textSizeF.Width) / 2;
                        y = ((float)srcImage.Height - textSizeF.Height) / 2;
                        break;
                    case ContentAlignment.MiddleRight:
                        x = (float)srcImage.Width - textSizeF.Width - x;
                        y = ((float)srcImage.Height - textSizeF.Height) / 2;
                        break;
                    case ContentAlignment.BottomLeft:
                        y = (float)srcImage.Height - textSizeF.Height - y;
                        break;
                    case ContentAlignment.BottomCenter:
                        x = ((float)srcImage.Width - textSizeF.Width) / 2;
                        y = (float)srcImage.Height - textSizeF.Height - y;
                        break;
                    case ContentAlignment.BottomRight:
                        x = (float)srcImage.Width - textSizeF.Width - x;
                        y = (float)srcImage.Height - textSizeF.Height - y;
                        break;
                }
                g.DrawString(text, textFont, new SolidBrush(textColor), x, y);
            }
        }

        public static void MakeWaterMark(string inImgPath, string outImgPath, string text, float textSize)
        {
            Image srcImage = Image.FromFile(inImgPath);
            MakeWaterMark(srcImage, text, new Font("Arial", textSize, FontStyle.Regular), Color.White, ContentAlignment.BottomRight, 24);
            srcImage.Save(outImgPath);
            srcImage.Dispose();
        }

        public static void MakeWaterMark(Image srcImage, Image waterMarkImage, ContentAlignment alignment, int margin)
        {
            if (srcImage.Width - margin < waterMarkImage.Width || srcImage.Height - margin < waterMarkImage.Height)
            {
                return;
            }
            using (Graphics g = Graphics.FromImage(srcImage))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                Rectangle destRect = new Rectangle(new Point(margin, margin), waterMarkImage.Size);
                switch (alignment)
                {
                    case ContentAlignment.TopLeft:
                        break;
                    case ContentAlignment.TopCenter:
                        destRect.X = (srcImage.Width - destRect.Width) / 2;
                        break;
                    case ContentAlignment.TopRight:
                        destRect.X = srcImage.Width - destRect.Width - destRect.X;
                        break;
                    case ContentAlignment.MiddleLeft:
                        destRect.Y = (srcImage.Height - destRect.Height) / 2;
                        break;
                    case ContentAlignment.MiddleCenter:
                        destRect.X = (srcImage.Width - destRect.Width) / 2;
                        destRect.Y = (srcImage.Height - destRect.Height) / 2;
                        break;
                    case ContentAlignment.MiddleRight:
                        destRect.X = srcImage.Width - destRect.Width - destRect.X;
                        destRect.Y = (srcImage.Height - destRect.Height) / 2;
                        break;
                    case ContentAlignment.BottomLeft:
                        destRect.Y = srcImage.Height - destRect.Height - destRect.Y;
                        break;
                    case ContentAlignment.BottomCenter:
                        destRect.X = (srcImage.Width - destRect.Width) / 2;
                        destRect.Y = srcImage.Height - destRect.Height - destRect.Y;
                        break;
                    case ContentAlignment.BottomRight:
                        destRect.X = srcImage.Width - destRect.Width - destRect.X;
                        destRect.Y = srcImage.Height - destRect.Height - destRect.Y;
                        break;
                }
                g.DrawImage(waterMarkImage, destRect, 0, 0, destRect.Width, destRect.Height, GraphicsUnit.Pixel);
            }
        }

        public static void MakeWaterMark(string inImgPath, string outImgPath, Image waterMarkImage)
        {
            Image srcImage = Image.FromFile(inImgPath);
            MakeWaterMark(srcImage, waterMarkImage, ContentAlignment.BottomRight, 24);
            srcImage.Save(outImgPath);
            srcImage.Dispose();
        }

        /// <summary>
        /// 在一张图片的指定位置处加入一张具有水印效果的图片
        /// </summary>
        /// <param name="SourceImage">指定源图片的绝对路径</param>
        /// <param name="WaterMarkImage">指定水印图片的绝对路径</param>
        /// <param name="SaveImage">保存图片的绝对路径</param>
        public static void MakeImageWaterMark(string SourceImage, string WaterMarkImage, string SaveImage)
        {
            // 创建一个对象用于操作需要加水印的源图片
            Image imgPhoto = Image.FromFile(SourceImage);
            // 获取该源图片的宽度和高度
            int phWidth = imgPhoto.Width;
            int phHeight = imgPhoto.Height;

            // 创建一个BMP格式的空白图片(宽度和高度与源图片一致)
            Bitmap bmPhoto = new Bitmap(phWidth, phHeight, PixelFormat.Format24bppRgb);

            // 设置该新建空白BMP图片的分辨率
            bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

            // 将该BMP图片设置成一个图形对象
            Graphics grPhoto = Graphics.FromImage(bmPhoto);

            // 设置生成图片的质量
            grPhoto.SmoothingMode = SmoothingMode.AntiAlias;

            // 将源图片加载至新建的BMP图片中
            grPhoto.DrawImage(
                imgPhoto,                               // Photo Image object
                new Rectangle(0, 0, phWidth, phHeight), // Rectangle structure
                0,                                      // x-coordinate of the portion of the source image to draw. 
                0,                                      // y-coordinate of the portion of the source image to draw. 
                phWidth,                                // Width of the portion of the source image to draw. 
                phHeight,                               // Height of the portion of the source image to draw. 
                GraphicsUnit.Pixel);                    // Units of measure 

            // 创建水印图片的 Image 对象
            Image imgWatermark = new Bitmap(WaterMarkImage);

            // 获取水印图片的宽度和高度
            int wmWidth = imgWatermark.Width;
            int wmHeight = imgWatermark.Height;

            //------------------------------------------------------------
            // 第一步： 插入水印图片
            //------------------------------------------------------------

            //Create a Bitmap based on the previously modified photograph Bitmap
            Bitmap bmWatermark = new Bitmap(bmPhoto);
            bmWatermark.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);
            //Load this Bitmap into a new Graphic Object
            Graphics grWatermark = Graphics.FromImage(bmWatermark);

            //To achieve a transulcent watermark we will apply (2) color 
            //manipulations by defineing a ImageAttributes object and 
            //seting (2) of its properties.
            ImageAttributes imageAttributes = new ImageAttributes();

            //The first step in manipulating the watermark image is to replace 
            //the background color with one that is trasparent (Alpha=0, R=0, G=0, B=0)
            //to do this we will use a Colormap and use this to define a RemapTable
            ColorMap colorMap = new ColorMap();

            //My watermark was defined with a background of 100% Green this will
            //be the color we search for and replace with transparency
            colorMap.OldColor = Color.FromArgb(255, 0, 255, 0);
            colorMap.NewColor = Color.FromArgb(0, 0, 0, 0);

            ColorMap[] remapTable = { colorMap };

            imageAttributes.SetRemapTable(remapTable, ColorAdjustType.Bitmap);

            //The second color manipulation is used to change the opacity of the 
            //watermark.  This is done by applying a 5x5 matrix that contains the 
            //coordinates for the RGBA space.  By setting the 3rd row and 3rd column 
            //to 0.3f we achive a level of opacity
            float[][] colorMatrixElements = { 
												new float[] {1.0f,  0.0f,  0.0f,  0.0f, 0.0f},       
												new float[] {0.0f,  1.0f,  0.0f,  0.0f, 0.0f},        
												new float[] {0.0f,  0.0f,  1.0f,  0.0f, 0.0f},        
												new float[] {0.0f,  0.0f,  0.0f,  0.3f, 0.0f},        
												new float[] {0.0f,  0.0f,  0.0f,  0.0f, 1.0f}
											};

            ColorMatrix wmColorMatrix = new ColorMatrix(colorMatrixElements);

            imageAttributes.SetColorMatrix(wmColorMatrix, ColorMatrixFlag.Default,
                ColorAdjustType.Bitmap);

            //For this example we will place the watermark in the upper right
            //hand corner of the photograph. offset down 10 pixels and to the 
            //left 10 pixles
            int xPosOfWm = ((phWidth - wmWidth) - 10);
            int yPosOfWm = 10;

            grWatermark.DrawImage(imgWatermark,
                new Rectangle(xPosOfWm, yPosOfWm, wmWidth, wmHeight),  //Set the detination Position
                0,                  // x-coordinate of the portion of the source image to draw. 
                0,                  // y-coordinate of the portion of the source image to draw. 
                wmWidth,            // Watermark Width
                wmHeight,		    // Watermark Height
                GraphicsUnit.Pixel, // Unit of measurment
                imageAttributes);   //ImageAttributes Object

            //Replace the original photgraphs bitmap with the new Bitmap
            imgPhoto.Dispose();
            imgPhoto = bmWatermark;
            grPhoto.Dispose();
            grWatermark.Dispose();
            bmPhoto.Dispose();

            //------------------------------------------------------------
            // 第三步：保存图片
            //------------------------------------------------------------
            imgPhoto.Save(SaveImage, ImageFormat.Jpeg);

            // 释放使用中的资源
            imgPhoto.Dispose();
            imgWatermark.Dispose();
            bmWatermark.Dispose();
        }
        #endregion

        #region LinearGradient
        /// <summary>
        /// 渐变效果
        /// </summary>
        /// <param name="srcImage">图片源</param>
        /// <param name="colorHead">起点色(HTML格式)</param>
        /// <param name="colorTail">终点色(HTML格式)</param>
        /// <param name="angle"></param>
        public static Image LinearGradient(Image srcImage, string colorHead, string colorTail, float angle)
        {
            int width = srcImage.Width, height = srcImage.Height;
            using (Graphics g = Graphics.FromImage(srcImage))
            {
                LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, width, height), ColorTranslator.FromHtml(colorHead), ColorTranslator.FromHtml(colorTail), angle);
                g.FillRectangle(brush, new Rectangle(0, 0, width, height));
                brush.Dispose();
            }
            return (Image)srcImage.Clone();
        }
        #endregion

        #region 产生波形滤镜效果
        private const double PI = 3.1415926535897932384626433832795;
        private const double PI2 = 6.283185307179586476925286766559;

        /// <summary>
        /// 正弦曲线Wave扭曲图片
        /// </summary>
        /// <param name="sourceBmp">图片源</param>
        /// <param name="bXDir">如果扭曲则选择为True</param>
        /// <param name="dMultValue">波形的幅度倍数，越大扭曲的程度越高，一般为3</param>
        /// <param name="dPhase">波形的起始相位，取值区间[0-2*PI)</param>
        /// <returns></returns>
        public static Bitmap TwistImage(Bitmap sourceBmp, bool bXDir, double dMultValue, double dPhase)
        {
            Bitmap destinationBmp = new Bitmap(sourceBmp.Width, sourceBmp.Height);
            // 将位图背景填充为白色
            Graphics graph = Graphics.FromImage(destinationBmp);
            graph.FillRectangle(new SolidBrush(Color.White), 0, 0, destinationBmp.Width, destinationBmp.Height);
            graph.Dispose();
            double dBaseAxisLen = bXDir ? (double)destinationBmp.Height : (double)destinationBmp.Width;
            for (int i = 0; i < destinationBmp.Width; i++)
            {
                for (int j = 0; j < destinationBmp.Height; j++)
                {
                    double dx = 0;
                    dx = bXDir ? (PI2 * (double)j) / dBaseAxisLen : (PI2 * (double)i) / dBaseAxisLen;
                    dx += dPhase;
                    double dy = Math.Sin(dx);
                    // 取得当前点的颜色
                    int nOldX = 0, nOldY = 0;
                    nOldX = bXDir ? i + (int)(dy * dMultValue) : i;
                    nOldY = bXDir ? j : j + (int)(dy * dMultValue);
                    Color color = sourceBmp.GetPixel(i, j);
                    if (nOldX >= 0 && nOldX < destinationBmp.Width && nOldY >= 0 && nOldY < destinationBmp.Height)
                    {
                        destinationBmp.SetPixel(nOldX, nOldY, color);
                    }
                }
            }
            return destinationBmp;
        }
        #endregion
    }
}