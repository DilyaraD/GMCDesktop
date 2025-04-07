using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Data.Entity;
using Microsoft.Win32;
using System.IO;
using System.Windows.Data;
using System.Globalization;

namespace GeotekMetallCompleteDesktop
{
    public partial class ProjectsPage : Page
    {
        public Users _user;
        private Projects _selectedProject;
        private Tasks _selectedTask;
        private List<BitmapImage> _selectedPhotos = new List<BitmapImage>();
        private List<Projects> _allProjects = new List<Projects>();

        public ProjectsPage(Users user)
        {
            InitializeComponent();
            _user = user;
            LoadProjects();
        }

        private void LoadProjects()
        {
            try
            {
                using (var context = new GeotekMetallCompleteEntities1())
                {
                    _allProjects = context.Projects
                        .Where(p => p.ProjectManagerID == _user.UserID)
                        .Include(p => p.Requests)
                        .Include(p => p.ProjectStages.Select(ps => ps.Tasks))
                        .Include(p => p.ActsOfWork)
                        .Include(p => p.Contracts)
                        .ToList();

                    ProjectsListView.ItemsSource = _allProjects;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке проектов: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowProjectDetails()
        {
            if (_selectedProject == null) return;

            ProjectsListPanel.Visibility = Visibility.Collapsed;
            ProjectDetailsScrollViewer.Visibility = Visibility.Visible;
            TaskDetailsPanel.Visibility = Visibility.Collapsed;

            ProjectDetailsTitle.Text = $"Детали проекта: {_selectedProject.Requests.ObjectName}";

            using (var context = new GeotekMetallCompleteEntities1())
            {
                _selectedProject = context.Projects
                    .Include(p => p.Requests)
                    .Include(p => p.ProjectStages.Select(ps => ps.Tasks.Select(t => t.Statuses)))
                    .Include(p => p.ActsOfWork)
                    .Include(p => p.Contracts)
                    .FirstOrDefault(p => p.ProjectID == _selectedProject.ProjectID);

                var requestDetails = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Название работы", _selectedProject.Requests.ObjectName),
                    new KeyValuePair<string, string>("Адрес", _selectedProject.Requests.Address),
                    new KeyValuePair<string, string>("Дата начала", _selectedProject.ProjectStartDate?.ToString("dd.MM.yyyy") ?? "Не указана"),
                    new KeyValuePair<string, string>("Дата окончания", _selectedProject.ProjectEndDate?.ToString("dd.MM.yyyy") ?? "Не указана"),
                    new KeyValuePair<string, string>("Описание", _selectedProject.Requests.Description ?? "Нет описания"),
                    new KeyValuePair<string, string>("Количество этапов", _selectedProject.ProjectStages.Count.ToString()),
                    new KeyValuePair<string, string>("Статус проекта", GetProjectStatus(_selectedProject))
                };

                RequestDetailsItemsControl.ItemsSource = requestDetails;

                // Обработка актов
                var acts = _selectedProject.ActsOfWork.ToList();
                ActsOfWorkItemsControl.ItemsSource = acts;
                ActsOfWorkTitle.Visibility = acts.Any() ? Visibility.Visible : Visibility.Collapsed;
                ActsOfWorkItemsControl.Visibility = acts.Any() ? Visibility.Visible : Visibility.Collapsed;
                NoActsText.Visibility = acts.Any() ? Visibility.Collapsed : Visibility.Visible;

                // Обработка договоров
                var contracts = _selectedProject.Contracts.ToList();
                ContractsItemsControl.ItemsSource = contracts;
                ContractsTitle.Visibility = contracts.Any() ? Visibility.Visible : Visibility.Collapsed;
                ContractsItemsControl.Visibility = contracts.Any() ? Visibility.Visible : Visibility.Collapsed;
                NoContractsText.Visibility = contracts.Any() ? Visibility.Collapsed : Visibility.Visible;

                // Обработка задач
                var allTasks = _selectedProject.ProjectStages
                    .SelectMany(ps => ps.Tasks)
                    .OrderBy(t => t.DueDate)
                    .ToList();

                AllTasksListBox.ItemsSource = allTasks;
                TasksTitle.Visibility = allTasks.Any() ? Visibility.Visible : Visibility.Collapsed;
                AllTasksListBox.Visibility = allTasks.Any() ? Visibility.Visible : Visibility.Collapsed;
                NoTasksText.Visibility = allTasks.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void DownloadFileButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                dynamic fileData = button.Tag;
                if (fileData != null && fileData.FilePath != null && fileData.FileType != null)
                {
                    var saveFileDialog = new SaveFileDialog
                    {
                        FileName = $"Документ_{DateTime.Now:yyyyMMddHHmmss}",
                        DefaultExt = fileData.FileType,
                        Filter = $"Файлы {fileData.FileType}|*.{fileData.FileType.ToLower()}"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            File.WriteAllBytes(saveFileDialog.FileName, fileData.FilePath);
                            MessageBox.Show("Файл успешно сохранен!");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}");
                        }
                    }
                }
            }
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectsListView == null || StatusFilterComboBox == null || SortComboBox == null || _allProjects == null)
                return;

            IEnumerable<Projects> filteredProjects = _allProjects;

            if (StatusFilterComboBox.SelectedIndex > 0)
            {
                string selectedStatus = (StatusFilterComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (!string.IsNullOrEmpty(selectedStatus))
                {
                    filteredProjects = filteredProjects.Where(p =>
                    {
                        var status = GetProjectStatus(p);
                        if (selectedStatus == "Отменен" && status == "Закрыт")
                            return true;
                        return status == selectedStatus;
                    });
                }
            }

            if (SortComboBox.SelectedIndex == 1)
            {
                filteredProjects = filteredProjects.OrderBy(p => p.ProjectEndDate);
            }
            else if (SortComboBox.SelectedIndex == 2)
            {
                filteredProjects = filteredProjects.OrderByDescending(p => p.ProjectEndDate);
            }

            ProjectsListView.ItemsSource = filteredProjects.ToList();
            ResetFiltersButton.Visibility = (StatusFilterComboBox.SelectedIndex > 0 || SortComboBox.SelectedIndex > 0)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ProjectStatusTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.DataContext is Projects project)
            {
                textBlock.Text = GetProjectStatus(project);
            }
        }

        private string GetProjectStatus(Projects project)
        {
            // Если нет этапов, статус "Новый"
            if (project.ProjectStages == null || !project.ProjectStages.Any())
                return "Новый";

            // Проверяем, все ли этапы отменены (StatusID = 5) и все ли задачи отменены (StatusID = 5)
            bool allCancelled = project.ProjectStages.All(s => s.StatusID == 5) &&
                               project.ProjectStages.SelectMany(s => s.Tasks)
                                                   .All(t => t.StatusID == 5);
            if (allCancelled)
                return "Отменен";

            // Проверяем, все ли этапы завершены (StatusID = 7) и все ли задачи завершены (3) или отменены (5)
            bool allCompleted = project.ProjectStages.All(s => s.StatusID == 7) &&
                               project.ProjectStages.SelectMany(s => s.Tasks)
                                                   .All(t => t.StatusID == 3 || t.StatusID == 5);
            if (allCompleted)
                return "Завершен";

            // Проверяем задержки: этапы с истекшим сроком, но не завершенные и не отмененные
            bool anyDelayed = project.ProjectStages.Any(s =>
                s.EndDate.HasValue &&
                s.EndDate.Value < DateTime.Now &&
                s.StatusID != 7 &&
                s.StatusID != 5);
            if (anyDelayed)
                return "Задерживается";

            // Проверяем этапы без задач, у которых истек срок
            bool stagesWithoutTasksCompleted = project.ProjectStages
                .Where(s => !s.Tasks.Any())
                .All(s => s.EndDate.HasValue && s.EndDate.Value <= DateTime.Now);
            if (stagesWithoutTasksCompleted)
                return "Завершен";

            // Проверяем, есть ли этапы в работе (StatusID = 4, 6, 8) или задачи в работе (StatusID = 2, 4)
            bool anyInProgress = project.ProjectStages.Any(s => s.StatusID == 4 || s.StatusID == 6 || s.StatusID == 8) ||
                                project.ProjectStages.SelectMany(s => s.Tasks)
                                                    .Any(t => t.StatusID == 2 || t.StatusID == 4);
            if (anyInProgress)
                return "В работе";

            // Если ничего не подошло
            return "Не определен";
        }
        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            StatusFilterComboBox.SelectedIndex = 0;
            SortComboBox.SelectedIndex = 0;
            LoadProjects();
            ResetFiltersButton.Visibility = Visibility.Collapsed;
        }

        private void ProjectsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectsListView.SelectedItem is Projects selectedProject)
            {
                _selectedProject = selectedProject;
                ShowProjectDetails();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectDetailsScrollViewer.Visibility = Visibility.Collapsed;
            ProjectsListPanel.Visibility = Visibility.Visible;
            ProjectsListView.SelectedItem = null;
        }

        private void TasksListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ListBox).SelectedItem is Tasks selectedTask)
            {
                _selectedTask = selectedTask;
                ShowTaskDetails();
            }
        }

        private void ShowTaskDetails()
        {
            try
            {
                using (var context = new GeotekMetallCompleteEntities1())
                {
                    var taskWithDetails = context.Tasks
                        .Include(t => t.Statuses)
                        .Include(t => t.TaskUsers.Select(tu => tu.Users))
                        .Include(t => t.TaskReports.Select(tr => tr.TaskReportFiles))
                        .FirstOrDefault(t => t.TaskID == _selectedTask.TaskID);

                    if (taskWithDetails == null) return;

                    _selectedTask = taskWithDetails;

                    TaskDescriptionText.Text = _selectedTask.TaskDescription;
                    TaskDueDateText.Text = _selectedTask.DueDate?.ToString("dd.MM.yyyy") ?? "Не указан";
                    TaskStatusText.Text = _selectedTask.Statuses?.StatusName ?? "Не определен";

                    var taskUsers = _selectedTask.TaskUsers.ToList();
                    if (taskUsers.Any())
                    {
                        TaskUsersItemsControl.ItemsSource = _selectedTask.TaskUsers.ToList();
                        ResponsibleTitle.Text = "Ответственные:";
                    }
                    else
                    {
                        ResponsibleTitle.Text = $"Задача выполняется ответственным лицом: {_user.LastName} {_user.FirstName}";
                        TaskUsersItemsControl.Visibility = Visibility.Collapsed;
                    }

                    var reports = _selectedTask.TaskReports.ToList();
                    TaskReportsItemsControl.ItemsSource = reports;
                    ReportsTitle.Visibility = reports.Any() ? Visibility.Visible : Visibility.Collapsed;
                    TaskReportsItemsControl.Visibility = reports.Any() ? Visibility.Visible : Visibility.Collapsed;
                    NoReportsText.Visibility = reports.Any() ? Visibility.Collapsed : Visibility.Visible;

                    bool canSendReport = _selectedTask.StatusID == 8 && reports.Count == 0;
                    SendReportButton.Visibility = canSendReport ? Visibility.Visible : Visibility.Collapsed;
                }

                ProjectDetailsScrollViewer.Visibility = Visibility.Collapsed;
                TaskDetailsPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке деталей задачи: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TaskBackButton_Click(object sender, RoutedEventArgs e)
        {
            TaskDetailsPanel.Visibility = Visibility.Collapsed;
            ProjectDetailsScrollViewer.Visibility = Visibility.Visible;
            _selectedTask = null;
            AllTasksListBox.SelectedItem = null;
        }

        private void TaskReportImage_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Image image && image.DataContext is TaskReportFiles file)
            {
                try
                {
                    var bitmapImage = new BitmapImage();
                    using (var ms = new MemoryStream(file.FilePath))
                    {
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = ms;
                        bitmapImage.EndInit();
                    }
                    image.Source = bitmapImage;
                    image.MouseLeftButtonDown += (s, args) => ShowImagePreview(bitmapImage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading image: {ex.Message}");
                }
            }
        }

        private void ShowImagePreview(BitmapImage image)
        {
            PreviewImage.Source = image;
            if (image.Height > 1200)
            {
                PreviewImage.Height = 500;
            }
            else
            {
                PreviewImage.Height = Double.NaN;
            }

            ImagePreviewOverlay.Visibility = Visibility.Visible;
        }

        private void ClosePreviewButton_Click(object sender, RoutedEventArgs e)
        {
            ImagePreviewOverlay.Visibility = Visibility.Collapsed;
            PreviewImage.Source = null;
        }

        private void SendReportButton_Click(object sender, RoutedEventArgs e)
        {
            ReportFormOverlay.Visibility = Visibility.Visible;
            _selectedPhotos.Clear();
            SelectedFilesItemsControl.ItemsSource = null;
            ReportDescriptionTextBox.Text = string.Empty;
        }

        private void AddPhotosButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Image files (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите фотографии"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    if (_selectedPhotos.Count >= 10)
                    {
                        MessageBox.Show("Можно добавить не более 10 фотографий", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                    }

                    try
                    {
                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.UriSource = new Uri(filename);
                        image.EndInit();
                        _selectedPhotos.Add(image);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                SelectedFilesItemsControl.ItemsSource = _selectedPhotos.ToList();
            }
        }

        private void RemovePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is BitmapImage image)
            {
                _selectedPhotos.Remove(image);
                SelectedFilesItemsControl.ItemsSource = _selectedPhotos.ToList();
            }
        }

        private void CancelReportButton_Click(object sender, RoutedEventArgs e)
        {
            ReportFormOverlay.Visibility = Visibility.Collapsed;
        }

        private void SubmitReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ReportDescriptionTextBox.Text))
            {
                MessageBox.Show("Введите описание отчета", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var context = new GeotekMetallCompleteEntities1())
                {
                    var report = new TaskReports
                    {
                        TaskID = _selectedTask.TaskID,
                        UploadedBy = _user.UserID,
                        Description = ReportDescriptionTextBox.Text,
                        UploadDate = DateTime.Now
                    };

                    context.TaskReports.Add(report);
                    context.SaveChanges();

                    foreach (var photo in _selectedPhotos)
                    {
                        byte[] imageData;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(photo));
                            encoder.Save(ms);
                            imageData = ms.ToArray();
                        }

                        var file = new TaskReportFiles
                        {
                            ReportID = report.ReportID,
                            FilePath = imageData,
                            FileType = ".jpg"
                        };

                        context.TaskReportFiles.Add(file);
                    }

                    context.SaveChanges();
                    MessageBox.Show("Отчет успешно отправлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    ReportFormOverlay.Visibility = Visibility.Collapsed;
                    ShowTaskDetails(); 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}