using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xceed.Words.NET;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WpfImage = System.Windows.Controls.Image;
using WpfParagraph = System.Windows.Documents.Paragraph;
using WpfRun = System.Windows.Documents.Run;
using OpenXmlParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using OpenXmlRun = DocumentFormat.OpenXml.Wordprocessing.Run;
using PdfImage = iTextSharp.text.Image;

namespace GeotekMetallCompleteDesktop
{
    public partial class DocumentEditorWindow : Window
    {
        private string _filePath;
        private string _originalFileType;
        private bool _isNewDocument;
        private bool _isPlaceholderActive = true;
        private Dictionary<WpfImage, Size> _originalImageSizes = new Dictionary<WpfImage, Size>();

        public byte[] DocumentData { get; private set; }
        public string FileName { get; private set; }
        public string FileType => _originalFileType;

        public DocumentEditorWindow(byte[] documentData = null, string fileName = "Новый документ", string fileType = ".docx", bool isNewDocument = true)
        {
            InitializeComponent();
            _originalFileType = fileType.StartsWith(".") ? fileType.ToLower() : "." + fileType.ToLower();
            _isNewDocument = isNewDocument;
            FileName = fileName;

            if (!_isNewDocument && documentData != null)
            {
                DocumentData = documentData;
                Title = $"Редактирование документа: {fileName}";

                try
                {
                    _filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + _originalFileType);
                    File.WriteAllBytes(_filePath, documentData);
                    LoadDocumentContent();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке документа: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            }
            else
            {
                Title = "Создание нового документа";
                _originalFileType = ".docx";
                _filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + _originalFileType);
                ShowPlaceholderText();
            }

            DocumentEditor.TextChanged += DocumentEditor_TextChanged;
        }

        private void LoadDocumentContent()
        {
            try
            {
                var flowDoc = new FlowDocument();
                flowDoc.ColumnWidth = double.PositiveInfinity;

                switch (_originalFileType)
                {
                    case ".txt":
                        LoadTextFile(flowDoc);
                        break;
                    case ".docx":
                        LoadWordFile(flowDoc);
                        break;
                    case ".pdf":
                        LoadPdfFile(flowDoc);
                        break;
                    default:
                        flowDoc.Blocks.Add(new WpfParagraph(new WpfRun("Неподдерживаемый формат файла")));
                        break;
                }

                DocumentEditor.Document = flowDoc;
                DocumentEditor.FontFamily = new System.Windows.Media.FontFamily("Times New Roman");
                DocumentEditor.FontSize = 14;
                _isPlaceholderActive = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении содержимого файла: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void LoadWordFile(FlowDocument flowDoc)
        {
            if (_originalFileType == ".docx")
            {
                using (var wordDoc = WordprocessingDocument.Open(_filePath, false))
                {
                    var body = wordDoc.MainDocumentPart.Document.Body;

                    foreach (var paragraph in body.Elements<OpenXmlParagraph>())
                    {
                        var wpfParagraph = new WpfParagraph();
                        wpfParagraph.Margin = new Thickness(0);

                        foreach (var run in paragraph.Elements<OpenXmlRun>())
                        {
                            foreach (var text in run.Elements<DocumentFormat.OpenXml.Wordprocessing.Text>())
                            {
                                wpfParagraph.Inlines.Add(new WpfRun(text.Text));
                            }

                            foreach (var drawing in run.Elements<DocumentFormat.OpenXml.Wordprocessing.Drawing>())
                            {
                                var blip = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
                                if (blip != null && blip.Embed != null)
                                {
                                    var imagePart = (ImagePart)wordDoc.MainDocumentPart.GetPartById(blip.Embed.Value);
                                    using (var stream = imagePart.GetStream())
                                    {
                                        var bitmapImage = new BitmapImage();
                                        bitmapImage.BeginInit();
                                        bitmapImage.StreamSource = stream;
                                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                        bitmapImage.EndInit();

                                        var img = new WpfImage
                                        {
                                            Source = bitmapImage,
                                            Stretch = Stretch.Uniform,
                                            MaxWidth = 300,
                                            Cursor = Cursors.Hand,
                                            Tag = "from_docx_" + blip.Embed.Value // Сохраняем идентификатор изображения
                                        };
                                        img.MouseDown += Image_MouseDown;

                                        wpfParagraph.Inlines.Add(new InlineUIContainer(img));
                                        wpfParagraph.Inlines.Add(new WpfRun(" "));
                                    }
                                }
                            }
                        }

                        if (wpfParagraph.Inlines.Count > 0)
                        {
                            flowDoc.Blocks.Add(wpfParagraph);
                        }
                    }
                }
            }
            else
            {
                using (var doc = DocX.Load(_filePath))
                {
                    AddTextToFlowDocument(flowDoc, string.Join(Environment.NewLine, doc.Paragraphs.Select(p => p.Text)));
                }
            }
        }
        private void LoadPdfFile(FlowDocument flowDoc)
        {
            try
            {
                using (var pdf = UglyToad.PdfPig.PdfDocument.Open(_filePath))
                {
                    foreach (var page in pdf.GetPages())
                    {
                        var pageParagraph = new WpfParagraph();
                        pageParagraph.Inlines.Add(new WpfRun());
                        pageParagraph.Inlines.Add(new LineBreak());

                        // Получаем текст страницы
                        var text = page.Text;
                        pageParagraph.Inlines.Add(new WpfRun(text));
                        pageParagraph.Inlines.Add(new LineBreak());

                        // Обрабатываем изображения
                        foreach (var image in page.GetImages())
                        {
                            try
                            {
                                var rawBytes = image.RawBytes.ToArray();
                                if (rawBytes.Length == 0) continue;

                                using (var ms = new MemoryStream(rawBytes))
                                {
                                    var bitmapImage = new BitmapImage();
                                    bitmapImage.BeginInit();
                                    bitmapImage.StreamSource = ms;
                                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmapImage.EndInit();

                                    var img = new WpfImage
                                    {
                                        Source = bitmapImage,
                                        Stretch = Stretch.Uniform,
                                        MaxWidth = 300,
                                        Cursor = Cursors.Hand,
                                        Tag = "from_pdf"
                                    };
                                    img.MouseDown += Image_MouseDown;

                                    pageParagraph.Inlines.Add(new InlineUIContainer(img));
                                    pageParagraph.Inlines.Add(new LineBreak());
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                                pageParagraph.Inlines.Add(new WpfRun($"[Изображение не загружено]"));
                                pageParagraph.Inlines.Add(new LineBreak());
                            }
                        }

                        if (pageParagraph.Inlines.Count > 0)
                        {
                            flowDoc.Blocks.Add(pageParagraph);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке PDF: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private BitmapImage LoadPngImage(MemoryStream ms)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }

        private BitmapImage LoadImage(MemoryStream ms)
        {
            var bitmapImage = new BitmapImage();
            try
            {
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
            catch
            {
                // Попробуем альтернативный метод для поврежденных изображений
                try
                {
                    ms.Position = 0;
                    var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    return ConvertToBitmapImage(decoder.Frames[0]);
                }
                catch
                {
                    return null;
                }
            }
        }

        private BitmapImage ConvertToBitmapImage(BitmapFrame frame)
        {
            var bitmapImage = new BitmapImage();
            using (var stream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(frame);
                encoder.Save(stream);
                stream.Position = 0;

                bitmapImage.BeginInit();
                bitmapImage.StreamSource = stream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }
            return bitmapImage;
        }
        private bool IsJpeg(MemoryStream ms)
        {
            var buffer = new byte[2];
            ms.Read(buffer, 0, 2);
            ms.Position = 0;
            return buffer[0] == 0xFF && buffer[1] == 0xD8; // JPEG signature
        }

        private bool IsPng(MemoryStream ms)
        {
            var buffer = new byte[8];
            ms.Read(buffer, 0, 8);
            ms.Position = 0;
            return buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47; // PNG signature
        }

        private BitmapImage LoadJpegImage(MemoryStream ms)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }


        private void LoadTextFile(FlowDocument flowDoc)
        {
            string textContent = File.ReadAllText(_filePath, Encoding.UTF8);
            AddTextToFlowDocument(flowDoc, textContent);
        }

        private void AddTextToFlowDocument(FlowDocument flowDoc, string text)
        {
            var paragraphs = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            foreach (var line in paragraphs)
            {
                var para = new WpfParagraph(new WpfRun(line));
                para.Margin = new Thickness(0);
                flowDoc.Blocks.Add(para);
            }
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is WpfImage image)
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
                    image.Width = DocumentEditor.ActualWidth - 40;
                    image.Height = double.NaN;
                }
            }
        }

        private void InsertImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*",
                Title = "Выберите изображение для вставки"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var fileInfo = new FileInfo(openFileDialog.FileName);
                    if (fileInfo.Length > 5 * 1024 * 1024)
                    {
                        MessageBox.Show("Размер изображения не должен превышать 5MB", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(openFileDialog.FileName);
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    var img = new WpfImage
                    {
                        Source = bitmapImage,
                        Stretch = Stretch.Uniform,
                        MaxWidth = 500,
                        Cursor = Cursors.Hand,
                        Tag = openFileDialog.FileName
                    };
                    img.MouseDown += Image_MouseDown;

                    var imageContainer = new BlockUIContainer(img);
                    DocumentEditor.Document.Blocks.Add(imageContainer);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = new TextRange(DocumentEditor.Document.ContentStart, DocumentEditor.Document.ContentEnd).Text;

                if (_isPlaceholderActive && (string.IsNullOrWhiteSpace(text) || text == "Введите текст документа..."))
                {
                    MessageBox.Show("Документ не содержит текста для сохранения.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dir = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                switch (_originalFileType)
                {
                    case ".txt":
                        File.WriteAllText(_filePath, text, Encoding.UTF8);
                        break;
                    case ".docx":
                        SaveAsDocx();
                        break;
                    case ".pdf":
                        SaveAsPdf();
                        break;
                    default:
                        _originalFileType = ".docx";
                        _filePath = Path.ChangeExtension(_filePath, ".docx");
                        SaveAsDocx();
                        break;
                }

                DocumentData = File.ReadAllBytes(_filePath);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении документа: {ex.Message}\nПуть: {_filePath}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAsDocx()
        {
            string tempImageDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempImageDir);

            try
            {
                using (var doc = DocX.Create(_filePath))
                {
                    var flowDoc = DocumentEditor.Document;
                    int imageCounter = 0;

                    foreach (var block in flowDoc.Blocks)
                    {
                        if (block is WpfParagraph wpfParagraph)
                        {
                            var para = doc.InsertParagraph();
                            foreach (var inline in wpfParagraph.Inlines)
                            {
                                if (inline is WpfRun run)
                                {
                                    para.Append(run.Text);
                                }
                                else if (inline is InlineUIContainer uiContainer && uiContainer.Child is WpfImage image)
                                {
                                    string tempImagePath = Path.Combine(tempImageDir, $"img_{imageCounter++}.png");
                                    SaveImageToFile(image, tempImagePath);

                                    using (var stream = new FileStream(tempImagePath, FileMode.Open))
                                    {
                                        var picture = doc.AddImage(stream);
                                        var img = picture.CreatePicture();
                                        para.AppendPicture(img);
                                    }
                                }
                            }
                        }
                        else if (block is BlockUIContainer uiContainer && uiContainer.Child is WpfImage image)
                        {
                            string tempImagePath = Path.Combine(tempImageDir, $"img_{imageCounter++}.png");
                            SaveImageToFile(image, tempImagePath);

                            using (var stream = new FileStream(tempImagePath, FileMode.Open))
                            {
                                var picture = doc.AddImage(stream);
                                var img = picture.CreatePicture();
                                doc.InsertParagraph().InsertPicture(img);
                            }
                        }
                    }
                    doc.Save();
                }
            }
            finally
            {
                Directory.Delete(tempImageDir, true);
            }
        }

        private void SaveAsPdf()
        {
            string tempImageDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempImageDir);

            try
            {
                using (var fs = new FileStream(_filePath, FileMode.Create))
                {
                    var document = new iTextSharp.text.Document();
                    var writer = PdfWriter.GetInstance(document, fs);
                    document.Open();

                    string fontPath = @"D:\диплом\GeotekMetallCompleteDesktop\timesnewromanpsmt.ttf";
                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    var font = new iTextSharp.text.Font(baseFont, 12);

                    var flowDoc = DocumentEditor.Document;
                    int imageCounter = 0;

                    foreach (var block in flowDoc.Blocks)
                    {
                        if (block is WpfParagraph wpfParagraph)
                        {
                            var paraText = new TextRange(wpfParagraph.ContentStart, wpfParagraph.ContentEnd).Text;
                            var para = new iTextSharp.text.Paragraph(paraText, font);
                            document.Add(para);
                        }
                        else if (block is BlockUIContainer uiContainer && uiContainer.Child is WpfImage image)
                        {
                            string tempImagePath = Path.Combine(tempImageDir, $"img_{imageCounter++}.png");
                            SaveImageToFile(image, tempImagePath);

                            using (var imageStream = new FileStream(tempImagePath, FileMode.Open))
                            {
                                var pdfImage = PdfImage.GetInstance(imageStream);
                                pdfImage.ScaleToFit(document.PageSize.Width - document.LeftMargin - document.RightMargin,
                                                  document.PageSize.Height - document.TopMargin - document.BottomMargin);
                                pdfImage.Alignment = PdfImage.ALIGN_CENTER;
                                document.Add(pdfImage);
                            }
                        }
                    }
                    document.Close();
                }
            }
            finally
            {
                Directory.Delete(tempImageDir, true);
            }
        }

        private void SaveImageToFile(WpfImage image, string filePath)
        {
            if (image.Tag is string originalPath && File.Exists(originalPath))
            {
                File.Copy(originalPath, filePath, true);
            }
            else if (image.Source is BitmapSource bitmapSource)
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(stream);
                }
            }
        }

        private void DocumentEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPlaceholderActive && DocumentEditor.Document.Blocks.Count > 0)
            {
                var text = new TextRange(DocumentEditor.Document.ContentStart, DocumentEditor.Document.ContentEnd).Text;
                if (!string.IsNullOrWhiteSpace(text) && text != "Введите текст документа...")
                {
                    _isPlaceholderActive = false;
                    if (text.StartsWith("Введите текст документа..."))
                    {
                        DocumentEditor.Document.Blocks.Clear();
                        DocumentEditor.Document.Blocks.Add(new WpfParagraph(new WpfRun(text.Replace("Введите текст документа...", ""))));
                    }
                }
            }
        }

        private void ShowPlaceholderText()
        {
            var flowDoc = new FlowDocument();
            var para = new WpfParagraph(new WpfRun("Введите текст документа..."));
            para.Margin = new Thickness(0);
            flowDoc.Blocks.Add(para);

            DocumentEditor.Document = flowDoc;
            DocumentEditor.FontFamily = new System.Windows.Media.FontFamily("Times New Roman");
            DocumentEditor.FontSize = 14;
            _isPlaceholderActive = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}