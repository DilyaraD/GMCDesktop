using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace GeotekMetallCompleteDesktop
{
    public partial class FinanceManagementPage : Page
    {
        public Users _user;
        private GeotekMetallCompleteEntities1 _db;
        //private byte[] _FilePath;
        private Projects _selectedProject;

        public FinanceManagementPage(Users user, int? projectId = null)
        {
            InitializeComponent();
            _user = user;
            _db = new GeotekMetallCompleteEntities1();
            if (projectId.HasValue)
            {
                _selectedProject = _db.Projects.Find(projectId);
                var filteredReports = _db.FinancialReports
                    .Where(r => r.ProjectID == projectId)
                    .ToList();
                FinancialReportsListView.ItemsSource = filteredReports;
            }
            else
            {
                LoadFinancialReports();
            }
        }

        private void LoadFinancialReports()
        {
            FinancialReportsListView.ItemsSource = _db.FinancialReports.Include("Projects").ToList();
        }

        //private void AddReportButton_Click(object sender, RoutedEventArgs e)
        //{
        //    FinancialReportsListView.Visibility = Visibility.Collapsed;
        //    AddReportStackPanel.Visibility = Visibility.Visible;
        //}

        private void SelectProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var projects = _db.Projects.ToList();
            var selectProjectWindow = new SelectProjectWindow(projects);
            if (selectProjectWindow.ShowDialog() == true)
            {
                _selectedProject = selectProjectWindow.SelectedProject;
                var filteredReports = _db.FinancialReports
                    .Where(r => r.ProjectID == _selectedProject.ProjectID)
                    .ToList();
                FinancialReportsListView.ItemsSource = filteredReports;
            }
        }

        private void ResetProjectButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedProject = null;
            LoadFinancialReports(); // Сброс фильтрации
        }

        //private void ChangeProjectButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var projects = _db.Projects.ToList();
        //    var selectProjectWindow = new SelectProjectWindow(projects);
        //    if (selectProjectWindow.ShowDialog() == true)
        //    {
        //        _selectedProject = selectProjectWindow.SelectedProject;
        //        SelectedProjectTextBlock.Text = _selectedProject.Requests.ObjectName;
        //    }
        //}

        //private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var openFileDialog = new OpenFileDialog();
        //    if (openFileDialog.ShowDialog() == true)
        //    {
        //        _FilePath = File.ReadAllBytes(openFileDialog.FileName);
        //        SelectedFilePathTextBlock.Text = Path.GetFileName(openFileDialog.FileName);
        //    }
        //}

        //private void SaveReportButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (string.IsNullOrEmpty(ReportTypeTextBox.Text))
        //    {
        //        MessageBox.Show("Введите название отчета.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        //        return;
        //    }

        //    if (_FilePath == null || _FilePath.Length == 0)
        //    {
        //        MessageBox.Show("Выберите файл отчета.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        //        return;
        //    }

        //    if (_selectedProject == null)
        //    {
        //        MessageBox.Show("Выберите проект.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        //        return;
        //    }

        //    var newReport = new FinancialReports
        //    {
        //        ProjectID = _selectedProject.ProjectID,
        //        ReportType = ReportTypeTextBox.Text,
        //        ReportDate = DateTime.Now,
        //        FilePath = _FilePath,
        //        FileType = Path.GetExtension(SelectedFilePathTextBlock.Text).TrimStart('.')
        //    };

        //    _db.FinancialReports.Add(newReport);
        //    _db.SaveChanges();

        //    LoadFinancialReports();
        //    BackButton_Click(null, null);
        //}

        //private void BackButton_Click(object sender, RoutedEventArgs e)
        //{
        //    ReportTypeTextBox.Text = string.Empty;
        //    _FilePath = null;
        //    SelectedFilePathTextBlock.Text = string.Empty;
        //    _selectedProject = null;
        //    SelectedProjectTextBlock.Text = string.Empty;

        //    FinancialReportsListView.Visibility = Visibility.Visible;
        //    AddReportStackPanel.Visibility = Visibility.Collapsed;
        //}

        private void DownloadReportButton_Click(object sender, RoutedEventArgs e)
        {
            var report = (sender as Button)?.Tag as FinancialReports;
            if (report != null && report.FilePath != null && report.FilePath.Length > 0)
            {
                try
                {
                    var saveFileDialog = new SaveFileDialog
                    {
                        FileName = $"{report.ReportType}.{report.FileType}",
                        Filter = $"{report.FileType} files (*.{report.FileType})|*.{report.FileType}"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        File.WriteAllBytes(saveFileDialog.FileName, report.FilePath);
                        MessageBox.Show("Файл успешно скачан.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при скачивании файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Файл отчета не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewReportButton_Click(object sender, RoutedEventArgs e)
        {
            var report = (sender as Button)?.Tag as FinancialReports;
            if (report != null && report.FilePath != null && report.FilePath.Length > 0)
            {
                try
                {
                    // Создаем временный файл
                    string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{report.FileType}");
                    File.WriteAllBytes(tempFilePath, report.FilePath);

                    // Открываем окно просмотра
                    var viewWindow = new viewingDocument(tempFilePath, report.ReportType);
                    viewWindow.ShowDialog();

                    // Удаляем временный файл
                    try { File.Delete(tempFilePath); } catch { /* Игнорируем */ }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии файла: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}