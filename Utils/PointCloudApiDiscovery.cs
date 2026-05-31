using System;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using Autodesk.Revit.UI;

namespace Metrika.Utils
{
    /// <summary>
    /// Utility para descubrir los métodos reales disponibles en la API de Point Cloud
    /// </summary>
    public static class PointCloudApiDiscovery
    {
        /// <summary>
        /// Descubre y registra todos los métodos y propiedades disponibles en PointCloudInstance
        /// </summary>
        public static void DiscoverPointCloudApi(PointCloudInstance pointCloud)
        {
            Logger.Log("=== DISCOVERING POINTCLOUDINSTANCE API ===");
            
            Type type = typeof(PointCloudInstance);
            
            // Listar todas las propiedades públicas
            Logger.Log("\n--- PUBLIC PROPERTIES ---");
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties.OrderBy(p => p.Name))
            {
                Logger.Log($"  {prop.Name} ({prop.PropertyType.Name}) - CanRead: {prop.CanRead}, CanWrite: {prop.CanWrite}");
            }
            
            // Listar todos los métodos públicos (excluyendo heredados de Object)
            Logger.Log("\n--- PUBLIC METHODS ---");
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var method in methods.OrderBy(m => m.Name))
            {
                if (method.IsSpecialName) continue; // Skip property getters/setters
                
                var parameters = method.GetParameters();
                var paramStr = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Logger.Log($"  {method.ReturnType.Name} {method.Name}({paramStr})");
            }
            
            // Buscar en el namespace Autodesk.Revit.DB.PointClouds
            Logger.Log("\n--- AVAILABLE TYPES IN Autodesk.Revit.DB.PointClouds ---");
            var assembly = typeof(PointCloudInstance).Assembly;
            var pointCloudTypes = assembly.GetTypes()
                .Where(t => t.Namespace == "Autodesk.Revit.DB.PointClouds")
                .OrderBy(t => t.Name);
            
            foreach (var t in pointCloudTypes)
            {
                bool isPublic = t.IsPublic || t.IsNestedPublic;
                Logger.Log($"  {(isPublic ? "[PUBLIC]" : "[INTERNAL]")} {t.Name}");
            }
            
            // Intentar acceder a información de punto cloud específica
            Logger.Log("\n--- POINTCLOUDINSTANCE SPECIFIC INFO ---");
            try
            {
                Logger.Log($"  Element Id: {pointCloud.Id.Value}");
                Logger.Log($"  Category: {pointCloud.Category?.Name ?? "null"}");
                Logger.Log($"  Name: {pointCloud.Name}");
                
                // Buscar parámetros que puedan dar información
                foreach (Parameter param in pointCloud.ParametersMap)
                {
                    if (param.HasValue)
                    {
                        Logger.Log($"  Parameter: {param.Definition.Name} = {param.AsValueString()}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"  Error accessing point cloud info: {ex.Message}");
            }
            
            Logger.Log("\n=== END OF API DISCOVERY ===\n");
        }
        
        /// <summary>
        /// Descubre tipos relacionados con filtros de punto cloud
        /// </summary>
        public static void DiscoverPointCloudFilterTypes()
        {
            Logger.Log("=== DISCOVERING POINT CLOUD FILTER TYPES ===");
            
            var assembly = typeof(PointCloudInstance).Assembly;
            var filterTypes = assembly.GetTypes()
                .Where(t => t.FullName != null && 
                           t.FullName.Contains("PointCloud") && 
                           t.FullName.Contains("Filter"))
                .OrderBy(t => t.FullName);
            
            foreach (var type in filterTypes)
            {
                bool isPublic = type.IsPublic || type.IsNestedPublic;
                Logger.Log($"\n{(isPublic ? "[PUBLIC]" : "[INTERNAL]")} {type.FullName}");
                
                if (isPublic)
                {
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                    foreach (var method in methods.Where(m => !m.IsSpecialName).OrderBy(m => m.Name))
                    {
                        var parameters = method.GetParameters();
                        var paramStr = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        Logger.Log($"    {method.ReturnType.Name} {method.Name}({paramStr})");
                    }
                }
            }
            
            Logger.Log("\n=== END OF FILTER DISCOVERY ===\n");
        }
        
        /// <summary>
        /// Intenta llamar a métodos conocidos de versiones anteriores para ver si existen
        /// </summary>
        public static void TestKnownMethods(PointCloudInstance pointCloud)
        {
            Logger.Log("=== TESTING KNOWN METHODS FROM OLDER VERSIONS ===");
            
            Type type = typeof(PointCloudInstance);
            
            // Intentar GetPointCloudAccess
            var getAccessMethod = type.GetMethod("GetPointCloudAccess");
            Logger.Log($"GetPointCloudAccess() exists: {getAccessMethod != null}");
            
            // Intentar GetPoints
            var getPointsMethod = type.GetMethod("GetPoints");
            Logger.Log($"GetPoints() exists: {getPointsMethod != null}");
            if (getPointsMethod != null)
            {
                var parameters = getPointsMethod.GetParameters();
                var paramStr = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Logger.Log($"  Signature: {getPointsMethod.ReturnType.Name} GetPoints({paramStr})");
            }
            
            // Buscar PointCloudFilterFactory
            var filterFactoryType = assembly.GetType("Autodesk.Revit.DB.PointClouds.PointCloudFilterFactory");
            Logger.Log($"\nPointCloudFilterFactory exists: {filterFactoryType != null}");
            if (filterFactoryType != null)
            {
                var methods = filterFactoryType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    var paramStr = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    Logger.Log($"  {method.ReturnType.Name} {method.Name}({paramStr})");
                }
            }
            
            Logger.Log("\n=== END OF METHOD TESTS ===\n");
        }
        
        private static Assembly assembly => typeof(PointCloudInstance).Assembly;
    }
}
