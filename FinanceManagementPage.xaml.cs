using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GeotekMetallCompleteDesktop
{
    public partial class FinanceManagementPage : Page
    {
        public Users _user;
        private GeotekMetallCompleteEntities1 _db;
        private byte[] _FilePath; 

        public FinanceManagementPage(Users user)
        {
            InitializeComponent();
            _user = user;
            _db = new GeotekMetallCompleteEntities1();
            LoadFinancialReports();
        }

        private void LoadFinancialReports()
        {
            FinancialReportsListView.ItemsSource = _db.FinancialReports.Include("Projects").ToList();
        }

        private void AddReportButton_Click(object sender, RoutedEventArgs e)
        {
            FinancialReportsListView.Visibility = Visibility.Collapsed;
            AddReportStackPanel.Visibility = Visibility.Visible;
        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                _FilePath = System.IO.File.ReadAllBytes(openFileDialog.FileName);
                SelectedFilePathTextBlock.Text = System.IO.Path.GetFileName(openFileDialog.FileName);
            }
        }

        private void SaveReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ReportTypeTextBox.Text))
            {
                MessageBox.Show("Введите название отчета.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_FilePath == null || _FilePath.Length == 0)
            {
                MessageBox.Show("Выберите файл отчета.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectProjectWindow = new SelectProjectWindow(_db.Projects.ToList());
            if (selectProjectWindow.ShowDialog() == true)
            {
                var selectedProject = selectProjectWindow.SelectedProject;
                if (selectedProject != null)
                {
                    var newReport = new FinancialReports
                    {
                        ProjectID = selectedProject.ProjectID,
                        ReportType = ReportTypeTextBox.Text,
                        ReportDate = DateTime.Now,
                        FilePath = _FilePath
                    };

                    _db.FinancialReports.Add(newReport);
                    _db.SaveChanges();

                    LoadFinancialReports();

                    BackButton_Click(null, null);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ReportTypeTextBox.Text = string.Empty;
            _FilePath = null;
            SelectedFilePathTextBlock.Text = string.Empty;

            FinancialReportsListView.Visibility = Visibility.Visible;
            AddReportStackPanel.Visibility = Visibility.Collapsed;
        }

        private void DownloadReportButton_Click(object sender, RoutedEventArgs e)
        {
            var report = (sender as Button)?.Tag as FinancialReports;
            if (report != null && report.FilePath != null && report.FilePath.Length > 0)
            {
                try
                {
                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = "report",
                        Filter = "All Files (*.*)|*.*"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        System.IO.File.WriteAllBytes(saveFileDialog.FileName, report.FilePath);
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
    }
}