using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls;
using UglyToad.PdfPig;
using OfficeOpenXml;
using Xceed.Words.NET;
using System.Windows.Xps.Packaging;
using System.Windows.Documents.Serialization;

namespace GeotekMetallCompleteDesktop
{
    public partial class viewingDocument : Window
    {
        private string _filePath;

        public viewingDocument(string filePath, string documentTitle)
        {
            InitializeComponent();
            Title = $"Просмотр документа: {documentTitle}";
            _filePath = filePath;
            LoadDocument();
        }

        private void LoadDocument()
        {
            try
            {
                var extension = Path.GetExtension(_filePath).ToLower();
                var flowDocument = new FlowDocument();

                switch (extension)
                {
                    case ".txt":
                    case ".log":
                    case ".csv":
                        LoadTextFile(flowDocument);
                        break;

                    case ".rtf":
                        LoadRtfFile(flowDocument);
                        break;

                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".bmp":
                    case ".gif":
                        LoadImageFile(flowDocument);
                        break;

                    case ".pdf":
                        LoadPdfFile(flowDocument);
                        break;

                    case ".doc":
                    case ".docx":
                        LoadWordFile(flowDocument);
                        break;

                    case ".xls":
                    case ".xlsx":
                        LoadExcelFile(flowDocument);
                        break;

                    default:
                        TryLoadAsText(flowDocument);
                        break;
                }

                flowDocumentReader.Document = flowDocument;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии документа: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void LoadTextFile(FlowDocument flowDocument)
        {
            flowDocument.Blocks.Add(new Paragraph(new Run(File.ReadAllText(_filePath))));
        }

        private void LoadRtfFile(FlowDocument flowDocument)
        {
            using (var stream = new FileStream(_filePath, FileMode.Open))
            {
                var range = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
                range.Load(stream, DataFormats.Rtf);
            }
        }

        private void LoadImageFile(FlowDocument flowDocument)
        {
            var image = new System.Windows.Controls.Image(); 
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(_filePath);
            bitmap.EndInit();
            image.Source = bitmap;
            image.Stretch = Stretch.Uniform;

            var imageContainer = new BlockUIContainer(image);
            flowDocument.Blocks.Add(imageContainer);
        }

        private void LoadPdfFile(FlowDocument flowDocument)
        {
            try
            {
                using (var pdfDocument = UglyToad.PdfPig.PdfDocument.Open(_filePath))
                {
                    foreach (var page in pdfDocument.GetPages())
                    {
                        var paragraph = new Paragraph();
                        paragraph.Inlines.Add(new Run($"Страница {page.Number}:"));
                        paragraph.Inlines.Add(new LineBreak());
                        paragraph.Inlines.Add(new Run(page.Text));
                        flowDocument.Blocks.Add(paragraph);
                    }
                }
            }
            catch (Exception ex)
            {
                flowDocument.Blocks.Add(new Paragraph(new Run($"Ошибка чтения PDF: {ex.Message}")));
            }
        }

        private void LoadWordFile(FlowDocument flowDocument)
        {
            try
            {
                using (var doc = DocX.Load(_filePath)) 
                {
                    foreach (var paragraph in doc.Paragraphs)
                    {
                        flowDocument.Blocks.Add(new Paragraph(new Run(paragraph.Text)));
                    }
                }
            }
            catch (Exception ex)
            {
                flowDocument.Blocks.Add(new Paragraph(new Run($"Ошибка чтения Word документа: {ex.Message}")));
            }
        }

        private void LoadExcelFile(FlowDocument flowDocument)
        {
            try
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial; 
                using (var package = new ExcelPackage(new FileInfo(_filePath)))
                {
                    foreach (var worksheet in package.Workbook.Worksheets)
                    {
                        var paragraph = new Paragraph();
                        paragraph.Inlines.Add(new Run($"Лист: {worksheet.Name}"));
                        paragraph.Inlines.Add(new LineBreak());

                        for (int row = 1; row <= worksheet.Dimension.Rows; row++)
                        {
                            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                            {
                                var cellValue = worksheet.Cells[row, col].Text;
                                paragraph.Inlines.Add(new Run($"{cellValue}\t"));
                            }
                            paragraph.Inlines.Add(new LineBreak());
                        }

                        flowDocument.Blocks.Add(paragraph);
                    }
                }
            }
            catch (Exception ex)
            {
                flowDocument.Blocks.Add(new Paragraph(new Run($"Ошибка чтения Excel файла: {ex.Message}")));
            }
        }

        private void TryLoadAsText(FlowDocument flowDocument)
        {
            try
            {
                flowDocument.Blocks.Add(new Paragraph(new Run(File.ReadAllText(_filePath))));
            }
            catch
            {
                flowDocument.Blocks.Add(new Paragraph(new Run($"Формат файла {Path.GetExtension(_filePath)} не поддерживается")));
            }
        }
    }
}