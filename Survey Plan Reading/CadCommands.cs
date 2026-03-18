using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.LocalV3;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;

namespace Survey_Plan_Reading
{
    public class CadCommands
    {
        [CommandMethod("ExtractSurveyLine")]
        public void ExtractSurveyLine()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            var ppr1 = ed.GetPoint("\nSelect first corner: ");
            if (ppr1.Status != PromptStatus.OK) return;

            var ppr2 = ed.GetCorner("\nSelect opposite corner: ", ppr1.Value);
            if (ppr2.Status != PromptStatus.OK) return;

            _ = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.ExecuteInCommandContextAsync(
                async (obj) => await ProcessExtractionAsync(doc, ppr1.Value, ppr2.Value),
                null
            );
        }

        private async Task ProcessExtractionAsync(Document doc, Autodesk.AutoCAD.Geometry.Point3d p1, Autodesk.AutoCAD.Geometry.Point3d p2)
        {
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                System.Windows.Point screenPt1 = ed.PointToScreen(p1, 1);
                System.Windows.Point screenPt2 = ed.PointToScreen(p2, 1);

                Autodesk.AutoCAD.Geometry.Point3d cap1 = new Autodesk.AutoCAD.Geometry.Point3d(screenPt1.X, screenPt1.Y, 0);
                Autodesk.AutoCAD.Geometry.Point3d cap2 = new Autodesk.AutoCAD.Geometry.Point3d(screenPt2.X, screenPt2.Y, 0);

                using (System.Drawing.Bitmap rawBmp = ScreenCaptureUtility.CaptureArea(cap1, cap2))
                using (System.Drawing.Bitmap processedBmp = ScreenCaptureUtility.PreProcessImage(rawBmp))
                {
                    string recognizedText = PerformPaddleOcr(processedBmp);
                    
                    if (string.IsNullOrWhiteSpace(recognizedText))
                    {
                        ed.WriteMessage("\nOCR Error: No text found.");
                        return;
                    }
                    
                    ed.WriteMessage($"\n--- PADDLE OCR OUTPUT ---\n{recognizedText}\n-------------------------");

                    var distances = OcrUtility.ParseDistances(recognizedText);
                    var bearings = OcrUtility.ParseDmsBearings(recognizedText);

                    if (!distances.Any() || !bearings.Any())
                    {
                        ed.WriteMessage("\nFailed to extract valid distance and bearing.");
                        return;
                    }

                    double distance = distances.First();
                    var (deg, min, sec) = bearings.First();
                    double bearingRad = SurveyGeometry.DmsToRadians(deg, min, sec);

                    var pprStart = ed.GetPoint("\nSelect start point: ");
                    if (pprStart.Status != PromptStatus.OK) return;

                    Autodesk.AutoCAD.Geometry.Point3d start = pprStart.Value;
                    Autodesk.AutoCAD.Geometry.Point3d end = SurveyGeometry.GetEndPoint(start, distance, bearingRad);

                    using (doc.LockDocument())
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        Line acLine = new Line(start, end);
                        acLine.SetDatabaseDefaults();
                        btr.AppendEntity(acLine);
                        tr.AddNewlyCreatedDBObject(acLine, true);

                        tr.Commit();
                        ed.WriteMessage($"\nSuccess: Line drawn (Dist: {distance}, Bearing: {deg}°{min}'{sec}\")");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError during extraction: {ex.Message}");
            }
        }

        private string PerformPaddleOcr(System.Drawing.Bitmap bmp)
        {
            // PaddleOCR initialization - Uses LocalFullModels.EnglishV3
            using (Mat mat = bmp.ToMat())
            using (PaddleOcrAll all = new PaddleOcrAll(LocalFullModels.EnglishV3)
            {
                AllowRotateDetection = true,
            })
            {
                PaddleOcrResult result = all.Run(mat);
                return result.Text;
            }
        }
    }
}
