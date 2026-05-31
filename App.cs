using System;
using Autodesk.Revit.UI;
using System.Reflection;

namespace Metrika
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                application.CreateRibbonTab("Scan2BIM");

                // ── Panel 1: Nube de Puntos ──────────────────────────────────
                RibbonPanel panelNube = application.CreateRibbonPanel("Scan2BIM", "Nube de Puntos");

                PushButtonData btnHide = new PushButtonData(
                    "HideUnhidePointCloud", "Ocultar /\nMostrar", assemblyPath,
                    "Metrika.Commands.HideUnhidePointCloudCommand")
                {
                    ToolTip = "Alterna la visibilidad de todas las nubes de puntos en la vista activa"
                };

                PushButtonData btnMedirNube = new PushButtonData(
                    "PickPointCoordinates", "Medir Punto\nen Nube", assemblyPath,
                    "Metrika.Commands.PickPointCoordinatesCommand")
                {
                    ToolTip         = "Haz clic en la nube de puntos y obtén X, Y, Z",
                    LongDescription = "Funciona en vistas 2D y 3D. Usa los snaps nativos de Revit. Requiere snap a nube de puntos activado."
                };

                PushButton btnHideItem = panelNube.AddItem(btnHide) as PushButton;
                if (btnHideItem != null)
                {
                    btnHideItem.LargeImage = Utils.IconHelper.HideUnhide(32);
                    btnHideItem.Image      = Utils.IconHelper.HideUnhide(16);
                }

                PushButton btnMedirNubeItem = panelNube.AddItem(btnMedirNube) as PushButton;
                if (btnMedirNubeItem != null)
                {
                    btnMedirNubeItem.LargeImage = Utils.IconHelper.MeasureCloud(32);
                    btnMedirNubeItem.Image      = Utils.IconHelper.MeasureCloud(16);
                }

                // ── Panel 2: Medición 3D ─────────────────────────────────────
                RibbonPanel panelMedicion = application.CreateRibbonPanel("Scan2BIM", "Medición 3D");

                PushButtonData btnElem = new PushButtonData(
                    "MeasureElementFace", "Medir Punto\nen Elemento", assemblyPath,
                    "Metrika.Commands.MeasureElementFaceCommand")
                {
                    ToolTip         = "Haz clic sobre cualquier cara de suelo, muro, techo, escalera… y obtén X, Y, Z",
                    LongDescription = "Funciona en vistas 2D y 3D. Soporta: Suelos, Muros, Techos, Escaleras, Barandillas, Columnas, Forjados estructurales, Rampas, Toposolid y Modelos Genéricos."
                };

                PushButton btnElemItem = panelMedicion.AddItem(btnElem) as PushButton;
                if (btnElemItem != null)
                {
                    btnElemItem.LargeImage = Utils.IconHelper.MeasureElement(32);
                    btnElemItem.Image      = Utils.IconHelper.MeasureElement(16);
                }

                // ── Panel 3: Marcadores ─────────────────────────────────────
                RibbonPanel panelMarcadores = application.CreateRibbonPanel("Scan2BIM", "Marcadores");

                PushButtonData btnExportMarkers = new PushButtonData(
                    "ExportMarkers", "Exportar\nCSV/JSON", assemblyPath,
                    "Metrika.Commands.ExportMarkersCommand")
                {
                    ToolTip = "Exporta marcadores Pinpoint a archivo CSV o JSON"
                };

                PushButtonData btnImportMarkers = new PushButtonData(
                    "ImportMarkers", "Importar\nCSV/JSON", assemblyPath,
                    "Metrika.Commands.ImportMarkersCommand")
                {
                    ToolTip = "Importa marcadores desde CSV o JSON y los crea en el modelo"
                };

                PushButton btnExportItem = panelMarcadores.AddItem(btnExportMarkers) as PushButton;
                if (btnExportItem != null)
                {
                    btnExportItem.LargeImage = Utils.IconHelper.ExportMarkers(32);
                    btnExportItem.Image      = Utils.IconHelper.ExportMarkers(16);
                }

                PushButton btnImportItem = panelMarcadores.AddItem(btnImportMarkers) as PushButton;
                if (btnImportItem != null)
                {
                    btnImportItem.LargeImage = Utils.IconHelper.ImportMarkers(32);
                    btnImportItem.Image      = Utils.IconHelper.ImportMarkers(16);
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Scan2BIM - Error", "Error al inicializar plugin:\n" + ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}

