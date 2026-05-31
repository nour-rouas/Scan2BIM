using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;

namespace Scan2BIM.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ImportMarkersCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Importar marcadores a Scan2BIM",
                    Filter = "CSV (*.csv)|*.csv|JSON (*.json)|*.json",
                    Multiselect = false,
                    CheckFileExists = true
                };

                bool? result = dialog.ShowDialog();
                if (result != true)
                    return Result.Cancelled;

                IList<Utils.MarkerRecord> records;
                string extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();

                if (extension == ".json")
                    records = Utils.MarkerDataIO.ReadJson(dialog.FileName);
                else
                    records = Utils.MarkerDataIO.ReadCsv(dialog.FileName);

                if (records.Count == 0)
                {
                    TaskDialog.Show("Scan2BIM", "El archivo no contiene marcadores válidos para importar.");
                    return Result.Cancelled;
                }

                int created = 0;
                int skipped = 0;

                using (var trans = new Transaction(doc, "Scan2BIM: Importar Marcadores"))
                {
                    trans.Start();

                    foreach (Utils.MarkerRecord record in records)
                    {
                        if (!IsValid(record))
                        {
                            skipped++;
                            continue;
                        }

                        XYZ point = new XYZ(
                            UnitUtils.ConvertToInternalUnits(record.XMeters, UnitTypeId.Meters),
                            UnitUtils.ConvertToInternalUnits(record.YMeters, UnitTypeId.Meters),
                            UnitUtils.ConvertToInternalUnits(record.ZMeters, UnitTypeId.Meters));

                        string mark = string.IsNullOrWhiteSpace(record.Mark)
                            ? $"IMP  Z={record.ZMeters:F3} m"
                            : record.Mark;

                        Utils.PinpointHelper.CreatePinpoint(
                            doc,
                            point,
                            record.XMeters,
                            record.YMeters,
                            record.ZMeters,
                            mark);

                        created++;
                    }

                    trans.Commit();
                }

                Utils.Logger.Log($"ImportMarkers: {created} created, {skipped} skipped from {dialog.FileName}");
                Utils.Logger.Flush();

                TaskDialog.Show("Scan2BIM",
                    $"Importación completada.\n\nCreados: {created}\nOmitidos: {skipped}\n\nArchivo:\n{dialog.FileName}");

                return created > 0 ? Result.Succeeded : Result.Cancelled;
            }
            catch (Exception ex)
            {
                Utils.Logger.LogError(ex);
                Utils.Logger.Flush();
                message = ex.Message;
                return Result.Failed;
            }
        }

        private static bool IsValid(Utils.MarkerRecord record)
        {
            if (record == null)
                return false;

            return !(double.IsNaN(record.XMeters)
                || double.IsNaN(record.YMeters)
                || double.IsNaN(record.ZMeters)
                || double.IsInfinity(record.XMeters)
                || double.IsInfinity(record.YMeters)
                || double.IsInfinity(record.ZMeters));
        }
    }
}
