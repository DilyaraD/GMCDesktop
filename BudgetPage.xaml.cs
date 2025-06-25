using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeotekMetallCompleteDesktop
{
    public partial class BudgetPage : Page
    {
        public Users _user;
        private GeotekMetallCompleteEntities1 _db;
        private Projects _selectedProject;
        private List<AttachedFile> _attachedFiles = new List<AttachedFile>();

        public class AttachedFile
        {
            public byte[] FileData { get; set; }
            public string FileName { get; set; }
            public string FileExtension { get; set; }
        }

        public BudgetPage(Users user)
        {
            InitializeComponent();
            _user = user;
            _db = new GeotekMetallCompleteEntities1();
            LoadTransactions();
            ResetProjectButton.Visibility = Visibility.Collapsed;
        }

        private void LoadTransactions()
        {
            TransactionsListView.ItemsSource = _db.BudgetTransactions
                .Include("Projects")
                .Include("Users")
                .Include("FinancialReports")
                .ToList();
        }

        private void AddTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            TransactionsListView.Visibility = Visibility.Collapsed;
            AddTransactionPanel.Visibility = Visibility.Visible;
            ResetProjectButton.Visibility = Visibility.Collapsed;
            SelectBtm.Visibility = Visibility.Collapsed;
            _attachedFiles.Clear();
            UpdateAttachedFilesList();
        }

        private void SelectProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var projects = _db.Projects.ToList();
            var selectProjectWindow = new SelectProjectWindow(projects);
            if (selectProjectWindow.ShowDialog() == true)
            {
                _selectedProject = selectProjectWindow.SelectedProject;
                var filteredTransactions = _db.BudgetTransactions
                    .Where(t => t.ProjectID == _selectedProject.ProjectID)
                    .ToList();
                TransactionsListView.ItemsSource = filteredTransactions;
                ResetProjectButton.Visibility = Visibility.Visible;
            }
        }

        private void ResetProjectButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedProject = null;
            LoadTransactions();
            ResetProjectButton.Visibility = Visibility.Collapsed;
        }

        private void ChangeProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var projects = _db.Projects.ToList();
            var selectProjectWindow = new SelectProjectWindow(projects);
            if (selectProjectWindow.ShowDialog() == true)
            {
                _selectedProject = selectProjectWindow.SelectedProject;
                SelectedProjectTextBlock.Text = _selectedProject.Requests.ObjectName;
            }
        }

        private void TransactionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TransactionTypeComboBox.SelectedItem != null)
            {
                var selectedItem = (ComboBoxItem)TransactionTypeComboBox.SelectedItem;
                if (selectedItem.Content.ToString() == "Другое")
                {
                    CustomDescriptionPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    CustomDescriptionPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SaveTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedProject == null)
                {
                    ShowMessage("Ошибка", "Выберите проект.", MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(AmountTextBox.Text) || !decimal.TryParse(AmountTextBox.Text, out decimal amount))
                {
                    ShowMessage("Ошибка", "Введите корректную сумму.", MessageBoxImage.Warning);
                    return;
                }

                if (amount < 1m)
                {
                    ShowMessage("Ошибка", "Сумма не может быть меньше 1 рубля", MessageBoxImage.Warning);
                    AmountTextBox.Focus();
                    return;
                }

                if (amount > 10_000_000m)
                {
                    ShowMessage("Ошибка", "Сумма не может превышать 10 000 000 рублей", MessageBoxImage.Warning);
                    AmountTextBox.Focus();
                    return;
                }

                string description;
                if (TransactionTypeComboBox.SelectedItem == null)
                {
                    ShowMessage("Ошибка", "Выберите тип транзакции.", MessageBoxImage.Warning);
                    return;
                }

                var selectedType = (ComboBoxItem)TransactionTypeComboBox.SelectedItem;
                if (selectedType.Content.ToString() == "Другое")
                {
                    if (string.IsNullOrEmpty(DescriptionTextBox.Text))
                    {
                        ShowMessage("Ошибка", "Введите название транзакции.", MessageBoxImage.Warning);
                        return;
                    }
                    description = DescriptionTextBox.Text;
                }
                else
                {
                    description = selectedType.Content.ToString();
                }

                if (!_attachedFiles.Any())
                {
                    var result = MessageBox.Show("К транзакции не прикреплено ни одного документа. Продолжить сохранение?",
                                               "Нет прикрепленных документов",
                                               MessageBoxButton.YesNo,
                                               MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                var newTransaction = new BudgetTransactions
                {
                    ProjectID = _selectedProject.ProjectID,
                    TransactionDate = DateTime.Now,
                    Amount = amount,
                    Description = description,
                    RecordedBy = _user.UserID
                };

                _db.BudgetTransactions.Add(newTransaction);
                _db.SaveChanges();

                foreach (var file in _attachedFiles)
                {
                    string safeReportType = $"Документ к транзакции N{newTransaction.TransactionID}_{description}"
                        .Replace(":", "_");

                    var report = new FinancialReports
                    {
                        ProjectID = _selectedProject.ProjectID,
                        ReportType = safeReportType,
                        ReportDate = DateTime.Now,
                        FilePath = file.FileData,
                        FileType = file.FileExtension,
                        TransactionID = newTransaction.TransactionID
                    };
                    _db.FinancialReports.Add(report);
                }

                _db.SaveChanges();

                ShowMessage("Успешно", "Транзакция успешно добавлена!", MessageBoxImage.Information);

                LoadTransactions();
                CancelButton_Click(null, null);
            }
            catch (Exception ex)
            {
                ShowMessage("Ошибка", $"Не удалось добавить транзакцию: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Документы и изображения (*.pdf;*.doc;*.docx;*.png;*.jpg;*.jpeg)|*.pdf;*.doc;*.docx;*.png;*.jpg;*.jpeg|Все файлы (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                AttachedFilesListBox.Visibility = Visibility.Visible;
                int totalFilesAfterAdd = _attachedFiles.Count + openFileDialog.FileNames.Length;
                if (totalFilesAfterAdd > 5)
                {
                    MessageBox.Show($"Можно прикрепить не более 5 файлов.\n" +
                                  $"У вас уже прикреплено {_attachedFiles.Count}, пытаетесь добавить еще {openFileDialog.FileNames.Length}.",
                                  "Превышен лимит файлов",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }
                if (IsFileLocked(openFileDialog.FileName))
                {
                    MessageBox.Show($"Файл '{Path.GetFileName(openFileDialog.FileName)}' открыт в другой программе.\n" +
                                  "Закройте файл и попробуйте снова.",
                                  "Файл занят",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }
                foreach (var fileName in openFileDialog.FileNames)
                {
                    try
                    {
                        using (var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            fileStream.Close();
                        }

                        _attachedFiles.Add(new AttachedFile
                        {
                            FileData = File.ReadAllBytes(fileName),
                            FileName = Path.GetFileNameWithoutExtension(fileName),
                            FileExtension = NormalizeFileType(Path.GetExtension(fileName))
                        });
                    }
                    catch (IOException)
                    {
                        MessageBox.Show($"Файл '{Path.GetFileName(fileName)}' открыт в другой программе.\n" +
                                      "Закройте файл и попробуйте снова.",
                                      "Файл занят",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Warning);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при обработке файла '{Path.GetFileName(fileName)}':\n{ex.Message}",
                                      "Ошибка",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Error);
                        continue;
                    }
                }
                UpdateAttachedFilesList();
            }
        }
        private bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }
        private string NormalizeFileType(string fileType)
        {
            if (string.IsNullOrEmpty(fileType)) return ".unknown";

            if (!fileType.StartsWith("."))
            {
                fileType = "." + fileType;
            }

            return fileType.ToLower();
        }

        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (AttachedFilesListBox.SelectedIndex >= 0)
            {
                _attachedFiles.RemoveAt(AttachedFilesListBox.SelectedIndex);
                UpdateAttachedFilesList();
            }
        }

        private void UpdateAttachedFilesList()
        {
            AttachedFilesListBox.ItemsSource = _attachedFiles.Select(f => $"{f.FileName}.{f.FileExtension}").ToList();

            var removeButton = FindRemoveButton();
            if (removeButton != null)
            {
                removeButton.Visibility = _attachedFiles.Any() ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private Button FindRemoveButton()
        {
            var stackPanel = AddTransactionPanel.FindName("AttachedFilesStackPanel") as StackPanel;
            if (stackPanel != null)
            {
                foreach (var child in stackPanel.Children)
                {
                    if (child is StackPanel innerPanel)
                    {
                        foreach (var innerChild in innerPanel.Children)
                        {
                            if (innerChild is Button button && button.Content.ToString() == "Удалить")
                            {
                                return button;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private void TransactionsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedTransaction = TransactionsListView.SelectedItem as BudgetTransactions;
            if (selectedTransaction != null)
            {
                NavigationService.Navigate(new FinanceManagementPage(_user, selectedTransaction.TransactionID));
            }
        }

        private void ShowMessage(string title, string message, MessageBoxImage icon)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            TransactionsListView.Visibility = Visibility.Visible;
            AddTransactionPanel.Visibility = Visibility.Collapsed;
            FilterPanel.Visibility = Visibility.Visible;
            SelectBtm.Visibility = Visibility.Visible;
            _selectedProject = null;
            SelectedProjectTextBlock.Text = string.Empty;
            AmountTextBox.Text = string.Empty;
            DescriptionTextBox.Text = string.Empty;
            TransactionTypeComboBox.SelectedIndex = -1;
            CustomDescriptionPanel.Visibility = Visibility.Collapsed;
            _attachedFiles.Clear();
            AttachedFilesListBox.ItemsSource = null;
        }

        private void AmountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$");
            if (!regex.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text)))
            {
                e.Handled = true;
            }

            TextBox textBox = sender as TextBox;

            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            if (textBox.Text.Length == 0 && e.Text == "0")
            {
                e.Handled = true;
            }
        }

        private void AmountTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }
    }
}