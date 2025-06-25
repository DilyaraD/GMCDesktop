using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GeotekMetallCompleteDesktop
{
    public partial class ContractsPage : Page
    {
        private Users _user;
        private GeotekMetallCompleteEntities1 _db;
        private Projects _selectedProject;
        private byte[] _selectedFile;
        private string _selectedFileType;
        private List<Contracts> _contracts;
        private Contracts _currentEditingContract;
        private Contracts _currEditingContract;
        public ContractsPage(Users user)
        {
            InitializeComponent();
            _user = user;
            _db = new GeotekMetallCompleteEntities1();
            LoadContracts();
        }
        private void LoadContracts()
        {
            _contracts = _db.Contracts.ToList();
            ContractsListView.ItemsSource = _contracts;
        }
        private void AddContractButton_Click(object sender, RoutedEventArgs e)
        {
            ResetFormState();
            ContractsListView.Visibility = Visibility.Collapsed;
            AddContractPanel.Visibility = Visibility.Visible;
            SelectButton.Visibility = Visibility.Collapsed;
            HeaderTextBlock.Text = "Добавление договора";
        }
        private void CreateNewContractButton_Click(object sender, RoutedEventArgs e)
        {
            var editor = new DocumentEditorWindow();
            if (editor.ShowDialog() == true)
            {
                _selectedFile = editor.DocumentData;
                _selectedFileType = NormalizeFileType(editor.FileType);
                SelectedFileTextBlock.Text = "Новый документ" + _selectedFileType;
            }
        }
        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Документы (*.docx;*.pdf)|*.docx;*.pdf|Word (*.docx)|*.docx|PDF (*.pdf)|*.pdf"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                if (IsFileLocked(openFileDialog.FileName))
                {
                    MessageBox.Show($"Файл '{Path.GetFileName(openFileDialog.FileName)}' открыт в другой программе.\n" +
                                  "Закройте файл и попробуйте снова.",
                                  "Файл занят",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    _selectedFile = File.ReadAllBytes(openFileDialog.FileName);
                    _selectedFileType = NormalizeFileType(Path.GetExtension(openFileDialog.FileName));
                    SelectedFileTextBlock.Text = Path.GetFileName(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при чтении файла:\n{ex.Message}",
                                  "Ошибка",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);

                    _selectedFile = null;
                    _selectedFileType = null;
                    SelectedFileTextBlock.Text = "Файл не выбран";
                }
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
        private void ViewContractButton_Click(object sender, RoutedEventArgs e)
        {
            var contract = (sender as Button)?.DataContext as Contracts;
            if (contract?.FilePath != null)
            {
                try
                {
                    var tempFilePath = Path.GetTempFileName() + contract.FileType;
                    File.WriteAllBytes(tempFilePath, contract.FilePath);


                    var viewer = new viewingDocument(tempFilePath, $"Договор N{contract.ContractID}");
                    viewer.Show();

                    viewer.Closed += (s, args) =>
                    {
                        try { File.Delete(tempFilePath); } catch { }
                    };
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии документа: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void EditContractButton_Click(object sender, RoutedEventArgs e)
        {
            var contract = (sender as Button)?.DataContext as Contracts;
            if (contract?.FilePath != null)
            {
                if (contract.FileType.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    OpenPdfEditForm(contract);
                }
                else
                {
                    var editor = new DocumentEditorWindow(
                        documentData: contract.FilePath,
                        fileName: $"Договор N{contract.ContractID}{contract.FileType}",
                        fileType: contract.FileType,
                        isNewDocument: false);
                    _currEditingContract = contract;

                    if (editor.ShowDialog() == true)
                    {
                        contract.FilePath = editor.DocumentData;
                        contract.FileType = NormalizeFileType(editor.FileType);
                        contract.ContractDate = DateTime.Now;
                        _db.SaveChanges();
                        LoadContracts();
                    }
                }
            }
        }
        private void OpenPdfEditForm(Contracts contract)
        {
            _currentEditingContract = contract;

            ResetFormState();
            ContractsListView.Visibility = Visibility.Collapsed;
            AddContractPanel.Visibility = Visibility.Visible;
            SelectButton.Visibility = Visibility.Collapsed;

            HeaderTextBlock.Text = "Редактирование договора";

            _selectedProject = _db.Projects.FirstOrDefault(p => p.ProjectID == contract.ProjectID);
            if (_selectedProject != null)
            {
                SelectedProjectTextBlock.Text = _selectedProject.Requests.ObjectName;
            }

            ChangeProjectButton.Visibility = Visibility.Collapsed;

            _selectedFile = contract.FilePath;
            _selectedFileType = contract.FileType;
            SelectedFileTextBlock.Text = $"Договор N{contract.ContractID}{contract.FileType}";
            _currEditingContract = contract;
            SaveContractButton.Content = "Обновить";
        }
        private void DeleteContractButton_Click(object sender, RoutedEventArgs e)
        {
            var contract = (sender as Button)?.DataContext as Contracts;
            if (contract != null)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить этот документ? Это действие нельзя отменить.",
                                            "Подтверждение удаления",
                                            MessageBoxButton.YesNo,
                                            MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _db.Contracts.Remove(contract);
                        _db.SaveChanges();
                        LoadContracts();
                        MessageBox.Show("Документ успешно удален", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении документа: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private void SaveContractButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currEditingContract == null && _selectedProject == null)
            {
                var projects = _db.Projects.ToList();
                var selectProjectWindow = new SelectProjectWindow(projects);
                if (selectProjectWindow.ShowDialog() != true)
                {
                    MessageBox.Show("Необходимо выбрать проект", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _selectedProject = selectProjectWindow.SelectedProject;
                SelectedProjectTextBlock.Text = _selectedProject.Requests.ObjectName;
            }
            if (_selectedFile == null || _selectedFile.Length == 0)
            {
                MessageBox.Show("Выберите файл для загрузки", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var currentEditingContract = _currEditingContract;
            try
            {
                if (currentEditingContract != null)
                {
                    currentEditingContract.FilePath = _selectedFile;
                    currentEditingContract.FileType = _selectedFileType;
                    currentEditingContract.ContractDate = DateTime.Now;
                }
                else
                {
                    var newContract = new Contracts
                    {
                        ProjectID = _selectedProject.ProjectID,
                        ContractDate = DateTime.Now,
                        FilePath = _selectedFile,
                        FileType = _selectedFileType
                    };
                    _db.Contracts.Add(newContract);
                }
                _db.SaveChanges();
                LoadContracts();
                CancelButton_Click(sender, e);

                MessageBox.Show(_currentEditingContract != null ? "Договор успешно обновлен" : "Договор успешно добавлен",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении договора: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DownloadContractButton_Click(object sender, RoutedEventArgs e)
        {
            var contract = (sender as Button)?.DataContext as Contracts;
            if (contract?.FilePath != null)
            {
                var saveFileDialog = new SaveFileDialog
                {
                    FileName = $"Договор_N{contract.ContractID}{contract.FileType}",
                    Filter = GetFilterForFileType(contract.FileType)
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        File.WriteAllBytes(saveFileDialog.FileName, contract.FilePath);
                        MessageBox.Show("Файл успешно сохранен", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private string GetFilterForFileType(string fileType)
        {
            switch (fileType.ToLower())
            {
                case ".docx": return "Word Document (*.docx)|*.docx";
                case ".pdf": return "PDF File (*.pdf)|*.pdf";
                default: return "All Files (*.*)|*.*";
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ResetFormState();
            _currentEditingContract = null;
            _currEditingContract = null;
            ContractsListView.Visibility = Visibility.Visible;
            AddContractPanel.Visibility = Visibility.Collapsed;
            SelectButton.Visibility = Visibility.Visible;
            HeaderTextBlock.Text = "Добавление договора";
        }
        private void ResetFormState()
        {
            _currentEditingContract = null;
            _selectedFile = null;
            _selectedFileType = null;
            _selectedProject = null;
            _currEditingContract = null;
            ChangeProjectButton.Visibility = Visibility.Visible;
            SaveContractButton.Content = "Сохранить";
            SelectedProjectTextBlock.Text = string.Empty;
            SelectedFileTextBlock.Text = string.Empty;
        }
        private void SelectProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var projects = _db.Projects.ToList();
            var selectProjectWindow = new SelectProjectWindow(projects);
            if (selectProjectWindow.ShowDialog() == true)
            {
                _selectedProject = selectProjectWindow.SelectedProject;
                var filteredContracts = _contracts.Where(c => c.ProjectID == _selectedProject.ProjectID).ToList();
                ContractsListView.ItemsSource = filteredContracts;
                ResetProjectButton.Visibility = Visibility.Visible;
            }
        }
        private void ResetProjectButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedProject = null;
            ContractsListView.ItemsSource = _contracts;
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
    }
}