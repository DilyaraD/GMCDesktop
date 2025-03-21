using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace GeotekMetallCompleteDesktop
{
    public partial class ActsOfWorkPage : Page
    {
        public Users _user;
        private List<ActsOfWork> _acts;
        private byte[] _selectedFile;
        private string _selectedFileType; // Тип файла (расширение)
        private string _selectedFileName; // Имя выбранного файла
        private Projects _selectedProject;

        private GeotekMetallCompleteEntities1 _db;

        public ActsOfWorkPage(Users user)
        {
            InitializeComponent();
            _user = user;
            _db = new GeotekMetallCompleteEntities1();
            LoadActs();
        }

        private void LoadActs()
        {
            _acts = _db.ActsOfWork.ToList();
            ActsListView.ItemsSource = _acts;
        }

        private void AddActButton_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно выбора проекта
            var projects = _db.Projects.ToList();
            var selectProjectWindow = new SelectProjectWindow(projects);
            if (selectProjectWindow.ShowDialog() == true)
            {
                _selectedProject = selectProjectWindow.SelectedProject;
                SelectedProjectTextBlock.Text = _selectedProject.Requests.ObjectName;
                ActsListView.Visibility = Visibility.Collapsed;
                AddActPanel.Visibility = Visibility.Visible;
            }
        }

        private void SelectProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var projects = _db.Projects.ToList();
            var selectProjectWindow = new SelectProjectWindow(projects);
            if (selectProjectWindow.ShowDialog() == true)
            {
                _selectedProject = selectProjectWindow.SelectedProject;
                var filteredActs = _acts.Where(a => a.ProjectID == _selectedProject.ProjectID).ToList();
                ActsListView.ItemsSource = filteredActs;
            }
        }

        private void ResetProjectButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedProject = null;
            ActsListView.ItemsSource = _acts;
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

        private void DownloadActButton_Click(object sender, RoutedEventArgs e)
        {
            var act = (sender as Button)?.DataContext as ActsOfWork;
            if (act?.FilePath != null)
            {
                var saveFileDialog = new SaveFileDialog
                {
                    FileName = $"Act_{act.ActID}.{act.FileType}",
                    Filter = $"{act.FileType} files (*.{act.FileType})|*.{act.FileType}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllBytes(saveFileDialog.FileName, act.FilePath);
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

        private void SaveActButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFile == null || _selectedProject == null)
            {
                MessageBox.Show("Выберите файл и проект.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newAct = new ActsOfWork
            {
                ProjectID = _selectedProject.ProjectID, 
                ActDate = DateTime.Now,
                FilePath = _selectedFile,
                FileType = _selectedFileType 
            };

            _db.ActsOfWork.Add(newAct);
            _db.SaveChanges();

            LoadActs();
            CancelButton_Click(sender, e);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ActsListView.Visibility = Visibility.Visible;
            AddActPanel.Visibility = Visibility.Collapsed;
            _selectedFile = null;
            _selectedFileType = null;
            _selectedFileName = null;
            _selectedProject = null;
            SelectedProjectTextBlock.Text = string.Empty;
            SelectedFileTextBlock.Text = string.Empty;
        }
    }
}