using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

namespace Metrika.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class HideUnhidePointCloudCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            View activeView = uidoc.ActiveView;

            try
            {
                // Obtener todas las instancias de nube de puntos en el documento
                IList<Element> pointClouds = new FilteredElementCollector(doc)
                    .OfClass(typeof(PointCloudInstance))
                    .ToElements();

                if (pointClouds.Count == 0)
                {
                    TaskDialog.Show("Scan2BIM", "No se encontraron nubes de puntos en el documento.");
                    return Result.Cancelled;
                }

                // Comprobar si alguna nube está visible en la vista activa
                bool anyVisible = pointClouds.Any(pc => !pc.IsHidden(activeView));

                using (Transaction tx = new Transaction(doc, "Scan2BIM - Hide/Unhide Point Cloud"))
                {
                    tx.Start();

                    if (anyVisible)
                    {
                        // Ocultar todas las nubes visibles
                        ICollection<ElementId> toHide = pointClouds
                            .Where(pc => !pc.IsHidden(activeView))
                            .Select(pc => pc.Id)
                            .ToList();
                        activeView.HideElements(toHide);
                    }
                    else
                    {
                        // Mostrar todas las nubes ocultas
                        ICollection<ElementId> toUnhide = pointClouds
                            .Where(pc => pc.IsHidden(activeView))
                            .Select(pc => pc.Id)
                            .ToList();
                        activeView.UnhideElements(toUnhide);
                    }

                    tx.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
