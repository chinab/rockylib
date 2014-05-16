using System;
using System.Linq;
using System.Web.UI;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using Jillzhang.GifUtility;
using System.Net;

namespace System.Web
{
    public static partial class Extensions
    {
        #region PreValidate
        internal const int UploadImageMaxLength = 1024 * 1024;
        internal static readonly string[] UploadImageFileExt;

        static Extensions()
        {
            UploadImageFileExt = new string[] { ".jpg", ".gif", ".png" };
        }

        public static bool PreImageValidate(this HttpPostedFile postedFile, out string errorMessage)
        {
            if (postedFile.ContentLength == 0)
            {
                errorMessage = "请选择一张非空的图片";
                return false;
            }
            if (postedFile.ContentLength > UploadImageMaxLength)
            {
                errorMessage = "图片文件大小超出限制";
                return false;
            }
            string ext = Path.GetExtension(postedFile.FileName);
            if (!UploadImageFileExt.Any(t => string.Compare(t, ext, true) == 0))
            {
                errorMessage = "图片文件类型不在接受范围内";
                return false;
            }
            errorMessage = string.Empty;
            return true;
        }
        #endregion

        #region SaveImageAs
        public static string SaveImageAs(this HttpPostedFile postedFile, CreateFolderMode mode, string savePath)
        {
            string vrtualFilePath = null;
            byte[] buffer = new byte[postedFile.ContentLength];
            postedFile.InputStream.Read(buffer, 0, buffer.Length);
            if (FileValidator.ValidateImage(buffer))
            {
                vrtualFilePath = GetFilePath(postedFile, ref mode, savePath);
                HttpContext context = HttpContext.Current;
                string physicalFilePath = context.Server.MapPath(vrtualFilePath);
                App.CreateDirectory(physicalFilePath);
                File.WriteAllBytes(physicalFilePath, buffer);
            }
            return vrtualFilePath;
        }
        public static string[] SaveImageAs(this HttpPostedFile postedFile, CreateFolderMode mode, WaterMarkInfo waterMarkInfo, params ThumbnailInfo[] thumbnailInfoArray)
        {
            string[] vrtualFilePaths = new string[1 + thumbnailInfoArray.Length];
            byte[] buffer = new byte[postedFile.ContentLength];
            postedFile.InputStream.Read(buffer, 0, buffer.Length);
            if (FileValidator.ValidateImage(buffer))
            {
                HttpContext context = HttpContext.Current;
                string vitualFilePath, physicalFilePath;
                Image image = Image.FromStream(new MemoryStream(buffer));
                if (image.RawFormat.Guid == ImageFormat.Gif.Guid)
                {
                    vitualFilePath = GetFilePath(postedFile, ref mode, waterMarkInfo.SavePath);
                    physicalFilePath = context.Server.MapPath(vitualFilePath);
                    image.Save(physicalFilePath);
                    if (waterMarkInfo.Mode != WaterMarkInfo.ExecuteMode.Skip)
                    {
                        if (waterMarkInfo.Mode == WaterMarkInfo.ExecuteMode.Image)
                        {
                            using (Bitmap waterImg = waterMarkInfo.GetImage())
                            {
                                float x = image.Width - waterImg.Width - 24F, y = image.Height - waterImg.Height - 24F;
                                GifHelper.WaterMark(physicalFilePath, waterImg, x, y, physicalFilePath);
                            }
                        }
                        else
                        {
                            float x = image.Width - waterMarkInfo.TextSize - 24F, y = image.Height - waterMarkInfo.TextSize - 24F;
                            GifHelper.SmartWaterMark(physicalFilePath, waterMarkInfo.Text, ColorTranslator.FromHtml(waterMarkInfo.TextColor), new Font("宋体", waterMarkInfo.TextSize), x, y, physicalFilePath);
                        }
                        vrtualFilePaths[0] = vitualFilePath;
                    }
                    string srcPhysicalFilePath = physicalFilePath;
                    for (int i = 0; i < thumbnailInfoArray.Length; )
                    {
                        ThumbnailInfo thumbnailInfo = thumbnailInfoArray[i];
                        vitualFilePath = GetFilePath(postedFile, ref mode, thumbnailInfo.SavePath);
                        physicalFilePath = context.Server.MapPath(vitualFilePath);
                        SizeF size = GDIHelper.GetProportionSize(new SizeF(thumbnailInfo.Width, thumbnailInfo.Height), image.Size);
                        GifHelper.GetThumbnail(srcPhysicalFilePath, (double)size.Width / (double)image.Width, physicalFilePath);
                        vrtualFilePaths[++i] = vitualFilePath;
                    }
                }
                else
                {
                    if (waterMarkInfo.Mode != WaterMarkInfo.ExecuteMode.Skip)
                    {
                        vitualFilePath = GetFilePath(postedFile, ref mode, waterMarkInfo.SavePath);
                        physicalFilePath = context.Server.MapPath(vitualFilePath);
                        if (waterMarkInfo.Mode == WaterMarkInfo.ExecuteMode.Image)
                        {
                            using (Bitmap waterImg = waterMarkInfo.GetImage())
                            {
                                GDIHelper.MakeWaterMark(image, waterImg, ContentAlignment.BottomRight, 24);
                            }
                        }
                        else
                        {
                            GDIHelper.MakeWaterMark(image, waterMarkInfo.Text, new Font("宋体", waterMarkInfo.TextSize), ColorTranslator.FromHtml(waterMarkInfo.TextColor), ContentAlignment.BottomRight, 24);
                        }
                        image.Save(physicalFilePath);
                        vrtualFilePaths[0] = vitualFilePath;
                    }
                    for (int i = 0; i < thumbnailInfoArray.Length; )
                    {
                        ThumbnailInfo thumbnailInfo = thumbnailInfoArray[i];
                        vitualFilePath = GetFilePath(postedFile, ref mode, thumbnailInfo.SavePath);
                        physicalFilePath = context.Server.MapPath(vitualFilePath);
                        Bitmap thumbnailImg = GDIHelper.GetThumbnailImage(image, new SizeF(thumbnailInfo.Width, thumbnailInfo.Height), thumbnailInfo.Mode);
                        thumbnailImg.Save(physicalFilePath);
                        vrtualFilePaths[++i] = vitualFilePath;
                    }
                }
                image.Dispose();
            }
            return vrtualFilePaths;
        }

        private static string GetFilePath(HttpPostedFile postedFile, ref CreateFolderMode mode, string vitualPath)
        {
            string prefixName = Path.GetFileNameWithoutExtension(vitualPath);
            if (prefixName.Length > 0)
            {
                vitualPath = StringHelper.TrimReplace(vitualPath, prefixName);
            }
            StringBuilder sb = new StringBuilder(vitualPath);
            DateTime now = DateTime.Now;
            switch (mode)
            {
                case CreateFolderMode.Daily:
                    sb.Append(now.ToString("yyyyMM'/'dd'/'"));
                    break;
                case CreateFolderMode.Hourly:
                    sb.Append(now.ToString("yyyyMM'/'dd'/'HH'/'"));
                    break;
                default:
                    sb.Append(now.ToString("yyyyMM'/'"));
                    break;
            }
            HttpContext context = HttpContext.Current;
            App.CreateDirectory(context.Server.MapPath(sb.ToString()));
            return sb.Append(prefixName).Append(StringHelper.NowDateString).Append(Path.GetExtension(postedFile.FileName)).ToString();
        }
        #endregion
    }

    public enum CreateFolderMode
    {
        Monthly, Daily, Hourly
    }

    #region WaterMarkInfo
    [Serializable]
    public class WaterMarkInfo
    {
        public enum ExecuteMode
        {
            Text, Image, Skip
        }

        public string Text { set; get; }
        public float TextSize { set; get; }
        public string TextColor { set; get; }
        /// <summary>
        /// 水印图片Base64(最好采用透明png)
        /// </summary>
        public string ImageBase64 { set; get; }
        public ExecuteMode Mode { set; get; }
        public string SavePath { set; get; }

        public Bitmap GetImage()
        {
            string base64 = this.ImageBase64;
            if (string.IsNullOrEmpty(base64))
            {
                throw new ArgumentException("ImageBase64");
            }
            return new Bitmap(new MemoryStream(Convert.FromBase64String(base64)));
        }
    }

    [Serializable]
    public class ThumbnailInfo
    {
        public int Width { set; get; }
        public int Height { set; get; }
        public ThumbnailMode Mode { set; get; }
        public string SavePath { set; get; }
    }
    #endregion
}