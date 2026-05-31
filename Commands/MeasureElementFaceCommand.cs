using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Scan2BIM.Commands
{
    /// <summary>
    /// Lets the user click any face of a common building element
    /// (floor, roof, wall, stair, railing, ceiling, structural column/frame,
    ///  ramp, generic model) and reports — or marks — the exact XYZ.
    ///
    /// Works in both plan and 3D views.
    /// Uses PickObject(ObjectType.Face) which is purely UI-driven:
    /// no document scanning, no background work → zero performance overhead.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class MeasureElementFaceCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document   doc   = uidoc.Document;

            try
            {
                Reference reference;
                try
                {
                    reference = uidoc.Selection.PickObject(
                        ObjectType.Face,
                        new BuildingElementFaceFilter(doc),
                        "Haz clic sobre la cara de un elemento (suelo, muro, techo, escalera…)");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }

                // GlobalPoint is the exact surface point the user clicked.
                XYZ point = reference.GlobalPoint;
                if (point == null)
                {
                    TaskDialog.Show("Scan2BIM", "No se pudo obtener la posición del punto.");
                    return Result.Failed;
                }

                // ── Element description ──────────────────────────────────────
                Element elem = doc.GetElement(reference);
                string elemDesc = elem != null
                    ? $"{elem.Category?.Name ?? elem.GetType().Name}  (Id {elem.Id})"
                    : "Elemento desconocido";

                // ── Convert units ────────────────────────────────────────────
                Units        projectUnits = doc.GetUnits();
                ForgeTypeId  lengthUnit   = projectUnits
                                               .GetFormatOptions(SpecTypeId.Length)
                                               .GetUnitTypeId();
                string unitLabel = LabelUtils.GetLabelForUnit(lengthUnit);

                double x  = UnitUtils.ConvertFromInternalUnits(point.X, lengthUnit);
                double y  = UnitUtils.ConvertFromInternalUnits(point.Y, lengthUnit);
                double z  = UnitUtils.ConvertFromInternalUnits(point.Z, lengthUnit);

                double xM = UnitUtils.ConvertFromInternalUnits(point.X, UnitTypeId.Meters);
                double yM = UnitUtils.ConvertFromInternalUnits(point.Y, UnitTypeId.Meters);
                double zM = UnitUtils.ConvertFromInternalUnits(point.Z, UnitTypeId.Meters);

                // ── Dialog ───────────────────────────────────────────────────
                string mainContent =
                    $"  X:  {x:F4} {unitLabel}\n" +
                    $"  Y:  {y:F4} {unitLabel}\n" +
                    $"  Z:  {z:F4} {unitLabel}\n\n" +
                    $"─────────────────────────────\n" +
                    $"  X:  {xM:F4} m\n" +
                    $"  Y:  {yM:F4} m\n" +
                    $"  Z:  {zM:F4} m";

                string clipboardText = $"X={x:F4} Y={y:F4} Z={z:F4} ({unitLabel})";

                TaskDialog td = new TaskDialog("Scan2BIM — Punto en Elemento");
                td.MainInstruction = elemDesc;
                td.MainContent     = mainContent;
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1,
                    "Crear marcador en este punto",
                    "Coloca un Modelo Genérico (pinpoint) con X, Y, Z almacenados");
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2,
                    "Copiar al portapapeles", clipboardText);
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Cerrar");
                td.DefaultButton = TaskDialogResult.CommandLink3;

                TaskDialogResult result = td.Show();

                if (result == TaskDialogResult.CommandLink1)
                {
                    using (Transaction trans = new Transaction(doc, "Scan2BIM: Marcador Elemento"))
                    {
                        trans.Start();
                        Utils.PinpointHelper.CreatePinpoint(doc, point, xM, yM, zM,
                            mark: $"ELEM  Z={zM:F3} m");
                        trans.Commit();
                    }
                    TaskDialog.Show("Scan2BIM", "Marcador creado exitosamente");
                }
                else if (result == TaskDialogResult.CommandLink2)
                {
                    System.Windows.Clipboard.SetText(clipboardText);
                }

                Utils.Logger.Flush();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                Utils.Logger.LogError(ex);
                Utils.Logger.Flush();
                return Result.Failed;
            }
        }
    }

    // ── Selection filter ─────────────────────────────────────────────────────

    /// <summary>
    /// Allows faces on the most common building-element categories plus
    /// toposolid / topography surfaces.
    /// The filter is intentionally permissive: only rejects elements that
    /// have no meaningful face (detail items, annotation, etc.).
    /// </summary>
    internal sealed class BuildingElementFaceFilter : ISelectionFilter
    {
        private static readonly HashSet<BuiltInCategory> _allowed =
            new HashSet<BuiltInCategory>
            {
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_Roofs,
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Stairs,
                BuiltInCategory.OST_StairsRailing,
                BuiltInCategory.OST_Ceilings,
                BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_StructuralFraming,
                BuiltInCategory.OST_StructuralFoundation,
                BuiltInCategory.OST_Ramps,
                BuiltInCategory.OST_GenericModel,
                BuiltInCategory.OST_Toposolid,
                BuiltInCategory.OST_TopographySurface,
                BuiltInCategory.OST_Columns,          // architectural columns
                BuiltInCategory.OST_Doors,
                BuiltInCategory.OST_Windows,
            };

        private readonly Document _doc;

        public BuildingElementFaceFilter(Document doc) { _doc = doc; }

        public bool AllowElement(Element elem)
        {
            if (elem?.Category == null) return false;
            var cat = (BuiltInCategory)(int)elem.Category.Id.Value;
            return _allowed.Contains(cat);
        }

        public bool AllowReference(Reference reference, XYZ position) => true;
    }
}
