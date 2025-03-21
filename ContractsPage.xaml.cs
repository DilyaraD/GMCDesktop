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
            var projects = _db.Projects.ToList();
            var selectProjectWindow = new SelectProjectWindow(projects);
            if (selectProjectWindow.ShowDialog() == true)
            {
                _selectedProject = selectProjectWindow.SelectedProject;
                SelectedProjectTextBlock.Text = _selectedProject.Requests.ObjectName;
                ContractsListView.Visibility = Visibility.Collapsed;
                AddContractPanel.Visibility = Visibility.Visible;
            }
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
            }
        }

        private void ResetProjectButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedProject = null;
            ContractsListView.ItemsSource = _contracts;
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

        private void DownloadContractButton_Click(object sender, RoutedEventArgs e)
        {
            var contract = (sender as Button)?.DataContext as Contracts;
            if (contract?.FilePath != null)
            {
                var saveFileDialog = new SaveFileDialog
                {
                    FileName = $"Contract_{contract.ContractID}.{contract.FileType}",
                    Filter = $"{contract.FileType} files (*.{contract.FileType})|*.{contract.FileType}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllBytes(saveFileDialog.FileName, contract.FilePath);
                }
            }
        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
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
            if (_selectedFile == null || _selectedProject == null)
            {
                MessageBox.Show("Выберите файл и проект.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ContractsListView.Visibility = Visibility.Visible;
            AddContractPanel.Visibility = Visibility.Collapsed;
            _selectedFile = null;
            _selectedFileType = null;
            _selectedFileName = null;
            _selectedProject = null;
            SelectedProjectTextBlock.Text = string.Empty;
            SelectedFileTextBlock.Text = string.Empty;
        }
    }
}