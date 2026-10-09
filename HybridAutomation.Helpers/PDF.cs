using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text;

namespace HybridAutomation.Helpers
{
    public class PDF
    {
        /// <summary>
        /// Extracts text from a PDF file
        /// </summary>
        /// <param name="pdfPath">Path to the PDF file</param>
        /// <returns>Extracted text content from all pages</returns>
        public string GetTextFromPDF(string pdfPath)
        {
            try
            {
                if (!Utilities.Files.VerifyFilePresent(pdfPath))
                {
                    throw new FileNotFoundException($"PDF file not found: {pdfPath}");
                }

                StringBuilder extractedText = new StringBuilder();

                using (PdfReader pdfReader = new PdfReader(pdfPath))
                using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
                {
                    int numberOfPages = pdfDocument.GetNumberOfPages();

                    for (int page = 1; page <= numberOfPages; page++)
                    {
                        ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                        string pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(page), strategy);
                        extractedText.AppendLine(pageText);
                    }
                }

                string result = extractedText.ToString();
                Utilities.Logger.Log(Logger.LogType.Info, $"Text extraction completed successfully from PDF: {pdfPath}. Characters extracted: {result.Length}");
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetTextFromPDF failed for pdfPath: {pdfPath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Extracts text from a specific page of a PDF file
        /// </summary>
        /// <param name="pdfPath">Path to the PDF file</param>
        /// <param name="pageNumber">Page number to extract text from (1-based)</param>
        /// <returns>Extracted text content from the specified page</returns>
        public string GetTextFromPDFPage(string pdfPath, int pageNumber)
        {
            try
            {
                if (!Utilities.Files.VerifyFilePresent(pdfPath))
                {
                    throw new FileNotFoundException($"PDF file not found: {pdfPath}");
                }

                using (PdfReader pdfReader = new PdfReader(pdfPath))
                using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
                {
                    int numberOfPages = pdfDocument.GetNumberOfPages();

                    if (pageNumber < 1 || pageNumber > numberOfPages)
                    {
                        throw new ArgumentOutOfRangeException(nameof(pageNumber),
                            $"Page number {pageNumber} is out of range. PDF has {numberOfPages} pages.");
                    }

                    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                    string pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(pageNumber), strategy);

                    Utilities.Logger.Log(Logger.LogType.Info, $"Text extraction completed successfully from PDF: {pdfPath} | Page: {pageNumber} | Characters extracted: {pageText.Length}");
                    return pageText;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nGetTextFromPDFPage failed for pdfPath: {pdfPath} and pageNumber: {pageNumber}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Compares two PDF files by extracting and comparing their text content
        /// </summary>
        /// <param name="expectedPdfPath">Path to the expected PDF file</param>
        /// <param name="actualPdfPath">Path to the actual PDF file</param>
        /// <param name="ignoreWhitespace">Whether to ignore whitespace differences in comparison</param>
        /// <returns>True if PDFs have identical text content, false otherwise</returns>
        public bool ComparePDFByText(string expectedPdfPath, string actualPdfPath, bool ignoreWhitespace = true)
        {
            try
            {
                if (!Utilities.Files.VerifyFilePresent(expectedPdfPath))
                {
                    throw new FileNotFoundException($"Expected PDF file not found: {expectedPdfPath}");
                }

                if (!Utilities.Files.VerifyFilePresent(actualPdfPath))
                {
                    throw new FileNotFoundException($"Actual PDF file not found: {actualPdfPath}");
                }

                string expectedText = GetTextFromPDF(expectedPdfPath);
                string actualText = GetTextFromPDF(actualPdfPath);

                if (ignoreWhitespace)
                {
                    expectedText = NormalizeWhitespace(expectedText);
                    actualText = NormalizeWhitespace(actualText);
                }

                bool areEqual = string.Equals(expectedText, actualText, StringComparison.Ordinal);

                Utilities.Logger.Log(Logger.LogType.Warning, $"PDF comparison completed\nExpected: {expectedPdfPath}\nActual: {actualPdfPath}\nResult: {(areEqual ? "MATCH" : "NO MATCH")}", false);

                if (!areEqual)
                {
                    Utilities.Logger.Log(Logger.LogType.Warning, $"PDF content mismatch detected.\nExpected length: {expectedText.Length}\nActual length: {actualText.Length}", false);
                }

                return areEqual;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nComparePDFByText failed for ExpectedPdfPath: {expectedPdfPath}\nActualPdfPath: {actualPdfPath}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Compares specific pages of two PDF files
        /// </summary>
        /// <param name="expectedPdfPath">Path to the expected PDF file</param>
        /// <param name="actualPdfPath">Path to the actual PDF file</param>
        /// <param name="pageNumber">Page number to compare (1-based)</param>
        /// <param name="ignoreWhitespace">Whether to ignore whitespace differences in comparison</param>
        /// <returns>True if the specified pages have identical text content, false otherwise</returns>
        public bool ComparePDFPagesByText(string expectedPdfPath, string actualPdfPath, int pageNumber, bool ignoreWhitespace = true)
        {
            try
            {
                string expectedText = GetTextFromPDFPage(expectedPdfPath, pageNumber);
                string actualText = GetTextFromPDFPage(actualPdfPath, pageNumber);

                if (ignoreWhitespace)
                {
                    expectedText = NormalizeWhitespace(expectedText);
                    actualText = NormalizeWhitespace(actualText);
                }

                bool areEqual = string.Equals(expectedText, actualText, StringComparison.Ordinal);

                Utilities.Logger.Log(Logger.LogType.Warning, $"PDF page comparison completed.\nExpected: {expectedPdfPath}\nActual: {actualPdfPath}\nPage: {pageNumber}\nResult: {(areEqual ? "MATCH" : "NO MATCH")}", false);

                return areEqual;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nComparePDFPagesByText failed for ExpectedPdfPath: {expectedPdfPath}\nActualPdfPath: {actualPdfPath}\nPgeNumber: {pageNumber}\n{ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// Normalizes whitespace in text for comparison purposes
        /// </summary>
        /// <param name="text">Text to normalize</param>
        /// <returns>Normalized text with standardized whitespace</returns>
        private string NormalizeWhitespace(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Replace multiple whitespace characters with single space
            return System.Text.RegularExpressions.Regex.Replace(text.Trim(), @"\s+", " ");
        }

        /// <summary>
        /// Compares two PDF files using the external diff-pdf tool
        /// </summary>
        /// <param name="expectedPdfPath">Path to the expected PDF file</param>
        /// <param name="actualPdfPath">Path to the actual PDF file</param>
        /// <param name="outputPath">Optional path to save the diff output image</param>
        /// <returns>True if PDFs are identical, false otherwise</returns>
        public bool ComparePDFByImposing(string expectedPdfPath, string actualPdfPath, string outputPath, string fileType)
        {
            try
            {
                string differencePdfPath = Path.Combine(outputPath, fileType + "_Comparison.pdf");
                string differencePdfView = Path.Combine(outputPath, fileType + "_Comparison_View.bat");
                string arguments = $"/c diff-pdf --output-diff=\"{differencePdfPath}\" \"{expectedPdfPath}\" \"{actualPdfPath}\"";
                string argumentsBatContent = $"diff-pdf --view \"%~dp0{Path.GetFileName(expectedPdfPath)}\" \"%~dp0{Path.GetFileName(actualPdfPath)}\"";

                if (!Utilities.Files.VerifyFilePresent(expectedPdfPath))
                {
                    throw new FileNotFoundException($"Expected PDF file not found: {expectedPdfPath}");
                }

                if (!Utilities.Files.VerifyFilePresent(actualPdfPath))
                {
                    throw new FileNotFoundException($"Actual PDF file not found: {actualPdfPath}");
                }

                //ExecuteProcess(string fileName, string arguments = null, bool runAsAdmin = true, bool redirectOutput = true)
                var processResult = Utilities.ProcessHandler.ExecuteProcess("cmd.exe", arguments);

                // diff-pdf returns 0 if files are identical, 1 if different, >1 for errors
                bool areIdentical = processResult.ExitCode == 0;

                if (processResult.ErrorMessage.Contains("is not recognized as an internal or external command") || processResult.ExitCode > 1)
                {
                    Utilities.Logger.Log(Logger.LogType.Skip, $"Skipped PDF comparison as diff-pdf failed with Error Code : {processResult.ExitCode}\nError Message : {processResult.ErrorMessage}\nPlease follow the instructions present on the readme.md to set diff-pdf to System Path or Run EnvirontmentSetup.");
                    Utilities.Logger.Log(Logger.LogType.Warning, $"PDF comparison failed using diff-pdf. PDF's can be checked manually.\nExpected: {expectedPdfPath}\nActual: {actualPdfPath}");
                    return false;
                }

                string resultMessage = areIdentical ? "MATCH" : "NO MATCH";
                if (areIdentical)
                {
                    Utilities.Logger.Log(Logger.LogType.Pass, $"PDF comparison using diff-pdf completed.\nExpected: {expectedPdfPath}\nActual: {actualPdfPath}\nResult: {resultMessage}", false);
                }
                else
                {
                    Utilities.Files.CreateFile(differencePdfView, argumentsBatContent);
                    Utilities.Logger.Log(Logger.LogType.Fail, $"PDF comparison using diff-pdf completed.\nExpected: {expectedPdfPath}\nActual: {actualPdfPath}\nResult: {resultMessage}" +
                        $"\nDifference view can be seen from: {differencePdfView}\nDifference pdf saved to: {differencePdfPath}", false);
                }
                return areIdentical;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}\nComparePDFByImposing failed for ExpectedPdfPath: {expectedPdfPath}\nActualPdfPath: {actualPdfPath}\nOutputPath: {outputPath}\nFileType: {fileType}\n{ex.StackTrace}", ex);
            }
        }
    }
}