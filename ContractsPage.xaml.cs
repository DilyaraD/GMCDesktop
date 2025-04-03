using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace GeotekMetallCompleteDesktop
{
    public partial class ContractsPage : Page
    {
        public Users _user;
        private List<Contracts> _contracts;
        private byte[] _selectedFile;
        private string _selectedFileType;
        private string _selectedFileName;
        private Projects _selectedProject;
        private GeotekMetallCompleteEntities1 _db;

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
            ContractsListView.Visibility = Visibility.Collapsed;
            AddContractPanel.Visibility = Visibility.Visible;
            FilterPanel.Visibility = Visibility.Collapsed;
        }

        private void CreateNewContractButton_Click(object sender, RoutedEventArgs e)
        {
            var editor = new DocumentEditorWindow();
            if (editor.ShowDialog() == true)
            {
                _selectedFile = editor.DocumentData;
                _selectedFileType = editor.FileType;
                SelectedFileTextBlock.Text = "Новый документ" + _selectedFileType;
            }
        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Документы (*.docx;*.pdf;*.txt)|*.docx;*.pdf;*.txt|Word (*.docx)|*.docx|PDF (*.pdf)|*.pdf|Текстовые файлы (*.txt)|*.txt"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFile = File.ReadAllBytes(openFileDialog.FileName);
                _selectedFileType = Path.GetExtension(openFileDialog.FileName).TrimStart('.');
                _selectedFileName = Path.GetFileName(openFileDialog.FileName);
                SelectedFileTextBlock.Text = _selectedFileName;
            }
        }

        private void SaveContractButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProject == null)
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

            try
            {
                var newContract = new Contracts
                {
                    ProjectID = _selectedProject.ProjectID,
                    ContractDate = DateTime.Now,
                    FilePath = _selectedFile,
                    FileType = _selectedFileType
                };

                _db.Contracts.Add(newContract);
                _db.SaveChanges();

                LoadContracts();
                CancelButton_Click(sender, e);
                MessageBox.Show("Договор успешно добавлен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении договора: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewContractButton_Click(object sender, RoutedEventArgs e)
        {
            var contract = (sender as Button)?.DataContext as Contracts;
            if (contract?.FilePath != null)
            {
                try
                {
                    var tempFilePath = Path.GetTempFileName() + "." + contract.FileType;
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
                var editor = new DocumentEditorWindow(
                    documentData: contract.FilePath,
                    fileName: $"Договор N{contract.ContractID}.{contract.FileType}",
                    fileType: contract.FileType,
                    isNewDocument: false);

                if (editor.ShowDialog() == true)
                {
                    contract.FilePath = editor.DocumentData;
                    contract.FileType = editor.FileType;
                    _db.SaveChanges();
                    if (_selectedProject != null)
                    {
                        var filteredContracts = _contracts.Where(c => c.ProjectID == _selectedProject.ProjectID).ToList();
                        ContractsListView.ItemsSource = filteredContracts;
                    }
                    else
                    {
                        ContractsListView.ItemsSource = _contracts;
                    }
                }
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
                case ".txt": return "Text File (*.txt)|*.txt";
                default: return "All Files (*.*)|*.*";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ContractsListView.Visibility = Visibility.Visible;
            AddContractPanel.Visibility = Visibility.Collapsed;
            FilterPanel.Visibility = Visibility.Visible;
            _selectedFile = null;
            _selectedFileType = null;
            _selectedFileName = null;
            _selectedProject = null;
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