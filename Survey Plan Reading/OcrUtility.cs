using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Survey_Plan_Reading
{
    public static class OcrUtility
    {
        // Distance: Matches whole numbers or decimals.
        // Also handles comma as decimal separator and optional "Dist/L/Length" prefixes.
        private static readonly Regex DistanceRegex = new Regex(@"(?:Dist|L|Length)?[:=]?\s*(\d+(?:[.,]\d+)?)\b", RegexOptions.IgnoreCase);

        // Bearing: Highly flexible DMS pattern
        // Group 1: Degrees, Group 2: Minutes (optional), Group 3: Seconds (optional)
        // Symbols for degrees: °, 0, o, O, 8, *
        // Symbols for minutes: ', ’, l, 1, i
        // Symbols for seconds: ", '', ”
        // Uses \s* to handle spaces between numbers and symbols.
        private static readonly Regex BearingRegex = new Regex(@"(\d{1,3})\s*[°0oO8\*]?\s*(?:(\d{1,2})\s*['’l1i]?\s*)?(?:(\d{1,2})\s*[""''”l1i]{0,2})?", RegexOptions.IgnoreCase);

        public static List<double> ParseDistances(string text)
        {
            var distances = new List<double>();
            foreach (Match match in DistanceRegex.Matches(text))
            {
                if (match.Groups[1].Success)
                {
                    string val = match.Groups[1].Value.Replace(',', '.');
                    if (double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d))
                        distances.Add(d);
                }
            }
            
            // Fallback: If no "prefixed" distance found, look for any number that looks like a distance (likely 2+ digits or decimal)
            if (distances.Count == 0)
            {
                var fallbackRegex = new Regex(@"\b(\d+(?:\.\d+)?)\b");
                foreach (Match match in fallbackRegex.Matches(text))
                {
                    if (double.TryParse(match.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d))
                    {
                        // Filter out very small or very large numbers if they don't look like typical survey distances
                        if (d > 0.1 && d < 10000) 
                            distances.Add(d);
                    }
                }
            }
            return distances;
        }

        public static List<(double D, double M, double S)> ParseDmsBearings(string text)
        {
            var bearings = new List<(double, double, double)>();
            foreach (Match match in BearingRegex.Matches(text))
            {
                if (double.TryParse(match.Groups[1].Value, out double d))
                {
                    double m = 0;
                    double s = 0;

                    if (match.Groups[2].Success) double.TryParse(match.Groups[2].Value, out m);
                    if (match.Groups[3].Success) double.TryParse(match.Groups[3].Value, out s);

                    // Basic validation for a bearing
                    if (d >= 0 && d <= 360 && m < 60 && s < 60)
                    {
                        // To avoid matching every single number as a degree, 
                        // we prioritize matches that have at least minutes OR a clear degree symbol
                        bool hasSymbol = Regex.IsMatch(match.Value, @"[°oO8\*'""’]");
                        if (match.Groups[2].Success || hasSymbol)
                        {
                            bearings.Add((d, m, s));
                        }
                    }
                }
            }
            return bearings;
        }
    }
}
