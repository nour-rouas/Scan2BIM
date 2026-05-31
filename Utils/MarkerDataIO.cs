using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;

namespace Scan2BIM.Utils
{
    [DataContract]
    internal sealed class MarkerRecord
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "mark")]
        public string Mark { get; set; }

        [DataMember(Name = "comments")]
        public string Comments { get; set; }

        [DataMember(Name = "x_m")]
        public double XMeters { get; set; }

        [DataMember(Name = "y_m")]
        public double YMeters { get; set; }

        [DataMember(Name = "z_m")]
        public double ZMeters { get; set; }

        [DataMember(Name = "source")]
        public string Source { get; set; }
    }

    internal static class MarkerDataIO
    {
        private static readonly Regex CoordinateRegex = new Regex(
            @"X\s*=\s*([+-]?\d+(?:[\.,]\d+)?)\D+Y\s*=\s*([+-]?\d+(?:[\.,]\d+)?)\D+Z\s*=\s*([+-]?\d+(?:[\.,]\d+)?)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IList<MarkerRecord> CollectPinpointMarkers(Document doc)
        {
            var markers = new List<MarkerRecord>();

            var elements = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (Element element in elements)
            {
                if (!IsPinpoint(element))
                    continue;

                MarkerRecord record = BuildMarkerRecord(element);
                if (record != null)
                    markers.Add(record);
            }

            return markers;
        }

        public static void WriteCsv(string filePath, IList<MarkerRecord> markers)
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine("id,mark,comments,x_m,y_m,z_m,source");

                foreach (MarkerRecord marker in markers)
                {
                    string line = string.Join(",",
                        marker.Id.ToString(CultureInfo.InvariantCulture),
                        EscapeCsv(marker.Mark),
                        EscapeCsv(marker.Comments),
                        marker.XMeters.ToString("F6", CultureInfo.InvariantCulture),
                        marker.YMeters.ToString("F6", CultureInfo.InvariantCulture),
                        marker.ZMeters.ToString("F6", CultureInfo.InvariantCulture),
                        EscapeCsv(marker.Source));

                    writer.WriteLine(line);
                }
            }
        }

        public static void WriteJson(string filePath, IList<MarkerRecord> markers)
        {
            var serializer = new DataContractJsonSerializer(typeof(List<MarkerRecord>));
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                serializer.WriteObject(stream, markers.ToList());
            }
        }

        public static IList<MarkerRecord> ReadCsv(string filePath)
        {
            var records = new List<MarkerRecord>();
            string[] lines = File.ReadAllLines(filePath);

            if (lines.Length == 0)
                return records;

            int startIndex = 0;
            Dictionary<string, int> headerIndex = null;

            List<string> firstColumns = ParseCsvLine(lines[0]);
            if (IsHeaderRow(firstColumns))
            {
                headerIndex = BuildHeaderIndex(firstColumns);
                startIndex = 1;
            }

            for (int i = startIndex; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                List<string> columns = ParseCsvLine(line);
                MarkerRecord record = ParseCsvRecord(columns, headerIndex);
                if (record != null)
                    records.Add(record);
            }

            return records;
        }

        public static IList<MarkerRecord> ReadJson(string filePath)
        {
            var serializer = new DataContractJsonSerializer(typeof(List<MarkerRecord>));
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var result = serializer.ReadObject(stream) as List<MarkerRecord>;
                return result ?? new List<MarkerRecord>();
            }
        }

        private static MarkerRecord BuildMarkerRecord(Element element)
        {
            string mark = element.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;
            string comments = element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? string.Empty;

            double xMeters;
            double yMeters;
            double zMeters;
            string source;

            if (TryReadMetersFromComments(comments, out xMeters, out yMeters, out zMeters))
            {
                source = "comments";
            }
            else if (TryReadMetersFromGeometry(element, out xMeters, out yMeters, out zMeters))
            {
                source = "geometry";
            }
            else
            {
                return null;
            }

            return new MarkerRecord
            {
                Id = (int)element.Id.Value,
                Mark = mark,
                Comments = comments,
                XMeters = xMeters,
                YMeters = yMeters,
                ZMeters = zMeters,
                Source = source
            };
        }

        private static bool IsPinpoint(Element element)
        {
            if (element == null)
                return false;

            if (string.Equals(element.Name, "Pinpoint", StringComparison.OrdinalIgnoreCase))
                return true;

            string comments = element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString();
            return TryReadMetersFromComments(comments, out _, out _, out _);
        }

        private static bool TryReadMetersFromComments(string comments, out double x, out double y, out double z)
        {
            x = 0;
            y = 0;
            z = 0;

            if (string.IsNullOrWhiteSpace(comments))
                return false;

            Match match = CoordinateRegex.Match(comments);
            if (!match.Success || match.Groups.Count < 4)
                return false;

            return TryParseDoubleFlexible(match.Groups[1].Value, out x)
                && TryParseDoubleFlexible(match.Groups[2].Value, out y)
                && TryParseDoubleFlexible(match.Groups[3].Value, out z);
        }

        private static bool TryReadMetersFromGeometry(Element element, out double x, out double y, out double z)
        {
            x = 0;
            y = 0;
            z = 0;

            XYZ point = null;

            LocationPoint locationPoint = element.Location as LocationPoint;
            if (locationPoint != null)
                point = locationPoint.Point;

            if (point == null)
            {
                BoundingBoxXYZ bbox = element.get_BoundingBox(null);
                if (bbox != null)
                    point = (bbox.Min + bbox.Max) * 0.5;
            }

            if (point == null)
                return false;

            x = UnitUtils.ConvertFromInternalUnits(point.X, UnitTypeId.Meters);
            y = UnitUtils.ConvertFromInternalUnits(point.Y, UnitTypeId.Meters);
            z = UnitUtils.ConvertFromInternalUnits(point.Z, UnitTypeId.Meters);
            return true;
        }

        private static bool TryParseDoubleFlexible(string text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string cleaned = text.Trim();
            if (double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;

            if (double.TryParse(cleaned, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                return true;

            cleaned = cleaned.Replace(" ", string.Empty);
            if (double.TryParse(cleaned.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;

            return false;
        }

        private static string EscapeCsv(string value)
        {
            if (value == null)
                return string.Empty;

            bool shouldQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
            if (!shouldQuote)
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static bool IsHeaderRow(List<string> columns)
        {
            if (columns == null || columns.Count == 0)
                return false;

            return columns.Any(c => string.Equals(c.Trim(), "x_m", StringComparison.OrdinalIgnoreCase))
                && columns.Any(c => string.Equals(c.Trim(), "y_m", StringComparison.OrdinalIgnoreCase))
                && columns.Any(c => string.Equals(c.Trim(), "z_m", StringComparison.OrdinalIgnoreCase));
        }

        private static Dictionary<string, int> BuildHeaderIndex(List<string> headers)
        {
            var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                string key = headers[i]?.Trim();
                if (!string.IsNullOrEmpty(key) && !index.ContainsKey(key))
                    index.Add(key, i);
            }
            return index;
        }

        private static MarkerRecord ParseCsvRecord(IList<string> columns, Dictionary<string, int> headerIndex)
        {
            int idIndex;
            int markIndex;
            int commentsIndex;
            int xIndex;
            int yIndex;
            int zIndex;
            int sourceIndex;

            if (headerIndex != null)
            {
                idIndex = TryGetIndex(headerIndex, "id", 0);
                markIndex = TryGetIndex(headerIndex, "mark", 1);
                commentsIndex = TryGetIndex(headerIndex, "comments", 2);
                xIndex = TryGetIndex(headerIndex, "x_m", 3);
                yIndex = TryGetIndex(headerIndex, "y_m", 4);
                zIndex = TryGetIndex(headerIndex, "z_m", 5);
                sourceIndex = TryGetIndex(headerIndex, "source", 6);
            }
            else
            {
                idIndex = 0;
                markIndex = 1;
                commentsIndex = 2;
                xIndex = 3;
                yIndex = 4;
                zIndex = 5;
                sourceIndex = 6;

                if (columns.Count >= 3)
                {
                    xIndex = 0;
                    yIndex = 1;
                    zIndex = 2;
                }
            }

            if (!TryGetDouble(columns, xIndex, out double x)
                || !TryGetDouble(columns, yIndex, out double y)
                || !TryGetDouble(columns, zIndex, out double z))
            {
                return null;
            }

            return new MarkerRecord
            {
                Id = TryGetInt(columns, idIndex),
                Mark = GetValue(columns, markIndex),
                Comments = GetValue(columns, commentsIndex),
                XMeters = x,
                YMeters = y,
                ZMeters = z,
                Source = string.IsNullOrWhiteSpace(GetValue(columns, sourceIndex)) ? "file" : GetValue(columns, sourceIndex)
            };
        }

        private static int TryGetIndex(Dictionary<string, int> map, string key, int fallback)
        {
            if (map.TryGetValue(key, out int idx))
                return idx;
            return fallback;
        }

        private static bool TryGetDouble(IList<string> values, int index, out double result)
        {
            result = 0;
            string text = GetValue(values, index);
            return TryParseDoubleFlexible(text, out result);
        }

        private static int TryGetInt(IList<string> values, int index)
        {
            string text = GetValue(values, index);
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                return result;
            return -1;
        }

        private static string GetValue(IList<string> values, int index)
        {
            if (values == null || index < 0 || index >= values.Count)
                return string.Empty;
            return values[index]?.Trim() ?? string.Empty;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            if (line == null)
            {
                values.Add(string.Empty);
                return values;
            }

            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(c);
            }

            values.Add(current.ToString());
            return values;
        }
    }
}
