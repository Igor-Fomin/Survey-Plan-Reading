using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Autodesk.AutoCAD.Geometry;

namespace Survey_Plan_Reading
{
    public static class ScreenCaptureUtility
    {
        /// <summary>
        /// Captures a specific area of the screen defined by two Point3d coordinates.
        /// Handles high-DPI scaling to ensure the capture window aligns with physical pixels.
        /// </summary>
        public static Bitmap CaptureArea(Point3d p1, Point3d p2)
        {
            float scale = GetDpiScaling();

            // Apply DPI scaling to logical coordinates to get physical pixel coordinates
            int left = (int)(Math.Min(p1.X, p2.X) * scale);
            int top = (int)(Math.Min(p1.Y, p2.Y) * scale);
            int width = (int)(Math.Abs(p1.X - p2.X) * scale);
            int height = (int)(Math.Abs(p1.Y - p2.Y) * scale);

            if (width <= 0 || height <= 0)
                throw new ArgumentException("Capture area must have a positive width and height.");

            Bitmap bitmap = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Copy from physical screen pixels
                g.CopyFromScreen(left, top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }

        public static float GetDpiScaling()
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                return g.DpiX / 96f; // Standard DPI is 96
            }
        }

        /// <summary>
        /// Pre-processes the image for better OCR accuracy: 
        /// 2x scale (Bicubic), Grayscale, and 30% Contrast Increase.
        /// </summary>
        public static Bitmap PreProcessImage(Bitmap source)
        {
            // 1. Scale 2x with Bicubic interpolation
            int newWidth = source.Width * 2;
            int newHeight = source.Height * 2;
            Bitmap scaled = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(scaled))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(source, 0, 0, newWidth, newHeight);
            }

            // 2. Convert to Grayscale and Increase Contrast (30%)
            Bitmap processed = new Bitmap(newWidth, newHeight);
            float contrast = 1.3f; // 30% increase
            float translate = 0.5f * (1f - contrast);

            using (Graphics g = Graphics.FromImage(processed))
            {
                var colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][]
                {
                    new float[] {0.299f * contrast, 0.299f * contrast, 0.299f * contrast, 0, 0},
                    new float[] {0.587f * contrast, 0.587f * contrast, 0.587f * contrast, 0, 0},
                    new float[] {0.114f * contrast, 0.114f * contrast, 0.114f * contrast, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {translate, translate, translate, 0, 1}
                });

                using (var attributes = new System.Drawing.Imaging.ImageAttributes())
                {
                    attributes.SetColorMatrix(colorMatrix);
                    g.DrawImage(scaled, new Rectangle(0, 0, newWidth, newHeight),
                        0, 0, newWidth, newHeight, GraphicsUnit.Pixel, attributes);
                }
            }

            scaled.Dispose();
            return processed;
        }
    }
}
