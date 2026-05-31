using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace Metrika.Utils
{
    /// <summary>
    /// Utilidades de geometría
    /// </summary>
    public static class GeometryUtils
    {
        /// <summary>
        /// Genera un grid de puntos sobre un floor
        /// </summary>
        /// <param name="floor">Floor sobre el cual generar el grid</param>
        /// <param name="spacing">Espaciado entre puntos en metros</param>
        /// <returns>Lista de puntos XYZ</returns>
        public static List<XYZ> GenerateGridOnFloor(Floor floor, double spacing)
        {
            List<XYZ> gridPoints = new List<XYZ>();

            // Convertir spacing de metros a feet
            double spacingFeet = spacing / 0.3048;

            try
            {
                // Obtener geometría del floor
                // ComputeReferences=false: no necesitamos referencias, solo posiciones XYZ
                // DetailLevel=Medium: suficiente para obtener la cara superior, evita costo de Fine
                Options opt = new Options();
                opt.ComputeReferences = false;
                opt.DetailLevel = ViewDetailLevel.Medium;
                GeometryElement geomElem = floor.get_Geometry(opt);

                foreach (GeometryObject geomObj in geomElem)
                {
                    Solid solid = geomObj as Solid;
                    if (solid != null && solid.Faces.Size > 0)
                    {
                        // Buscar la cara superior del floor
                        foreach (Face face in solid.Faces)
                        {
                            PlanarFace planarFace = face as PlanarFace;
                            if (planarFace != null)
                            {
                                // Verificar que es la cara superior (normal apunta hacia arriba)
                                XYZ normal = planarFace.FaceNormal;
                                if (normal.Z > 0.9) // Aproximadamente hacia arriba
                                {
                                    gridPoints = GenerateGridOnFace(planarFace, spacingFeet);
                                    Logger.Log($"Cara superior encontrada, grid generado con spacing {spacing}m");
                                    break;
                                }
                            }
                        }
                        if (gridPoints.Count > 0) break;
                    }
                }

                if (gridPoints.Count == 0)
                {
                    Logger.Log("No se encontró cara superior del floor");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error generando grid: {ex.Message}");
            }

            return gridPoints;
        }

        private static List<XYZ> GenerateGridOnFace(PlanarFace face, double spacing)
        {
            List<XYZ> points = new List<XYZ>();

            try
            {
                // Obtener BoundingBox de la cara
                BoundingBoxUV bbox = face.GetBoundingBox();
                UV min = bbox.Min;
                UV max = bbox.Max;

                // Calcular dimensiones
                double uRange = max.U - min.U;
                double vRange = max.V - min.V;

                Logger.Log($"BoundingBox UV: Min={min}, Max={max}, Range=({uRange}, {vRange})");

                // Generar grid de puntos UV
                int uCount = 0;
                int vCount = 0;

                for (double u = min.U; u <= max.U; u += spacing)
                {
                    vCount = 0;
                    for (double v = min.V; v <= max.V; v += spacing)
                    {
                        UV point = new UV(u, v);

                        // Verificar si el punto está dentro de la cara
                        if (face.IsInside(point))
                        {
                            XYZ xyz = face.Evaluate(point);
                            points.Add(xyz);
                            vCount++;
                        }
                    }
                    uCount++;
                }

                Logger.Log($"Grid generado: {uCount}x{vCount} = {points.Count} puntos dentro del contorno");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error en GenerateGridOnFace: {ex.Message}");
            }

            return points;
        }

        /// <summary>
        /// Verifica si un punto está dentro de un polígono (2D)
        /// </summary>
        public static bool IsPointInsidePolygon(XYZ point, List<XYZ> polygon)
        {
            // Algoritmo Ray Casting
            int intersections = 0;
            int count = polygon.Count;

            for (int i = 0; i < count; i++)
            {
                XYZ p1 = polygon[i];
                XYZ p2 = polygon[(i + 1) % count];

                if (RayIntersectsSegment(point, p1, p2))
                {
                    intersections++;
                }
            }

            return (intersections % 2 == 1);
        }

        private static bool RayIntersectsSegment(XYZ point, XYZ p1, XYZ p2)
        {
            // Rayo horizontal desde 'point' hacia la derecha
            if (p1.Y > p2.Y)
            {
                XYZ temp = p1;
                p1 = p2;
                p2 = temp;
            }

            if (point.Y == p1.Y || point.Y == p2.Y)
            {
                point = new XYZ(point.X, point.Y + 0.0001, point.Z);
            }

            if (point.Y < p1.Y || point.Y > p2.Y)
            {
                return false;
            }

            if (point.X >= Math.Max(p1.X, p2.X))
            {
                return false;
            }

            if (point.X < Math.Min(p1.X, p2.X))
            {
                return true;
            }

            double red = (point.Y - p1.Y) / (p2.Y - p1.Y);
            double blue = (point.X - p1.X) / (p2.X - p1.X);

            return blue >= red;
        }
    }
}
