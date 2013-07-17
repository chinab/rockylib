using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace InfrastructureService.Client
{
    public static class TransferHelper
    {
        public static MemoryStream DrawImage(Stream imgStream, string text, Font textFont, Color textColor, ContentAlignment alignment, float margin)
        {
            var stream = new MemoryStream();
            using (var img = Image.FromStream(imgStream))
            {
                GDIHelper.MakeWaterMark(img, text, textFont, textColor, alignment, margin);
                img.Save(stream, img.RawFormat);
            }
            return stream;
        }

        public static MemoryStream DrawImage(Stream imgStream, Image waterMarkImage, System.Drawing.ContentAlignment alignment, int margin)
        {
            var stream = new MemoryStream();
            using (var img = Image.FromStream(imgStream))
            {
                GDIHelper.MakeWaterMark(img, waterMarkImage, alignment, margin);
                img.Save(stream, img.RawFormat);
            }
            return stream;
        }
    }
}