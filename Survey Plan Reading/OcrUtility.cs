using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Survey_Plan_Reading
{
    public static class OcrUtility
    {
        // Matches distances like '204.7' or '77.21'
        private static readonly Regex DistanceRegex = new Regex(@"\b(\d+(\.\d+)?)\b");

        // Matches bearings like '270', '90', or common OCR errors like '2700', '270o', '270°'
        // This regex looks for 1-3 digits followed by a symbol that might be the degree mark.
        private static readonly Regex BearingRegex = new Regex(@"\b(\d{1,3})([0o°]?)\b");

        /// <summary>
        /// Parses survey plan text to identify distances.
        /// </summary>
        public static List<double> ParseDistances(string text)
        {
            var distances = new List<double>();
            var matches = DistanceRegex.Matches(text);

            foreach (Match match in matches)
            {
                if (double.TryParse(match.Value, out double distance))
                {
                    distances.Add(distance);
                }
            }

            return distances;
        }

        /// <summary>
        /// Parses survey plan text to identify bearings, cleaning OCR errors.
        /// </summary>
        public static List<double> ParseBearings(string text)
        {
            var bearings = new List<double>();
            var matches = BearingRegex.Matches(text);

            foreach (Match match in matches)
            {
                string value = match.Groups[1].Value;
                if (double.TryParse(value, out double bearing))
                {
                    // Basic validation: bearings are typically 0-360
                    if (bearing >= 0 && bearing <= 360)
                    {
                        bearings.Add(bearing);
                    }
                }
            }

            return bearings;
        }

        /// <summary>
        /// Cleans a specific OCR string where the degree symbol might be misinterpreted.
        /// </summary>
        public static string CleanOcrError(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Replace 'o' or '0' at the end of a suspected bearing with '°' if it seems appropriate
            // Or just remove them to get the raw numeric value.
            return Regex.Replace(input, @"(\d{1,3})[0o]$","$1°");
        }
    }
}
