using System;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.Geometry;

namespace Survey_Plan_Reading
{
    public static class ScreenCaptureUtility
    {
        /// <summary>
        /// Captures a specific area of the screen defined by two Point3d coordinates.
        /// Assumes the Point3d coordinates have already been converted to screen pixels.
        /// Handles high-DPI scaling to maintain OCR accuracy.
        /// </summary>
        public static Bitmap CaptureArea(Point3d p1, Point3d p2)
        {
            // Determine the capture rectangle
            int left = (int)Math.Min(p1.X, p2.X);
            int top = (int)Math.Min(p1.Y, p2.Y);
            int width = (int)Math.Abs(p1.X - p2.X);
            int height = (int)Math.Abs(p1.Y - p2.Y);

            // Ensure dimensions are valid
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Capture area must have a positive width and height.");

            // Create a bitmap with the specified dimensions
            // Note: System.Drawing.Common is used here; ensure it's referenced in the project.
            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // In high-DPI scenarios, the screen coordinates might be scaled.
                // CopyFromScreen uses pixel coordinates.
                g.CopyFromScreen(left, top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }

        /// <summary>
        /// Example of how to handle DPI scaling factor if needed for manual adjustments.
        /// </summary>
        private static float GetDpiScaling()
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                return g.DpiX / 96f; // Standard DPI is 96
            }
        }
    }
}
