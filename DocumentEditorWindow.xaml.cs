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
using UglyToad.PdfPig;
using Xceed.Words.NET;
using WpfImage = System.Windows.Controls.Image;
using WpfParagraph = System.Windows.Documents.Paragraph;
using iTextParagraph = iTextSharp.text.Paragraph;
using iTextSharp.text.pdf.parser;
using Paragraph = System.Windows.Documents.Paragraph;
using Path = System.IO.Path;
using PdfDocument = UglyToad.PdfPig.PdfDocument;
using System.Linq;

namespace GeotekMetallCompleteDesktop
{
    public partial class DocumentEditorWindow : Window
{
    private string _filePath;
    private string _originalFileType;
    private bool _isNewDocument;
    private bool _isPlaceholderActive = true;

    public byte[] DocumentData { get; private set; }
    public string FileName { get; private set; }
    public string FileType => _originalFileType;

    public DocumentEditorWindow(byte[] documentData = null, string fileName = "Новый документ", string fileType = ".docx", bool isNewDocument = true)
    {
        InitializeComponent();

        // Всегда сохраняем FileType с точкой
        _originalFileType = fileType.StartsWith(".") ? fileType.ToLower() : "." + fileType.ToLower();
        _isNewDocument = isNewDocument;
        FileName = fileName;

        if (!_isNewDocument && documentData != null)
        {
            DocumentData = documentData;
            Title = $"Редактирование документа: {fileName}";

            try
            {
                // Исправлено: правильное формирование временного файла с точкой
                _filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + _originalFileType);
                File.WriteAllBytes(_filePath, documentData);
                LoadTextContent();
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
            // Для новых документов всегда используем .docx по умолчанию
            _originalFileType = ".docx";
            // Исправлено: правильное формирование временного файла
            _filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + _originalFileType);
            ShowPlaceholderText();
        }

        DocumentEditor.TextChanged += DocumentEditor_TextChanged;
    }

    private void LoadTextContent()
    {
                try
                {
                    string textContent = string.Empty;

                    switch (_originalFileType)
                    {
                        case ".txt":
                            textContent = File.ReadAllText(_filePath, Encoding.UTF8);
                            break;
                        case ".docx":
                            using (var doc = DocX.Load(_filePath))
                            {
                                // Сохраняем оригинальные переносы строк из DOCX
                                textContent = string.Join(Environment.NewLine,
                                    doc.Paragraphs.Select(p => p.Text));
                            }
                            break;
                        case ".pdf":
                            using (var pdf = PdfDocument.Open(_filePath))
                            {
                                var sb = new StringBuilder();
                                foreach (var page in pdf.GetPages())
                                {
                                    // Сохраняем оригинальные переносы строк из PDF
                                    sb.Append(page.Text);
                                    sb.Append(Environment.NewLine); // Добавляем перенос после каждой страницы
                                }
                                textContent = sb.ToString();
                            }
                            break;
                        default:
                            textContent = "Неподдерживаемый формат файла. Содержимое не может быть отображено.";
                            break;
                    }

                    // Создаем документ с сохранением переносов строк
                    var flowDoc = new FlowDocument();
                    // Разбиваем текст с учетом всех вариантов переносов строк
                    var paragraphs = textContent.Split(new[] { "\r\n", "\n", "\r" },
                        StringSplitOptions.None);

                    foreach (var line in paragraphs)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            var para = new Paragraph(new Run(line));
                            para.Margin = new Thickness(0);
                            flowDoc.Blocks.Add(para);
                        }
                        else
                        {
                            // Добавляем пустой параграф для пустых строк
                            flowDoc.Blocks.Add(new Paragraph(new Run("")));
                        }
                    }

                    DocumentEditor.Document = flowDoc;
                    DocumentEditor.FontFamily = new FontFamily("Times New Roman");
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

            // Убедимся, что директория существует
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
                        if (!_isNewDocument && File.Exists(_filePath))
                        {
                            // Для существующих DOCX файлов - редактируем
                            using (var doc = DocX.Load(_filePath))
                            {
                                // Получаем все параграфы и удаляем их
                                var paragraphsToRemove = doc.Paragraphs.ToList();
                                foreach (var para in paragraphsToRemove)
                                {
                                    para.Remove(false); // false - не сохранять отслеживание изменений
                                }

                                // Добавляем новые параграфы
                                var paragraphs = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                                foreach (var paraText in paragraphs)
                                {
                                    var para = doc.InsertParagraph(paraText);
                                    para.Font("Times New Roman").FontSize(14);
                                }
                                doc.Save();
                            }
                        }
                        else
                        {
                            // Для новых файлов - создаем
                            using (var doc = DocX.Create(_filePath))
                            {
                                var paragraphs = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                                foreach (var paraText in paragraphs)
                                {
                                    var para = doc.InsertParagraph(paraText);
                                    para.Font("Times New Roman").FontSize(14);
                                }
                                doc.Save();
                            }
                        }
                        break;
                    case ".pdf":
                        using (var fs = new FileStream(_filePath, FileMode.Create))
                        {
                            var document = new iTextSharp.text.Document();
                            var writer = PdfWriter.GetInstance(document, fs);
                            document.Open();

                            string fontPath = @"D:\диплом\GeotekMetallCompleteDesktop\timesnewromanpsmt.ttf";

                            try
                            {
                                BaseFont baseFont = BaseFont.CreateFont(
                                    fontPath,
                                    BaseFont.IDENTITY_H,
                                    BaseFont.EMBEDDED);
                                var font = new iTextSharp.text.Font(baseFont, 12);

                                var flowDoc = DocumentEditor.Document;
                                foreach (var block in flowDoc.Blocks)
                                {
                                    if (block is Paragraph wpfParagraph)
                                    {
                                        var paraText = new TextRange(wpfParagraph.ContentStart, wpfParagraph.ContentEnd).Text;

                                        var lines = paraText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

                                        foreach (var line in lines)
                                        {
                                            iTextSharp.text.Paragraph para = new iTextSharp.text.Paragraph(line, font)
                                            {
                                                IndentationLeft = 20f,
                                                SpacingAfter = 5f
                                            };
                                            document.Add(para);
                                        }

                                        document.Add(new iTextSharp.text.Paragraph(" ", font));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Ошибка при создании PDF: {ex.Message}", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                            finally
                            {
                                document.Close();
                            }
                        }
                        break;

                    default:
                    _originalFileType = ".docx";
                    _filePath = Path.ChangeExtension(_filePath, ".docx");
                    goto case ".docx";
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
                        DocumentEditor.Document.Blocks.Add(new Paragraph(new Run(text.Replace("Введите текст документа...", ""))));
                    }
                }
            }
        }

        private void ShowPlaceholderText()
        {
            var flowDoc = new FlowDocument();
            var para = new Paragraph(new Run("Введите текст документа..."));
            para.Margin = new Thickness(0);
            flowDoc.Blocks.Add(para);

            DocumentEditor.Document = flowDoc;
            DocumentEditor.FontFamily = new FontFamily("Times New Roman");
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