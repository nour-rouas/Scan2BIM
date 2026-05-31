using System;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Scan2BIM.Utils;

namespace Scan2BIM.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    public class DiscoverPointCloudApiCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            
            try
            {
                Utils.Logger.Log("Starting Point Cloud API Discovery...");
                
                // Primero, descubrir todos los tipos disponibles
                PointCloudApiDiscovery.DiscoverPointCloudFilterTypes();
                
                // Buscar una instancia de punto cloud en el documento
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                PointCloudInstance pointCloud = collector
                    .OfClass(typeof(PointCloudInstance))
                    .FirstOrDefault() as PointCloudInstance;
                
                if (pointCloud == null)
                {
                    Utils.Logger.Log("No se encontró ninguna instancia de Point Cloud en el documento.");
                    Utils.Logger.Log("Por favor, carga un archivo de punto cloud (.rcp, .rcs) en Revit primero.");
                    
                    TaskDialog.Show("Point Cloud API Discovery", 
                        "No point cloud found in the document.\n\n" +
                        "Please load a point cloud file (.rcp, .rcs) into Revit first, then run this command again.");
                    
                    return Result.Cancelled;
                }
                
                Utils.Logger.Log($"\nPoint Cloud encontrado: {pointCloud.Name}");
                
                // Descubrir la API completa
                PointCloudApiDiscovery.DiscoverPointCloudApi(pointCloud);
                PointCloudApiDiscovery.TestKnownMethods(pointCloud);
                
                // Mostrar resultado
                TaskDialog.Show("Point Cloud API Discovery", 
                    "API discovery complete!\n\n" +
                    "Check the log file for detailed information:\n" +
                    $"{Utils.Logger.GetLogFilePath()}");
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                Utils.Logger.Log($"ERROR: {ex.Message}\n{ex.StackTrace}");
                TaskDialog.Show("Error", $"Error during API discovery:\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}
