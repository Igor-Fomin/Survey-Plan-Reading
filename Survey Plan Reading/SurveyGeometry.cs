using Autodesk.AutoCAD.Geometry;

namespace Survey_Plan_Reading
{
    public static class SurveyGeometry
    {
        /// <summary>
        /// Converts Degrees, Minutes, Seconds to Decimal Radians.
        /// North is 0 degrees, increasing clockwise.
        /// </summary>
        public static double DmsToRadians(double degrees, double minutes, double seconds)
        {
            double decimalDegrees = degrees + (minutes / 60.0) + (seconds / 3600.0);
            return decimalDegrees * (Math.PI / 180.0);
        }

        /// <summary>
        /// Calculates the end point based on a start point, distance, and bearing (in radians).
        /// North is 0 degrees (0 radians), increasing clockwise.
        /// Uses DeltaX = d * sin(theta) and DeltaY = d * cos(theta).
        /// </summary>
        public static Point3d GetEndPoint(Point3d start, double distance, double bearing)
        {
            double deltaX = distance * Math.Sin(bearing);
            double deltaY = distance * Math.Cos(bearing);

            return new Point3d(start.X + deltaX, start.Y + deltaY, start.Z);
        }
    }
}
