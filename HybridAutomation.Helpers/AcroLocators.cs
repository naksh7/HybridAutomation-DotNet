using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text.Json;
using iText.Forms;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using OfficeOpenXml;
using PdfRectangle = iText.Kernel.Geom.Rectangle;

namespace HybridAutomation.Helpers
{
    /// <summary>Pixel coordinates of an AcroField mapped onto the page image (origin: top-left).</summary>
    public sealed record FieldBox(
        string PageName,
        string FieldName,
        int PageNumber,
        int X,
        int Y,
        int Width,
        int Height)
    {
        public Rectangle Bounds => new(X, Y, Width, Height);

        public Point Center => new(X + Width / 2, Y + Height / 2);

        public override string ToString() =>
            $"{PageName}/{FieldName} (p{PageNumber}) => x:{X} y:{Y} w:{Width} h:{Height} center:{Center.X},{Center.Y}";
    }

    /// <summary>
    /// Maps AcroForm field rectangles from a PDF onto the pixel space of a rendered page image.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class AcroLocators
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private sealed record FieldLocator(int X, int Y);

        /// <summary>
        /// Resolves the pixel coordinates of the given AcroFields on <paramref name="imagePath"/>.
        /// </summary>
        /// <param name="pageName">Label of the page, used only to tag the results.</param>
        /// <param name="fieldNames">Fully qualified AcroField names. Pass none to return every field.</param>
        public IReadOnlyList<FieldBox> GetFieldCoordinates(string pdfPath, string imagePath, string pageName, params string[] fieldNames) =>
            GetFieldCoordinates(pdfPath, imagePath, pageName, null, fieldNames);

        /// <summary>
        /// Resolves the pixel coordinates of the given AcroFields on <paramref name="imagePath"/>,
        /// keeping only widgets on <paramref name="pageNumber"/> (1-based, null for every page).
        /// </summary>
        public IReadOnlyList<FieldBox> GetFieldCoordinates(string pdfPath, string imagePath, string pageName, int? pageNumber, params string[] fieldNames) =>
            GetAllFieldCoordinates(pdfPath, imagePath, pageName, pageNumber, Rectangle.Empty, fieldNames);

        /// <summary>
        /// As above, but the page is rendered into <paramref name="pageArea"/> of the image rather than
        /// filling it — use this when the image is a screenshot of a viewer. Pass
        /// <see cref="Rectangle.Empty"/> to use the whole image.
        /// </summary>
        public IReadOnlyList<FieldBox> GetAllFieldCoordinates(string pdfPath, string imagePath, string pageName, int? pageNumber, Rectangle pageArea, params string[] fieldNames)
        {            
            Utilities.Logger.Log(Logger.LogType.Info, $"Extracting AcroField coordinates for page '{pageName}' from '{Path.GetFileName(pdfPath)}'.", false);
            if (!Utilities.Files.VerifyFilePresent(pdfPath)) throw new FileNotFoundException("PDF not found.", pdfPath);
            if (!Utilities.Files.VerifyFilePresent(imagePath)) throw new FileNotFoundException("Image not found.", imagePath);

            Size imageSize;
            using (var image = Image.FromFile(imagePath))
            {
                imageSize = image.Size;
            }

            var target = pageArea.IsEmpty ? new Rectangle(Point.Empty, imageSize) : pageArea;

            using var reader = new PdfReader(pdfPath);
            using var pdf = new PdfDocument(reader);

            var form = PdfAcroForm.GetAcroForm(pdf, false)
                       ?? throw new InvalidOperationException($"'{pdfPath}' contains no AcroForm.");

            var fields = form.GetAllFormFields();
            var pageOfAnnotation = BuildAnnotationPageIndex(pdf);
            var wanted = fieldNames.Length == 0 ? fields.Keys.ToArray() : fieldNames;

            var results = new List<FieldBox>();

            foreach (var name in wanted)
            {
                if (!fields.TryGetValue(name, out var field))
                    throw new KeyNotFoundException($"AcroField '{name}' does not exist in '{Path.GetFileName(pdfPath)}'.");

                foreach (var widget in field.GetWidgets())
                {
                    var reference = widget.GetPdfObject().GetIndirectReference();
                    if (reference is null || !pageOfAnnotation.TryGetValue(reference, out var widgetPage))
                        continue;

                    if (pageNumber is not null && widgetPage != pageNumber) continue;

                    var pageSize = pdf.GetPage(widgetPage).GetPageSize();
                    results.Add(ToPixels(pageName, name, widgetPage, widget.GetRectangle().ToRectangle(), pageSize, target));
                }
            }

            Utilities.Logger.Log(Logger.LogType.Pass, $"Extracted {results.Count} AcroField coordinate(s) for page '{pageName}'.", false);
            return results;
        }

        /// <summary>
        /// Maps every AcroField of each named page onto its image and writes the click points to a
        /// separate JSON file named after each page.
        /// </summary>
        /// <param name="imageFolder">Folder holding one image per page, named after the page.</param>
        /// <param name="pages">Page label to 1-based PDF page number.</param>
        /// <returns>The paths of the written JSON files.</returns>
        public IReadOnlyList<string> GetAcroFieldLocatorsInJson(string pdfPath, string imageFolderPath, Dictionary<string, int> pages, Rectangle pageArea, string outputPath)
        {
            Utilities.Logger.Log(Logger.LogType.Info, $"Generating AcroField locator JSON for {pages.Count} page(s) in '{outputPath}'.", false);
            if (!Utilities.Files.VerifyFolderPresent(imageFolderPath)) throw new DirectoryNotFoundException(imageFolderPath);
            Utilities.Files.EnsureDirectoryExist(outputPath);

            var outputFiles = new List<string>();

            foreach (var (pageName, pageNumber) in pages)
            {
                Utilities.Logger.Log(Logger.LogType.Info, $"Processing page '{pageName}' (PDF page {pageNumber}).", false);
                var imagePath = ResolveImage(imageFolderPath, pageName);
                var locators = new Dictionary<string, FieldLocator>();

                foreach (var box in GetAllFieldCoordinates(pdfPath, imagePath, pageName, pageNumber, pageArea))
                    locators[box.FieldName] = new FieldLocator(box.Center.X, box.Center.Y);

                var outputFile = Path.Combine(outputPath, $"{pageName}.json");
                Utilities.Files.WriteTextToFile(outputFile, JsonSerializer.Serialize(locators, JsonOptions));
                outputFiles.Add(outputFile);
                Utilities.Logger.Log(Logger.LogType.Pass, $"Wrote {locators.Count} locator(s) to '{outputFile}'.", false);
            }

            Utilities.Logger.Log(Logger.LogType.Pass, $"Generated {outputFiles.Count} AcroField locator JSON file(s).", false);
            return outputFiles;
        }

        /// <summary>
        /// Generates the per-page locator JSON files, then consolidates them into a single workbook
        /// named after <paramref name="formName"/>, one worksheet per page.
        /// </summary>
        /// <returns>The path of the written Excel file.</returns>
        public IReadOnlyList<string> GetAcroFieldLocatorsInExcel(string formName, string pdfPath, string imageFolderPath, Dictionary<string, int> pages, Rectangle pageArea, string outputPath, string taxYear)
        {
            Utilities.Logger.Log(Logger.LogType.Info, $"Generating AcroField locator Excel for {pages.Count} page(s) in '{outputPath}'.", false);

            var jsonFiles = GetAcroFieldLocatorsInJson(pdfPath, imageFolderPath, pages, pageArea, outputPath);

            var outputFile = Path.Combine(outputPath, $"{formName}_Coordinates_{taxYear}.xlsx");
            if (File.Exists(outputFile))
                File.Delete(outputFile);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var pageCount = 1;

            foreach (var jsonFile in jsonFiles)
            {
                var locators = JsonSerializer.Deserialize<Dictionary<string, FieldLocator>>(File.ReadAllText(jsonFile), JsonOptions)
                    ?? throw new InvalidOperationException($"No locators found in '{jsonFile}'.");
                locators = RemoveShallowerDuplicateFieldNames(locators);

                var worksheet = package.Workbook.Worksheets.Add($"{formName}_{pageCount}");
                worksheet.Cells[1, 1].Value = "SourceNode";
                worksheet.Cells[1, 2].Value = "Field";
                worksheet.Cells[1, 3].Value = "X";
                worksheet.Cells[1, 4].Value = "Y";
                worksheet.Cells[1, 5].Value = "XOffsetAdjustment";
                worksheet.Cells[1, 6].Value = "YOffsetAdjustment";

                var row = 2;
                foreach (var (fieldName, locator) in locators.OrderBy(entry => entry.Value.Y))
                {
                    worksheet.Cells[row, 2].Value = fieldName;
                    worksheet.Cells[row, 3].Value = locator.X;
                    worksheet.Cells[row, 4].Value = locator.Y;
                    worksheet.Cells[row, 5].Value = 0;
                    worksheet.Cells[row, 6].Value = 2;
                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                Utilities.Logger.Log(Logger.LogType.Pass, $"Added {row - 2} locator(s) to worksheet '{worksheet.Name}' from '{jsonFile}'.", false);
                pageCount++;
            }

            package.SaveAs(new FileInfo(outputFile));
            Utilities.Logger.Log(Logger.LogType.Pass, $"Generated AcroField locator Excel file '{outputFile}'.", false);
            return new List<string> { outputFile };
        }

        /// <summary>
        /// When two field names share the same X/Y, keeps only the one(s) with the most dot-separated
        /// segments (e.g. keeps "Contact.UTR" and drops "Contact").
        /// </summary>
        private static Dictionary<string, FieldLocator> RemoveShallowerDuplicateFieldNames(Dictionary<string, FieldLocator> locators)
        {
            var deduplicated = new Dictionary<string, FieldLocator>();
            foreach (var group in locators
                .Where(entry => !entry.Key.Contains("Footer", StringComparison.OrdinalIgnoreCase))
                .GroupBy(entry => (entry.Value.X, entry.Value.Y)))
            {
                var maxDepth = group.Max(entry => entry.Key.Split('.').Length);
                foreach (var entry in group.Where(entry => entry.Key.Split('.').Length == maxDepth))
                    deduplicated[entry.Key] = entry.Value;
            }
            return deduplicated;
        }

        private string ResolveImage(string imageFolder, string pageName)
        {
            if (Path.HasExtension(pageName))
            {
                var exact = Path.Combine(imageFolder, pageName);
                if (Utilities.Files.VerifyFilePresent(exact)) return exact;
            }

            var match = Utilities.Files.GetFiles(imageFolder, pageName + ".*", SearchOption.TopDirectoryOnly).FirstOrDefault();
            return match ?? throw new FileNotFoundException($"No image for page '{pageName}'.", Path.Combine(imageFolder, pageName));
        }

        /// <summary>Draws the boxes onto a copy of the image so the mapping can be eyeballed.</summary>
        public string SaveOverlay(string imagePath, IEnumerable<FieldBox> boxes, string? outputPath = null)
        {
            Utilities.Logger.Log(Logger.LogType.Info, $"Creating AcroField overlay for '{imagePath}'.", false);
            outputPath ??= Path.Combine(
                Path.GetDirectoryName(imagePath)!,
                Path.GetFileNameWithoutExtension(imagePath) + "_overlay.png");

            using var source = Image.FromFile(imagePath);
            using var canvas = new Bitmap(source);
            using var graphics = Graphics.FromImage(canvas);
            using var pen = new Pen(Color.Red, 2f);
            using var brush = new SolidBrush(Color.Red);
            using var font = new Font("Segoe UI", 8f);

            foreach (var box in boxes)
            {
                graphics.DrawRectangle(pen, box.Bounds);
                graphics.DrawString(box.FieldName, font, brush, box.X, Math.Max(0, box.Y - 12));
            }

            canvas.Save(outputPath, ImageFormat.Png);
            Utilities.Logger.Log(Logger.LogType.Pass, $"Saved AcroField overlay to '{outputPath}'.", false);
            return outputPath;
        }

        private FieldBox ToPixels(
            string pageName,
            string fieldName,
            int pageNumber,
            PdfRectangle rect,
            PdfRectangle pageSize,
            Rectangle target)
        {
            var scaleX = target.Width / pageSize.GetWidth();
            var scaleY = target.Height / pageSize.GetHeight();

            // PDF origin is bottom-left and the page box may be offset, so flip Y against the page top.
            var x = target.X + (rect.GetLeft() - pageSize.GetLeft()) * scaleX;
            var y = target.Y + (pageSize.GetTop() - rect.GetTop()) * scaleY;

            return new FieldBox(
                pageName,
                fieldName,
                pageNumber,
                (int)Math.Round(x),
                (int)Math.Round(y),
                (int)Math.Round(rect.GetWidth() * scaleX),
                (int)Math.Round(rect.GetHeight() * scaleY));
        }

        private Dictionary<PdfIndirectReference, int> BuildAnnotationPageIndex(PdfDocument pdf)
        {
            var index = new Dictionary<PdfIndirectReference, int>();

            for (var pageNumber = 1; pageNumber <= pdf.GetNumberOfPages(); pageNumber++)
            {
                foreach (var annotation in pdf.GetPage(pageNumber).GetAnnotations())
                {
                    if (annotation is not PdfWidgetAnnotation) continue;

                    var reference = annotation.GetPdfObject().GetIndirectReference();
                    if (reference is not null) index[reference] = pageNumber;
                }
            }

            return index;
        }
    }
}
