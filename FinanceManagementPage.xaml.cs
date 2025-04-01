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
                Filter.Visibility= Visibility.Collapsed;
            }
            else
            {
                Filter.Visibility = Visibility.Visible;
                LoadFinancialReports();
            }
        }

        private void LoadFinancialReports()
        {
            FinancialReportsListView.ItemsSource = _db.FinancialReports.Include("Projects").ToList();
            if (_selectedProject == null)
            {
                ResetProjectButton.Visibility = Visibility.Collapsed;
            }
        }

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
                ResetProjectButton.Visibility = Visibility.Visible;
            }
        }

        private void ResetProjectButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedProject = null;
            LoadFinancialReports();
        }

        private void DownloadReportButton_Click(object sender, RoutedEventArgs e)
        {
            var report = (sender as Button)?.Tag as FinancialReports;
            if (report != null && report.FilePath != null && report.FilePath.Length > 0)
            {
                try
                {
                    var saveFileDialog = new SaveFileDialog
                    {
                        FileName = $"{report.ReportType}_{report.ReportID}.{report.FileType}",
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
                    string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{report.FileType}");
                    File.WriteAllBytes(tempFilePath, report.FilePath);

                    var viewWindow = new viewingDocument(tempFilePath, report.ReportType);
                    viewWindow.ShowDialog();

                    try { File.Delete(tempFilePath); } catch { }
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