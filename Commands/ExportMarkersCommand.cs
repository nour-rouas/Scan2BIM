using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;

namespace Scan2BIM.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    [Regeneration(RegenerationOption.Manual)]
    public class ExportMarkersCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            try
            {
                IList<Utils.MarkerRecord> markers = Utils.MarkerDataIO.CollectPinpointMarkers(doc);
                if (markers.Count == 0)
                {
                    TaskDialog.Show("Scan2BIM", "No se encontraron marcadores Pinpoint para exportar.");
                    return Result.Cancelled;
                }

                var dialog = new SaveFileDialog
                {
                    Title = "Exportar marcadores de Scan2BIM",
                    Filter = "CSV (*.csv)|*.csv|JSON (*.json)|*.json",
                    FileName = $"Scan2BIM_Markers_{DateTime.Now:yyyyMMdd_HHmmss}",
                    AddExtension = true,
                    OverwritePrompt = true,
                    DefaultExt = ".csv"
                };

                bool? result = dialog.ShowDialog();
                if (result != true)
                    return Result.Cancelled;

                string extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
                if (extension == ".json")
                    Utils.MarkerDataIO.WriteJson(dialog.FileName, markers);
                else
                    Utils.MarkerDataIO.WriteCsv(dialog.FileName, markers);

                Utils.Logger.Log($"ExportMarkers: {markers.Count} markers exported to {dialog.FileName}");
                Utils.Logger.Flush();

                TaskDialog.Show("Scan2BIM",
                    $"Exportados {markers.Count} marcadores.\n\nArchivo:\n{dialog.FileName}");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Utils.Logger.LogError(ex);
                Utils.Logger.Flush();
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
