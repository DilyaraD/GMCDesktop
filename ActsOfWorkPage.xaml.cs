using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace GeotekMetallCompleteDesktop
{
    public partial class ActsOfWorkPage : Page
    {
        private Users _user;
        private GeotekMetallCompleteEntities1 _db;
        private Projects _selectedProject;
        private byte[] _selectedFile;
        private string _selectedFileType;
        private List<ActsOfWork> _acts;

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
            ActsListView.Visibility = Visibility.Collapsed;
            AddActPanel.Visibility = Visibility.Visible;
            SelectBtm.Visibility = Visibility.Collapsed;
        }

        private void CreateNewActButton_Click(object sender, RoutedEventArgs e)
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
                SelectedFileTextBlock.Text = Path.GetFileName(openFileDialog.FileName);
            }
        }

        private void SaveActButton_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show("Акт успешно добавлен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении акта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewActButton_Click(object sender, RoutedEventArgs e)
        {
            var act = (sender as Button)?.DataContext as ActsOfWork;
            if (act?.FilePath != null)
            {
                try
                {
                    var tempFilePath = Path.GetTempFileName() + "." + act.FileType;
                    File.WriteAllBytes(tempFilePath, act.FilePath);

                    var viewer = new viewingDocument(tempFilePath, $"Акт N{act.ActID}");
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

        private void EditActButton_Click(object sender, RoutedEventArgs e)
        {
            var act = (sender as Button)?.DataContext as ActsOfWork;
            if (act?.FilePath != null)
            {
                var editor = new DocumentEditorWindow(
                    documentData: act.FilePath,
                    fileName: $"Акт N{act.ActID}.{act.FileType}",
                    fileType: act.FileType,
                    isNewDocument: false);

                if (editor.ShowDialog() == true)
                {
                    act.FilePath = editor.DocumentData;
                    act.FileType = editor.FileType;
                    _db.SaveChanges();
                    if (_selectedProject != null)
                    {
                        var filteredActs = _acts.Where(c => c.ProjectID == _selectedProject.ProjectID).ToList();
                        ActsListView.ItemsSource = filteredActs;
                    }
                    else
                    {
                        ActsListView.ItemsSource = _acts;
                    }
                }
            }
        }

        private void DownloadActButton_Click(object sender, RoutedEventArgs e)
        {
            var act = (sender as Button)?.DataContext as ActsOfWork;
            if (act?.FilePath != null)
            {
                var saveFileDialog = new SaveFileDialog
                {
                    FileName = $"Акт_N{act.ActID}{act.FileType}",
                    Filter = GetFilterForFileType(act.FileType)
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        File.WriteAllBytes(saveFileDialog.FileName, act.FilePath);
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
            ActsListView.Visibility = Visibility.Visible;
            AddActPanel.Visibility = Visibility.Collapsed;
            SelectBtm.Visibility = Visibility.Visible;
            _selectedFile = null;
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
                var filteredActs = _acts.Where(a => a.ProjectID == _selectedProject.ProjectID).ToList();
                ActsListView.ItemsSource = filteredActs;
                ResetProjectButton.Visibility = Visibility.Visible;
            }
        }

        private void ResetProjectButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedProject = null;
            ActsListView.ItemsSource = _acts;
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