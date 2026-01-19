using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ModelessForms.IssuesManager.Services
{
    public class ScreenshotService
    {
        public byte[] CaptureRegion(int x, int y, int width, int height)
        {
            if (width <= 0 || height <= 0)
                return null;

            try
            {
                using (var bitmap = new Bitmap(width, height))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height));
                    using (var stream = new MemoryStream())
                    {
                        bitmap.Save(stream, ImageFormat.Png);
                        return stream.ToArray();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public string SaveScreenshot(byte[] imageData, string imagesFolder, string issueId, int index)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            try
            {
                var fileName = $"{issueId}_{index:D3}.png";
                var filePath = Path.Combine(imagesFolder, fileName);

                File.WriteAllBytes(filePath, imageData);

                return fileName;
            }
            catch
            {
                return null;
            }
        }

        public void DeleteScreenshot(string imagesFolder, string fileName)
        {
            try
            {
                var filePath = Path.Combine(imagesFolder, fileName);
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch
            {
            }
        }

        public string GetScreenshotPath(string imagesFolder, string fileName)
        {
            return Path.Combine(imagesFolder, fileName);
        }
    }
}
