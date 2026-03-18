using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace Survey_Plan_Reading
{
    public class CadCommands
    {
        [CommandMethod("ExtractSurveyLine")]
        public async void ExtractSurveyLine()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Autodesk.AutoCAD.EditorInput.Editor ed = doc.Editor;

            // 1. User selects the area containing the text (bearing/distance)
            var ppr1 = ed.GetPoint("\nSelect first corner of the text area: ");
            if (ppr1.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;

            var ppr2 = ed.GetCorner("\nSelect opposite corner: ", ppr1.Value);
            if (ppr2.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;

            try
            {
                // 2. Convert CAD points to screen coordinates for capture
                var screenPt1 = ed.PointToScreen(ppr1.Value, 1);
                var screenPt2 = ed.PointToScreen(ppr2.Value, 1);

                Point3d capturePt1 = new Point3d(screenPt1.X, screenPt1.Y, 0);
                Point3d capturePt2 = new Point3d(screenPt2.X, screenPt2.Y, 0);

                // 3. Capture the screen area
                using (System.Drawing.Bitmap bmp = ScreenCaptureUtility.CaptureArea(capturePt1, capturePt2))
                {
                    // 4. Perform Real OCR
                    string recognizedText = await PerformOcr(bmp);
                    ed.WriteMessage($"\nRecognized Text: {recognizedText}");

                    // 5. Parse the recognized text
                    var distances = OcrUtility.ParseDistances(recognizedText);
                    var bearings = OcrUtility.ParseBearings(recognizedText);

                    if (distances.Count == 0 || bearings.Count == 0)
                    {
                        ed.WriteMessage("\nCould not recognize both a distance and a bearing.");
                        return;
                    }

                    double distance = distances[0];
                    double bearingDeg = bearings[0];
                    double bearingRad = SurveyGeometry.DmsToRadians(bearingDeg, 0, 0);

                    // 6. Prompt for the line's start point
                    var pprStart = ed.GetPoint("\nSelect start point for the new line: ");
                    if (pprStart.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;

                    Point3d startPoint = pprStart.Value;
                    Point3d endPoint = SurveyGeometry.GetEndPoint(startPoint, distance, bearingRad);

                    // 7. Draw the line in a transaction
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        Line acLine = new Line(startPoint, endPoint);
                        acLine.SetDatabaseDefaults();

                        btr.AppendEntity(acLine);
                        tr.AddNewlyCreatedDBObject(acLine, true);

                        tr.Commit();
                        ed.WriteMessage($"\nLine drawn: Distance {distance}, Bearing {bearingDeg}°");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");
            }
        }

        private async Task<string> PerformOcr(System.Drawing.Bitmap bmp)
        {
            // Convert System.Drawing.Bitmap to SoftwareBitmap (for Windows OCR)
            using (MemoryStream stream = new MemoryStream())
            {
                bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                var decoder = await BitmapDecoder.CreateAsync(BitmapDecoder.BmpDecoderId, stream.AsRandomAccessStream());
                var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                // Initialize OCR Engine (Default English language)
                OcrEngine ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
                if (ocrEngine == null) return "OCR Engine not available.";

                OcrResult result = await ocrEngine.RecognizeAsync(softwareBitmap);
                return result.Text;
            }
        }
    }
}
