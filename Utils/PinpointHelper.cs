using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace Scan2BIM.Utils
{
    /// <summary>
    /// Creates DirectShape "pinpoint" markers in the model.
    /// Each marker stores X, Y, Z (meters) in the element's
    /// Mark and Comments parameters so they are visible in
    /// element properties and can be scheduled or filtered later.
    ///
    /// Uses simple 3-axis line geometry (X=red, Y=green, Z=blue) 
    /// similar to ScanToBIMs plugin for maximum visibility and reliability.
    ///
    /// Must be called inside an open Transaction.
    /// </summary>
    internal static class PinpointHelper
    {
        // Axis length in Revit internal units (feet): ~1 ft = 30 cm
        private const double AxisLength = 1.0;

        /// <summary>
        /// Creates a 3-axis crosshair Generic Model DirectShape at
        /// <paramref name="position"/> and writes X, Y, Z (metres) to the
        /// element's Mark and Comments parameters.
        /// </summary>
        /// <param name="doc">Active Revit document.</param>
        /// <param name="position">Placement point in Revit internal units (feet).</param>
        /// <param name="xMeters">X coordinate in metres to store as parameter.</param>
        /// <param name="yMeters">Y coordinate in metres to store as parameter.</param>
        /// <param name="zMeters">Z coordinate in metres to store as parameter.</param>
        /// <param name="mark">Optional Mark text; defaults to "PT  Z=…m".</param>
        /// <returns>The created <see cref="DirectShape"/>.</returns>
        public static DirectShape CreatePinpoint(
            Document doc,
            XYZ position,
            double xMeters,
            double yMeters,
            double zMeters,
            string mark = null)
        {
            DirectShape ds = null;
            
            try
            {
                ds = DirectShape.CreateElement(
                    doc, new ElementId(BuiltInCategory.OST_GenericModel));

                // Set name to make it identifiable
                ds.Name = "Pinpoint";

                // Build geometry at local origin and move the element afterwards.
                // This keeps shape coordinates stable and makes placement explicit.
                IList<GeometryObject> geom = BuildAxisCrosshair(XYZ.Zero, AxisLength);
                if (geom != null && geom.Count > 0)
                {
                    ds.SetShape(geom);

                    // Apply placement as an element transform from origin to picked point.
                    ElementTransformUtils.MoveElement(doc, ds.Id, position);
                }
                else
                {
                    Logger.Log("PinpointHelper: No geometry objects created");
                }

                // Set parameters
                string markText    = mark ?? $"PT  Z={zMeters:F3} m";
                string commentText = $"X={xMeters:F4}  Y={yMeters:F4}  Z={zMeters:F4}  (m)";

                Parameter markParam = ds.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                if (markParam != null && !markParam.IsReadOnly)
                    markParam.Set(markText);

                Parameter commentParam = ds.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                if (commentParam != null && !commentParam.IsReadOnly)
                    commentParam.Set(commentText);

                // Best effort: write elevation to common instance parameters when available.
                bool elevationWritten = TrySetElevationParameter(ds, position.Z);

                if (!elevationWritten)
                    Logger.Log("PinpointHelper: No writable elevation parameter found on DirectShape");

                Logger.Log($"Pinpoint created at X={xMeters:F3}, Y={yMeters:F3}, Z={zMeters:F3} m");
            }
            catch (Exception ex)
            {
                Logger.Log($"PinpointHelper ERROR: {ex.Message}");
                Logger.Log($"Stack: {ex.StackTrace}");
                throw; // Re-throw so the transaction can handle it
            }

            return ds;
        }

        /// <summary>
        /// Attempts to write the elevation in internal units (feet) to common
        /// elevation/offset parameters if present on the instance.
        /// </summary>
        private static bool TrySetElevationParameter(Element element, double zInternalFeet)
        {
            if (element == null)
                return false;

            string[] candidateNames =
            {
                "Elevation from Level",
                "Offset from Level",
                "Base Offset",
                "Offset",
                "Elevación desde nivel",
                "Desfase desde nivel",
                "Desfase de base",
                "Cota"
            };

            foreach (string parameterName in candidateNames)
            {
                Parameter parameter = element.LookupParameter(parameterName);
                if (parameter == null || parameter.IsReadOnly)
                    continue;

                if (parameter.StorageType != StorageType.Double)
                    continue;

                parameter.Set(zInternalFeet);
                Logger.Log($"PinpointHelper: Wrote elevation to parameter '{parameterName}'");
                return true;
            }

            return false;
        }

        // ── Internal geometry ────────────────────────────────────────────────

        /// <summary>
        /// Builds a 3-axis crosshair using simple line segments:
        /// X-axis (red), Y-axis (green), Z-axis (blue).
        /// Simple and reliable - always visible in any view.
        /// </summary>
        private static IList<GeometryObject> BuildAxisCrosshair(XYZ center, double length)
        {
            List<GeometryObject> curves = new List<GeometryObject>();

            try
            {
                // X-axis: red line along X direction
                XYZ xStart = new XYZ(center.X - length / 2, center.Y, center.Z);
                XYZ xEnd   = new XYZ(center.X + length / 2, center.Y, center.Z);
                Line xLine = Line.CreateBound(xStart, xEnd);
                curves.Add(xLine);

                // Y-axis: green line along Y direction  
                XYZ yStart = new XYZ(center.X, center.Y - length / 2, center.Z);
                XYZ yEnd   = new XYZ(center.X, center.Y + length / 2, center.Z);
                Line yLine = Line.CreateBound(yStart, yEnd);
                curves.Add(yLine);

                // Z-axis: blue line along Z direction (vertical)
                XYZ zStart = new XYZ(center.X, center.Y, center.Z - length / 2);
                XYZ zEnd   = new XYZ(center.X, center.Y, center.Z + length / 2);
                Line zLine = Line.CreateBound(zStart, zEnd);
                curves.Add(zLine);

                Logger.Log($"Built 3 axis lines at center {center}");
            }
            catch (Exception ex)
            {
                Logger.Log($"BuildAxisCrosshair error: {ex.Message}");
            }

            return curves;
        }
    }
}
