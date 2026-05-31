using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.Attributes;

namespace Scan2BIM.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class PickPointCoordinatesCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                XYZ point = null;

                // Prefer PointOnElement to get true 3D point from point cloud geometry.
                // This avoids plan/work-plane picks that can force Z = 0.
                try
                {
                    Reference reference = uidoc.Selection.PickObject(
                        ObjectType.PointOnElement,
                        new PointCloudSelectionFilter(),
                        "Haz clic sobre un punto de la nube de puntos");

                    point = reference != null ? reference.GlobalPoint : null;
                    if (point != null)
                        Utils.Logger.Log($"PickPointCoordinates: PointOnElement GlobalPoint = {point}");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // Fallback below.
                }

                if (point == null)
                {
                    throw new InvalidOperationException(
                        "No se pudo capturar un punto 3D real de la nube. " +
                        "Activa snap a nube de puntos y selecciona un punto visible en 3D.");
                }

                // Obtener unidades del proyecto
                Units projectUnits = doc.GetUnits();
                ForgeTypeId lengthUnitId = projectUnits.GetFormatOptions(SpecTypeId.Length).GetUnitTypeId();
                string unitLabel = LabelUtils.GetLabelForUnit(lengthUnitId);

                // Coordenadas del modelo (internas de Revit convertidas a unidades del proyecto)
                double xModel = UnitUtils.ConvertFromInternalUnits(point.X, lengthUnitId);
                double yModel = UnitUtils.ConvertFromInternalUnits(point.Y, lengthUnitId);
                double zModel = UnitUtils.ConvertFromInternalUnits(point.Z, lengthUnitId);

                // Coordenadas del modelo en metros (para marcador y logs)
                double xModelM = UnitUtils.ConvertFromInternalUnits(point.X, UnitTypeId.Meters);
                double yModelM = UnitUtils.ConvertFromInternalUnits(point.Y, UnitTypeId.Meters);
                double zModelM = UnitUtils.ConvertFromInternalUnits(point.Z, UnitTypeId.Meters);

                // Coordenadas compartidas (Survey/UTM) desde la ubicación activa del proyecto.
                // Para nubes georreferenciadas, estas suelen ser las coordenadas esperadas por topografía.
                bool hasShared = false;
                double eShared = 0;
                double nShared = 0;
                double zShared = 0;
                double eSharedM = 0;
                double nSharedM = 0;
                double zSharedM = 0;

                try
                {
                    ProjectLocation location = doc.ActiveProjectLocation;
                    ProjectPosition shared = location != null ? location.GetProjectPosition(point) : null;

                    if (shared != null)
                    {
                        hasShared = true;
                        eShared = UnitUtils.ConvertFromInternalUnits(shared.EastWest, lengthUnitId);
                        nShared = UnitUtils.ConvertFromInternalUnits(shared.NorthSouth, lengthUnitId);
                        zShared = UnitUtils.ConvertFromInternalUnits(shared.Elevation, lengthUnitId);

                        eSharedM = UnitUtils.ConvertFromInternalUnits(shared.EastWest, UnitTypeId.Meters);
                        nSharedM = UnitUtils.ConvertFromInternalUnits(shared.NorthSouth, UnitTypeId.Meters);
                        zSharedM = UnitUtils.ConvertFromInternalUnits(shared.Elevation, UnitTypeId.Meters);
                    }
                }
                catch (Exception ex)
                {
                    Utils.Logger.Log($"PickPointCoordinates: no se pudo obtener coordenadas compartidas: {ex.Message}");
                }

                double deltaZM = hasShared ? (zSharedM - zModelM) : 0.0;

                Utils.Logger.Log(
                    $"PickPointCoordinates: modelo(m) X={xModelM:F4}, Y={yModelM:F4}, Z={zModelM:F4}");

                if (hasShared)
                {
                    Utils.Logger.Log(
                        $"PickPointCoordinates: compartidas(m) E={eSharedM:F4}, N={nSharedM:F4}, Z={zSharedM:F4}, dZ={deltaZM:F4}");
                }

                string mainContent =
                    $"MODELO (coordenadas internas de Revit)\n" +
                    $"  X:  {xModel:F4} {unitLabel}\n" +
                    $"  Y:  {yModel:F4} {unitLabel}\n" +
                    $"  Z:  {zModel:F4} {unitLabel}\n\n" +
                    $"  X:  {xModelM:F4} m\n" +
                    $"  Y:  {yModelM:F4} m\n" +
                    $"  Z:  {zModelM:F4} m";

                if (hasShared)
                {
                    mainContent +=
                        $"\n\n─────────────────────────────\n" +
                        $"COMPARTIDAS (Survey/UTM)\n" +
                        $"  E:  {eShared:F4} {unitLabel}\n" +
                        $"  N:  {nShared:F4} {unitLabel}\n" +
                        $"  Z:  {zShared:F4} {unitLabel}\n\n" +
                        $"  E:  {eSharedM:F4} m\n" +
                        $"  N:  {nSharedM:F4} m\n" +
                        $"  Z:  {zSharedM:F4} m\n\n" +
                        $"ΔZ compartida - modelo: {deltaZM:F4} m";
                }
                else
                {
                    mainContent +=
                        $"\n\n─────────────────────────────\n" +
                        $"No se pudieron calcular coordenadas compartidas.\n" +
                        $"Verifica Project Location / Shared Coordinates.";
                }

                string clipboardModel =
                    $"X={xModel:F4} Y={yModel:F4} Z={zModel:F4} ({unitLabel}) | " +
                    $"X={xModelM:F4} Y={yModelM:F4} Z={zModelM:F4} (m)";

                string clipboardShared =
                    $"E={eShared:F4} N={nShared:F4} Z={zShared:F4} ({unitLabel}) | " +
                    $"E={eSharedM:F4} N={nSharedM:F4} Z={zSharedM:F4} (m)";

                TaskDialog td = new TaskDialog("Scan2BIM — Coordenadas del punto");
                td.MainInstruction = "Punto seleccionado";
                td.MainContent = mainContent;
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1,
                    "Crear marcador en este punto",
                    "Coloca un Modelo Genérico (pinpoint) con coordenadas de modelo almacenadas");
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2,
                    "Copiar coordenadas de modelo", clipboardModel);

                if (hasShared)
                {
                    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3,
                        "Copiar coordenadas compartidas (Survey/UTM)", clipboardShared);
                    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink4, "Cerrar");
                    td.DefaultButton = TaskDialogResult.CommandLink4;
                }
                else
                {
                    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Cerrar");
                    td.DefaultButton = TaskDialogResult.CommandLink3;
                }

                TaskDialogResult result = td.Show();

                if (result == TaskDialogResult.CommandLink1)
                {
                    using (Transaction trans = new Transaction(doc, "Scan2BIM: Marcador Nube de Puntos"))
                    {
                        trans.Start();
                        DirectShape marker = Utils.PinpointHelper.CreatePinpoint(
                            doc,
                            point,
                            xModelM,
                            yModelM,
                            zModelM);

                        // Conservamos X/Y/Z de modelo para estabilidad geométrica,
                        // pero añadimos también E/N/Z compartidas para trazabilidad topográfica.
                        if (hasShared && marker != null)
                        {
                            Parameter commentsParam = marker.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                            if (commentsParam != null && !commentsParam.IsReadOnly)
                            {
                                string existing = commentsParam.AsString() ?? string.Empty;
                                string sharedLine =
                                    $"SC_E={eSharedM:F4}  SC_N={nSharedM:F4}  SC_Z={zSharedM:F4}  (m)";
                                commentsParam.Set(string.IsNullOrWhiteSpace(existing)
                                    ? sharedLine
                                    : existing + "\n" + sharedLine);
                            }
                        }

                        trans.Commit();
                    }
                    TaskDialog.Show("Scan2BIM", "Marcador creado exitosamente");
                }
                else if (result == TaskDialogResult.CommandLink2)
                {
                    System.Windows.Clipboard.SetText(clipboardModel);
                }
                else if (hasShared && result == TaskDialogResult.CommandLink3)
                {
                    System.Windows.Clipboard.SetText(clipboardShared);
                }

                Utils.Logger.Flush();
                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // El usuario canceló con Escape — no es un error
                Utils.Logger.Flush();
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                Utils.Logger.LogError(ex);
                Utils.Logger.Flush();
                message = ex.Message;
                return Result.Failed;
            }
        }

        private sealed class PointCloudSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                if (elem == null || elem.Category == null)
                    return false;

                return elem.Category.Id.Value == (long)BuiltInCategory.OST_PointClouds;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return true;
            }
        }
    }
}
