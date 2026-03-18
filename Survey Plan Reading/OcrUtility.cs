using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Survey_Plan_Reading
{
    public static class OcrUtility
    {
        // Distance: Matches decimals like 204.7, 77.21. 
        // Also handles comma as decimal separator and optional "Dist/L" prefixes.
        private static readonly Regex DistanceRegex = new Regex(@"(?:Dist|L|Length)?[:=]?\s*(\d+[.,]\d+)\b", RegexOptions.IgnoreCase);

        // Bearing: Highly flexible DMS pattern
        // Group 1: Degrees, Group 2: Minutes (optional), Group 3: Seconds (optional)
        // Handles mistakes like '0' or 'o' for degrees, and 'l' or '1' for minutes/seconds.
        private static readonly Regex BearingRegex = new Regex(@"(\d{1,3})[\s°0oO]*\s*(?:(\d{1,2})[\s'’l1]*\s*)?(?:(\d{1,2})[\s""'’l1]{0,2})?", RegexOptions.IgnoreCase);

        public static List<double> ParseDistances(string text)
        {
            var distances = new List<double>();
            foreach (Match match in DistanceRegex.Matches(text))
            {
                string val = match.Groups[1].Value.Replace(',', '.');
                if (double.TryParse(val, out double d))
                    distances.Add(d);
            }
            // Fallback: If no "prefixed" distance found, just look for any decimal
            if (distances.Count == 0)
            {
                var fallbackRegex = new Regex(@"\b(\d+\.\d+)\b");
                foreach (Match match in fallbackRegex.Matches(text))
                {
                    if (double.TryParse(match.Value, out double d))
                        distances.Add(d);
                }
            }
            return distances;
        }

        public static List<(double D, double M, double S)> ParseDmsBearings(string text)
        {
            var bearings = new List<(double, double, double)>();
            foreach (Match match in BearingRegex.Matches(text))
            {
                double d = double.Parse(match.Groups[1].Value);
                double m = 0;
                double s = 0;

                if (match.Groups[2].Success) double.TryParse(match.Groups[2].Value, out m);
                if (match.Groups[3].Success) double.TryParse(match.Groups[3].Value, out s);

                // Basic validation for a bearing
                if (d >= 0 && d <= 360)
                {
                    bearings.Add((d, m, s));
                }
            }
            return bearings;
        }
    }
}
