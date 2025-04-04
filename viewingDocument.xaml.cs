using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls;
using UglyToad.PdfPig;
using Xceed.Words.NET;
using System.Windows.Xps.Packaging;
using System.Printing;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Drawing = DocumentFormat.OpenXml.Wordprocessing.Drawing;
using OpenXmlParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using OpenXmlRun = DocumentFormat.OpenXml.Wordprocessing.Run;
using WpfParagraph = System.Windows.Documents.Paragraph;
using WpfRun = System.Windows.Documents.Run;

namespace GeotekMetallCompleteDesktop
{
    public partial class viewingDocument : Window
    {
        private string _filePath;
        private Dictionary<Image, Size> _originalImageSizes = new Dictionary<Image, Size>();

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

                flowDocument.ColumnWidth = double.PositiveInfinity;
                flowDocument.IsOptimalParagraphEnabled = true;
                flowDocument.PagePadding = new Thickness(20);

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

        private void Image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Image image)
            {
                if (_originalImageSizes.ContainsKey(image))
                {
                    image.Width = _originalImageSizes[image].Width;
                    image.Height = _originalImageSizes[image].Height;
                    _originalImageSizes.Remove(image);
                }
                else
                {
                    _originalImageSizes[image] = new Size(image.Width, image.Height);
                    image.Width = flowDocumentReader.ActualWidth - 40; 
                    image.Height = double.NaN; 
                }
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
            image.MaxWidth = 500;
            image.Cursor = System.Windows.Input.Cursors.Hand;
            image.MouseDown += Image_MouseDown;

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
                        var paragraph = new WpfParagraph();
                        paragraph.Inlines.Add(new WpfRun($"Страница {page.Number}:"));
                        paragraph.Inlines.Add(new LineBreak());
                        paragraph.Inlines.Add(new WpfRun(page.Text));
                        flowDocument.Blocks.Add(paragraph);

                        foreach (var image in page.GetImages())
                        {
                            try
                            {
                                using (var imageMemoryStream = new MemoryStream(image.RawBytes.ToArray()))
                                {
                                    var bitmapImage = new BitmapImage();
                                    bitmapImage.BeginInit();
                                    bitmapImage.StreamSource = imageMemoryStream;
                                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmapImage.EndInit();

                                    var img = new System.Windows.Controls.Image
                                    {
                                        Source = bitmapImage,
                                        Stretch = Stretch.Uniform,
                                        MaxWidth = 500,
                                        Cursor = System.Windows.Input.Cursors.Hand
                                    };
                                    img.MouseDown += Image_MouseDown;

                                    var imageContainer = new BlockUIContainer(img);
                                    flowDocument.Blocks.Add(imageContainer);
                                }
                            }
                            catch (Exception ex)
                            {
                                flowDocument.Blocks.Add(new WpfParagraph(new WpfRun($"Ошибка загрузки изображения: {ex.Message}")));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                flowDocument.Blocks.Add(new WpfParagraph(new WpfRun($"Ошибка чтения PDF: {ex.Message}")));
            }
        }

        private void LoadWordFileWithImages(FlowDocument flowDocument)
        {
            using (var wordDocument = WordprocessingDocument.Open(_filePath, false))
            {
                var body = wordDocument.MainDocumentPart.Document.Body;

                foreach (var paragraph in body.Elements<OpenXmlParagraph>())
                {
                    var flowParagraph = new WpfParagraph();

                    foreach (var run in paragraph.Elements<OpenXmlRun>())
                    {
                        foreach (var text in run.Elements<Text>())
                        {
                           flowParagraph.Inlines.Add(new WpfRun(text.Text));
                        }

                        foreach (var drawing in run.Elements<Drawing>())
                        {
                            var blip = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
                            if (blip != null && blip.Embed != null)
                            {
                                var imagePart = (ImagePart)wordDocument.MainDocumentPart.GetPartById(blip.Embed.Value);
                                using (var stream = imagePart.GetStream())
                                {
                                    var bitmapImage = new BitmapImage();
                                    bitmapImage.BeginInit();
                                    bitmapImage.StreamSource = stream;
                                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmapImage.EndInit();

                                    var img = new System.Windows.Controls.Image
                                    {
                                        Source = bitmapImage,
                                        Stretch = Stretch.Uniform,
                                        MaxWidth = 500,
                                        Cursor = System.Windows.Input.Cursors.Hand
                                    };
                                    img.MouseDown += Image_MouseDown;

                                    flowParagraph.Inlines.Add(new InlineUIContainer(img));
                                }
                            }
                        }
                    }

                    if (flowParagraph.Inlines.Count > 0)
                    {
                        flowDocument.Blocks.Add(flowParagraph);
                    }
                }
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                    var document = flowDocumentReader.Document;
                    var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
                    printDialog.PrintDocument(paginator, $"Печать документа: {Title}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при печати: {ex.Message}", "Ошибка печати",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTextFile(FlowDocument flowDocument)
        {
            flowDocument.Blocks.Add(new WpfParagraph(new WpfRun(File.ReadAllText(_filePath))));
        }

        private void LoadRtfFile(FlowDocument flowDocument)
        {
            using (var stream = new FileStream(_filePath, FileMode.Open))
            {
                var range = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
                range.Load(stream, DataFormats.Rtf);
            }
        }

        private void LoadWordFile(FlowDocument flowDocument)
        {
            try
            {
                if (Path.GetExtension(_filePath).Equals(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    LoadWordFileWithImages(flowDocument);

                    if (flowDocument.Blocks.Count == 0)
                    {
                        flowDocument.Blocks.Add(new WpfParagraph(new WpfRun("Документ не содержит видимого содержимого")));
                    }
                }
                else
                {

                    using (var doc = DocX.Load(_filePath))
                    {
                        foreach (var paragraph in doc.Paragraphs)
                        {
                            flowDocument.Blocks.Add(new WpfParagraph(new WpfRun(paragraph.Text)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                flowDocument.Blocks.Add(new WpfParagraph(new WpfRun($"Ошибка чтения Word документа: {ex.Message}")));
            }
        }

       
        private void TryLoadAsText(FlowDocument flowDocument)
        {
            try
            {
                flowDocument.Blocks.Add(new WpfParagraph(new WpfRun(File.ReadAllText(_filePath))));
            }
            catch
            {
                flowDocument.Blocks.Add(new WpfParagraph(new WpfRun($"Формат файла {Path.GetExtension(_filePath)} не поддерживается")));
            }
        }
    }
}