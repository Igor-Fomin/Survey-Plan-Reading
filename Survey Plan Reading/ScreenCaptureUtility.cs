using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Point3d = Autodesk.AutoCAD.Geometry.Point3d;
using Size = System.Drawing.Size;

namespace Survey_Plan_Reading
{
    public static class ScreenCaptureUtility
    {
        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        /// <summary>
        /// Captures a specific area of the screen defined by two Point3d coordinates.
        /// Handles high-DPI scaling to ensure the capture window aligns with physical pixels.
        /// </summary>
        public static Bitmap CaptureArea(Point3d p1, Point3d p2)
        {
            float scale = GetDpiScaling();
            
            // Log for debugging (will show up in AutoCAD console if called from there)
            // But we can't easily log from here to AutoCAD Editor without passing it.
            
            // Apply DPI scaling to logical coordinates (DIPs) to get physical pixel coordinates
            // Note: If ed.PointToScreen already returns physical pixels, scale should be 1.0.
            // On modern AutoCAD with High DPI, PointToScreen usually returns logical pixels.
            int left = (int)(Math.Min(p1.X, p2.X) * scale);
            int top = (int)(Math.Min(p1.Y, p2.Y) * scale);
            int width = (int)(Math.Abs(p1.X - p2.X) * scale);
            int height = (int)(Math.Abs(p1.Y - p2.Y) * scale);

            if (width <= 0 || height <= 0)
                throw new ArgumentException($"Capture area must have a positive width and height. Calculated: {width}x{height} at ({left},{top}) with scale {scale}");

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
            try
            {
                IntPtr hWnd = Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Handle;
                if (hWnd != IntPtr.Zero)
                {
                    uint dpi = GetDpiForWindow(hWnd);
                    if (dpi > 0) return dpi / 96f;
                }
            }
            catch { }

            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                return g.DpiX / 96f; // Standard DPI is 96
            }
        }

        /// <summary>
        /// Pre-processes the image for better OCR accuracy: 
        /// 2x scale (Bicubic), Grayscale, and Otsu Binarization.
        /// </summary>
        public static Bitmap PreProcessImage(Bitmap source)
        {
            // 1. Scale 2x with Bicubic interpolation
            int newWidth = source.Width * 2;
            int newHeight = source.Height * 2;

            using (Bitmap scaled = new Bitmap(newWidth, newHeight))
            {
                using (Graphics g = Graphics.FromImage(scaled))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(source, 0, 0, newWidth, newHeight);
                }

                // 2. Use OpenCvSharp for Grayscale and Otsu Binarization
                using (Mat mat = BitmapConverter.ToMat(scaled))
                using (Mat gray = new Mat())
                using (Mat binarized = new Mat())
                {
                    // Convert to Grayscale
                    Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);

                    // Apply Otsu Binarization
                    Cv2.Threshold(gray, binarized, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                    // Convert back to Bitmap
                    return BitmapConverter.ToBitmap(binarized);
                }
            }
        }
    }
}
