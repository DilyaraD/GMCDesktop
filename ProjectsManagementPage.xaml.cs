using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace GeotekMetallCompleteDesktop
{
    public partial class ProjectsManagementPage : Page
    {
        public Users _user;
        private readonly GeotekMetallCompleteEntities1 _db;
        private List<Projects> _projects;
        private List<ProjectViewModel> _projectViewModels;
        private Projects _selectedProject;

        public ProjectsManagementPage(Users user, Projects selectedProject = null)
        {   
            try
            {
                _db = new GeotekMetallCompleteEntities1();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к БД: {ex.Message}");
                return;
            }
            InitializeComponent();
            
            _user = user;
            _selectedProject = selectedProject;

            StageStartDatePicker.DisplayDateStart = DateTime.Today.AddDays(1);
            StageEndDatePicker.DisplayDateStart = DateTime.Today.AddDays(1);
            TaskDueDatePicker.DisplayDateStart = DateTime.Today.AddDays(1);

            if (_selectedProject != null)
            {
                FilterStackPanel.Visibility = Visibility.Collapsed;
                ProjectsDataGrid.Visibility = Visibility.Collapsed;
                ProjectsDataGrid2.Visibility = Visibility.Collapsed;
                BackButton.Visibility = Visibility.Collapsed;

                ShowProjectDetails();
            }
            else
            {
                SortByDeadlineComboBox.SelectedIndex = 0;
                SearchTextBox.GotFocus += General.RemoveText;
                SearchTextBox.LostFocus += SearchTextBox_LostFocus;
                BackToUserButton.Visibility = Visibility.Collapsed;
                LoadProjects();
            }
        }

        private void LoadProjects()
        {
            if (_db == null)
            {
                MessageBox.Show("Ошибка: подключение к БД не инициализировано");
                return;
            }

            try
            {
                _projects = _db.Projects.Include("Requests")
                                       .Include("ProjectStages")
                                       .Include("ProjectStages.Tasks")
                                       .ToList();

                _projectViewModels = _projects
                    .Select(p => new ProjectViewModel
                    {
                        ProjectID = p.ProjectID,
                        RequestID = p.RequestID,
                        ProjectStartDate = p.ProjectStartDate,
                        ProjectEndDate = p.ProjectEndDate,
                        ProjectManagerID = p.ProjectManagerID,
                        WorkName = p.Requests.WorkTypes?.WorkTypeName ?? "Не найдено",
                        StageCount = p.ProjectStages?.Count ?? 0,
                        Status = GetProjectStatus(p) // Определяем статус проекта
                    }).ToList();

                ProjectsDataGrid.ItemsSource = _projectViewModels;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки проектов: {ex.Message}");
                _projectViewModels = new List<ProjectViewModel>();
            }
        }

        // Метод для определения статуса проекта
        private string GetProjectStatus(Projects project)
        {
            if (project.ProjectStages == null || !project.ProjectStages.Any())
                return "Новый";

            // Проверяем, все ли этапы и задачи отменены
            bool allCancelled = project.ProjectStages.All(s => s.StatusID == 5) &&
                               project.ProjectStages.SelectMany(s => s.Tasks)
                                                    .All(t => t.StatusID == 4);
            if (allCancelled)
                return "Закрыт";

            // Проверяем, все ли этапы и задачи завершены
            bool allCompleted = project.ProjectStages.All(s => s.StatusID == 7) &&
                               project.ProjectStages.SelectMany(s => s.Tasks)
                                                    .All(t => t.StatusID == 3);
            if (allCompleted)
                return "Завершен";

            // Проверяем, есть ли хотя бы один этап или задача в работе
            bool anyInProgress = project.ProjectStages.Any(s => s.StatusID == 4 || s.StatusID == 6) ||
                                project.ProjectStages.SelectMany(s => s.Tasks)
                                                     .Any(t => t.StatusID == 2);
            if (anyInProgress)
                return "В работе";

            // Если ни одна из проверок не сработала
            return "Не определен";
        }

        private void ApplyFiltersAndSort()
        {

            if (_projectViewModels == null)
            {
                return;
            }

           var searchText = SearchTextBox.Text.ToLower();
            var selectedSort = (SortByDeadlineComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            IEnumerable<ProjectViewModel> filteredProjects;
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text) || SearchTextBox.Text == "Поиск по названию работы")
            {
                filteredProjects = _projectViewModels.ToList();
            }
            else
            {
                filteredProjects = _projectViewModels
                    .Where(p => p.WorkName?.ToLower()?.Contains(searchText) ?? false)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(selectedSort))
            {
                switch (selectedSort)
                {
                    case "Ближайшие":
                        filteredProjects = filteredProjects.OrderBy(p => p.ProjectStartDate).ToList();
                        Console.WriteLine("Отсортировано по дате (возрастание)");
                        break;
                    case "Убывание":
                        filteredProjects = filteredProjects.OrderByDescending(p => p.ProjectStartDate).ToList();
                        Console.WriteLine("Отсортировано по дате (убывание)");
                        break;
                    case "Сортировка по дате":
                        filteredProjects = filteredProjects.OrderBy(p => p.ProjectID).ToList();
                        Console.WriteLine("Отсортировано по ProjectID");
                        break;
                }
            }

            ProjectsDataGrid.ItemsSource = filteredProjects;
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            General.AddText(sender, e);

            if (string.IsNullOrWhiteSpace(SearchTextBox.Text) || SearchTextBox.Text == "Поиск по названию работы")
            {
                ProjectsDataGrid.ItemsSource = _projectViewModels;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFiltersAndSort();
        }

        private void SortByDeadlineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortByDeadlineComboBox?.SelectedItem != null)
            {
                ApplyFiltersAndSort();
            }
        }

        private void SearchToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Visibility = Visibility.Visible;
            SearchToggleButton.Content = "Скрыть";
        }

        private void SearchToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Visibility = Visibility.Collapsed;
            SearchToggleButton.Content = "Поиск";
            SearchTextBox.Text = "Поиск по названию работы";
            SearchTextBox.Foreground = Brushes.Gray;
            ProjectsDataGrid.ItemsSource = _projectViewModels;
        }

        private void ProjectsDataGrid_SelectionChanged(object sender, MouseButtonEventArgs e)
        {
            var selectedProjectViewModel = ProjectsDataGrid.SelectedItem as ProjectViewModel;
            if (selectedProjectViewModel != null)
            {
                _selectedProject = _projects.FirstOrDefault(p => p.ProjectID == selectedProjectViewModel.ProjectID);
                if (_selectedProject != null)
                {
                    ShowProjectDetails();
                }
            }
        }

        private void ShowProjectDetails()
        {
            FilterStackPanel.Visibility = Visibility.Collapsed;
            ProjectsDataGrid.Visibility = Visibility.Collapsed;
            ProjectsDataGrid2.Visibility = Visibility.Collapsed;
            ProjectDetailsStackPanel.Visibility = Visibility.Visible;

            var requestDetails = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Название работы", _selectedProject.Requests.ObjectName),
                new KeyValuePair<string, string>("Адрес", _selectedProject.Requests.Address),
                new KeyValuePair<string, string>("Дата начала", _selectedProject.ProjectStartDate?.ToString("dd.MM.yyyy")),
                new KeyValuePair<string, string>("Дата окончания", _selectedProject.ProjectEndDate?.ToString("dd.MM.yyyy")),
                new KeyValuePair<string, string>("Описание", _selectedProject.Requests.Description),
                new KeyValuePair<string, string>("Количество этапов", _selectedProject.ProjectStages.Count.ToString())
            };

            RequestDetailsItemsControl.ItemsSource = requestDetails;

            var acts = _selectedProject.ActsOfWork.ToList();
            ActsOfWorkItemsControl.ItemsSource = acts;
            ActsOfWorkTitle.Visibility = acts.Any() ? Visibility.Visible : Visibility.Collapsed;
            ActsOfWorkItemsControl.Visibility = acts.Any() ? Visibility.Visible : Visibility.Collapsed;

            var contracts = _selectedProject.Contracts.ToList();
            ContractsItemsControl.ItemsSource = contracts;
            ContractsTitle.Visibility = contracts.Any() ? Visibility.Visible : Visibility.Collapsed;
            ContractsItemsControl.Visibility = contracts.Any() ? Visibility.Visible : Visibility.Collapsed;

            ShowMainProjectDetails();
            LoadProjectStages();
            UpdateButtonsVisibility();
        }


        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaskDetailsPanel.Visibility == Visibility.Visible)
            {
                TaskDetailsPanel.Visibility = Visibility.Collapsed;
                TasksItemsControl.Visibility = Visibility.Visible;
                StageDetailsTitle.Visibility = Visibility.Visible;
                AddTaskButton.Visibility = Visibility.Visible;
            }
            else if (StageDetailsStackPanel.Visibility == Visibility.Visible)
            {
                StageDetailsStackPanel.Visibility = Visibility.Collapsed;
                StageButtonsStackPanel.Visibility = Visibility.Visible;
                ShowMainProjectDetails();
            }
            else if (AddStagePanel.Visibility == Visibility.Visible)
            {
                AddStagePanel.Visibility = Visibility.Collapsed;
                ShowMainProjectDetails();
            }
            else
            {
                FilterStackPanel.Visibility = Visibility.Visible;
                ProjectsDataGrid.Visibility = Visibility.Visible;
                ProjectsDataGrid2.Visibility = Visibility.Visible;
                ProjectDetailsStackPanel.Visibility = Visibility.Collapsed;
                LoadProjects();
            }
            //UpdateButtonsVisibility();
        }

        private void ShowMainProjectDetails()
        {
            ProjectDetailsTitle.Visibility = Visibility.Visible;
            RequestDetailsItemsControl.Visibility = Visibility.Visible;
            ActsOfWorkTitle.Visibility = Visibility.Visible;
            ActsOfWorkItemsControl.Visibility = Visibility.Visible;
            ContractsTitle.Visibility = Visibility.Visible;
            ContractsItemsControl.Visibility = Visibility.Visible;
            StageButtonsStackPanel.Visibility = Visibility.Visible;
            AddStageButton.Visibility = Visibility.Visible;
            CancelProjectButton.Visibility = Visibility.Visible;
            BackButton.Visibility = Visibility.Visible;
        }

        private void StageButton_Click(object sender, RoutedEventArgs e)
        {
            var stage = (sender as Button)?.Tag as ProjectStages;
            if (stage != null)
            {
                StageDetailsStackPanel.Tag = stage;

                ProjectDetailsTitle.Visibility = Visibility.Collapsed;
                RequestDetailsItemsControl.Visibility = Visibility.Collapsed;
                ActsOfWorkTitle.Visibility = Visibility.Collapsed;
                ActsOfWorkItemsControl.Visibility = Visibility.Collapsed;
                ContractsTitle.Visibility = Visibility.Collapsed;
                ContractsItemsControl.Visibility = Visibility.Collapsed;
                StageButtonsStackPanel.Visibility = Visibility.Collapsed;
                AddStageButton.Visibility = Visibility.Collapsed;
                CancelProjectButton.Visibility = Visibility.Collapsed;

                StageDetailsStackPanel.Visibility = Visibility.Visible;
                StageDetailsTitle.Visibility = Visibility.Visible;
                AddTaskButton.Visibility = Visibility.Visible;
                TasksTitle.Visibility = Visibility.Visible;
                TasksItemsControl.Visibility = Visibility.Visible;
                CompleteStageButton.Visibility = Visibility.Visible;
                CancelStageButton.Visibility = Visibility.Visible;

                StageNameTextBlock.Text = $"Название: {stage.StageName}";
                StageStartDateTextBlock.Text = $"Дата начала: {stage.StartDate?.ToString("dd.MM.yyyy")}";
                StageEndDateTextBlock.Text = $"Дата окончания: {stage.EndDate?.ToString("dd.MM.yyyy")}";
                StageStatusTextBlock.Text = $"Статус: {stage.Statuses?.StatusName}";

                LoadTasks(stage);
            }

            UpdateButtonsVisibility();
        }

        private void AddStageButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectDATETextBlock.Text = $"Даты проекта: {_selectedProject.ProjectStartDate?.ToString("dd.MM.yyyy")} - {_selectedProject.ProjectEndDate?.ToString("dd.MM.yyyy")}";
            var stagesText = "Этапы:\n";
            int stageNumber = 1;

            foreach (var stage in _selectedProject.ProjectStages.OrderBy(s => s.StartDate))
            {
                stagesText += $"{stage.StageName} с {stage.StartDate?.ToString("dd.MM.yyyy")} до {stage.EndDate?.ToString("dd.MM.yyyy")}\n";
                stageNumber++;
            }
            StagesInfoTextBlock.Text = stagesText;

            ProjectDetailsTitle.Visibility = Visibility.Collapsed;
            RequestDetailsItemsControl.Visibility = Visibility.Collapsed;
            ActsOfWorkTitle.Visibility = Visibility.Collapsed;
            ActsOfWorkItemsControl.Visibility = Visibility.Collapsed;
            ContractsTitle.Visibility = Visibility.Collapsed;
            ContractsItemsControl.Visibility = Visibility.Collapsed;
            StageButtonsStackPanel.Visibility = Visibility.Collapsed;
            AddStageButton.Visibility = Visibility.Collapsed;
            CancelProjectButton.Visibility = Visibility.Collapsed;

            AddStagePanel.Visibility = Visibility.Visible;
            BackButton.Visibility = Visibility.Visible;
            StageDescriptionTextBox.Clear();
            StageStartDatePicker.SelectedDate = null;
            StageEndDatePicker.SelectedDate = null;

            StageStartDatePicker.DisplayDateStart = _selectedProject.ProjectStartDate > DateTime.Today
                ? _selectedProject.ProjectStartDate
                : DateTime.Today.AddDays(1);
            StageStartDatePicker.DisplayDateEnd = _selectedProject.ProjectEndDate;

            StageEndDatePicker.DisplayDateStart = _selectedProject.ProjectStartDate > DateTime.Today
                ? _selectedProject.ProjectStartDate?.AddDays(1)
                : DateTime.Today.AddDays(2);
            StageEndDatePicker.DisplayDateEnd = _selectedProject.ProjectEndDate;

            UpdateButtonsVisibility();
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var stage = StageDetailsStackPanel.Tag as ProjectStages;
            ProjectDATETextBlock.Text = $"Даты проекта: {_selectedProject.ProjectStartDate?.ToString("dd.MM.yyyy")} - {_selectedProject.ProjectEndDate?.ToString("dd.MM.yyyy")}";
            StagesInfoTextBlock2.Text = $"Даты выбранного этапа: {stage.StartDate?.ToString("dd.MM.yyyy")} -  {stage.EndDate?.ToString("dd.MM.yyyy")}";
            
            StageDetailsTitle.Visibility = Visibility.Collapsed;
            StageNameTextBlock.Visibility = Visibility.Collapsed;
            StageStartDateTextBlock.Visibility = Visibility.Collapsed;
            StageEndDateTextBlock.Visibility = Visibility.Collapsed;
            StageStatusTextBlock.Visibility = Visibility.Collapsed;
            TasksTitle.Visibility = Visibility.Collapsed;
            TasksItemsControl.Visibility = Visibility.Collapsed;
            CompleteStageButton.Visibility = Visibility.Collapsed;
            CancelStageButton.Visibility = Visibility.Collapsed;
            AddTaskButton.Visibility = Visibility.Collapsed;

            AddTaskPanel.Visibility = Visibility.Visible;
            BackButton.Visibility = Visibility.Visible;
            TaskDescriptionTextBox.Clear();
            TaskDueDatePicker.SelectedDate = null;

            
            if (stage != null)
            {
                TaskDueDatePicker.DisplayDateStart = stage.StartDate > DateTime.Today
                    ? stage.StartDate?.AddDays(1)
                    : DateTime.Today.AddDays(1);
                TaskDueDatePicker.DisplayDateEnd = stage.EndDate;
            }
            UpdateButtonsVisibility();
        }

        private void DownloadFileButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var fileData = button.Tag as dynamic;
                if (fileData != null && fileData.FilePath != null && fileData.FileType != null)
                {
                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
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


        private void LoadProjectStages()
        {
            StageButtonsStackPanel.Children.Clear();
            foreach (var stage in _selectedProject.ProjectStages)
            {
                var stageButton = new Button
                {
                    Content = $"{stage.StageName}",
                    Tag = stage,
                    Background = Brushes.LightGray,
                    Margin = new Thickness(5, 0, 5, 0)
                };
                stageButton.Click += StageButton_Click;
                StageButtonsStackPanel.Children.Add(stageButton);
            }
        }


        private void LoadTasks(ProjectStages stage)
        {
            TasksItemsControl.ItemsSource = stage.Tasks.ToList();
        }

        private void CancelAddStageButton_Click(object sender, RoutedEventArgs e)
        {
            AddStagePanel.Visibility = Visibility.Collapsed;
            StageButtonsStackPanel.Visibility = Visibility.Visible;
            RequestDetailsItemsControl.Visibility = Visibility.Visible;
            ProjectDetailsTitle.Visibility = Visibility.Visible; 
            ActsOfWorkTitle.Visibility = Visibility.Visible;
            ContractsTitle.Visibility = Visibility.Visible; 
            AddStageButton.Visibility = Visibility.Visible;
            CancelProjectButton.Visibility = Visibility.Visible;
            ActsOfWorkTitle.Visibility = Visibility.Visible;
            ActsOfWorkItemsControl.Visibility = Visibility.Visible;
            ContractsTitle.Visibility = Visibility.Visible;
            ContractsItemsControl.Visibility = Visibility.Visible;
            ProjectDetailsTitle.Visibility = Visibility.Visible;
            UpdateButtonsVisibility();
        }



        private void TaskButton_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button)?.Tag as Tasks;
            if (task != null)
            {
                TasksItemsControl.Visibility = Visibility.Collapsed;
                TasksTitle.Visibility = Visibility.Collapsed;
                AddTaskButton.Visibility = Visibility.Collapsed;
                CompleteStageButton.Visibility = Visibility.Collapsed;
                CancelStageButton.Visibility = Visibility.Collapsed;
                TaskDetailsPanel.Visibility = Visibility.Visible;

                TaskDescriptionText.Text = $"Описание: {task.TaskDescription}";
                TaskDueDateText.Text = $"Срок: {task.DueDate?.ToString("dd.MM.yyyy")}";
                TaskStatusText.Text = $"Статус: {task.Statuses?.StatusName}";

                var taskUsers = _db.TaskUsers.Include("Users").Where(tu => tu.TaskID == task.TaskID).ToList();
                TaskWorkersItemsControl.ItemsSource = taskUsers;
                TaskWorkersTitle.Visibility = taskUsers.Any() ? Visibility.Visible : Visibility.Collapsed;
                TaskWorkersItemsControl.Visibility = taskUsers.Any() ? Visibility.Visible : Visibility.Collapsed;

                var taskReports = _db.TaskReports.Where(tr => tr.TaskID == task.TaskID).ToList();
                TaskReportsItemsControl.ItemsSource = taskReports;
                TaskReportsTitle.Visibility = taskReports.Any() ? Visibility.Visible : Visibility.Collapsed;
                TaskReportsItemsControl.Visibility = taskReports.Any() ? Visibility.Visible : Visibility.Collapsed;

                CompleteTaskButton.Visibility = taskReports.Any() ? Visibility.Visible : Visibility.Collapsed;
                ResetTaskButton.Visibility = taskReports.Any() ? Visibility.Visible : Visibility.Collapsed;
                CancelTaskButton.Visibility = Visibility.Visible;

                CompleteTaskButton.Tag = task;
                ResetTaskButton.Tag = task;
                CancelTaskButton.Tag = task;

                UpdateButtonsVisibility();
            }
        }

        private void CancelAddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            AddTaskPanel.Visibility = Visibility.Collapsed;
            AddTaskButton.Visibility = Visibility.Visible;
            StageDetailsTitle.Visibility = Visibility.Visible;
            StageNameTextBlock.Visibility = Visibility.Visible;
            StageStartDateTextBlock.Visibility = Visibility.Visible;
            StageEndDateTextBlock.Visibility = Visibility.Visible;
            StageStatusTextBlock.Visibility = Visibility.Visible;
            TasksTitle.Visibility = Visibility.Visible; 
            TasksItemsControl.Visibility = Visibility.Visible;
            CompleteStageButton.Visibility = Visibility.Visible;
            CancelStageButton.Visibility = Visibility.Visible;
            ActsOfWorkTitle.Visibility = Visibility.Visible;
            ActsOfWorkItemsControl.Visibility = Visibility.Visible;
            ContractsTitle.Visibility = Visibility.Visible;
            ContractsItemsControl.Visibility = Visibility.Visible;
            ProjectDetailsTitle.Visibility = Visibility.Visible;
            UpdateButtonsVisibility();
        }

        private void SaveTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var stage = StageDetailsStackPanel.Tag as ProjectStages;
            if (stage == null)
            {
                MessageBox.Show("Этап не выбран");
                return;
            }

            if (string.IsNullOrWhiteSpace(TaskDescriptionTextBox.Text))
            {
                MessageBox.Show("Введите описание задачи");
                return;
            }

            if (!TaskDueDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату выполнения");
                return;
            }

            var dueDate = TaskDueDatePicker.SelectedDate.Value;

            if (dueDate <= DateTime.Today)
            {
                MessageBox.Show("Дата выполнения задачи не может быть сегодня или в прошлом");
                return;
            }

            if (dueDate < stage.StartDate || dueDate > stage.EndDate)
            {
                MessageBox.Show("Дата выполнения задачи должна быть в рамках дат этапа");
                return;
            }

            var newTask = new Tasks
            {
                StageID = stage.StageID,
                TaskDescription = TaskDescriptionTextBox.Text,
                DueDate = dueDate,
                StatusID = 1 // Статус "Новый"
            };

            var workers = _db.Users
                .Where(u => u.UserRoles.Any(ur => ur.Roles.RoleName == "Работник"))
                .ToList();

            var selectWorkersWindow = new SelectWorkersWindow(workers);
            if (selectWorkersWindow.ShowDialog() == true)
            {
                try
                {
                    foreach (var worker in selectWorkersWindow.SelectedWorkers)
                    {
                        _db.TaskUsers.Add(new TaskUsers
                        {
                            TaskID = newTask.TaskID,
                            UserID = worker.UserID
                        });
                    }

                    _db.Tasks.Add(newTask);
                    _db.SaveChanges();

                    LoadTasks(stage);
                    AddTaskPanel.Visibility = Visibility.Collapsed;
                    StageDetailsTitle.Visibility = Visibility.Visible;
                    StageNameTextBlock.Visibility = Visibility.Visible;
                    StageStartDateTextBlock.Visibility = Visibility.Visible;
                    StageEndDateTextBlock.Visibility = Visibility.Visible;
                    StageStatusTextBlock.Visibility = Visibility.Visible;
                    TasksTitle.Visibility = Visibility.Visible;
                    TasksItemsControl.Visibility = Visibility.Visible;
                    AddTaskButton.Visibility = Visibility.Visible;

                    TaskDescriptionTextBox.Clear();
                    TaskDueDatePicker.SelectedDate = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении задачи: {ex.Message}");
                }
            }
            UpdateButtonsVisibility();
        }

        private void CompleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var task = (Tasks)((Button)sender).Tag;
            if (task != null)
            {
                task.StatusID = 3; // Статус "Выполнено"
                _db.SaveChanges();
                LoadTasks(task.ProjectStages);
                TaskDetailsPanel.Visibility = Visibility.Collapsed;
            }
            UpdateButtonsVisibility();
        }

        private void ResetTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var task = (Tasks)((Button)sender).Tag;
            if (task != null)
            {
                task.StatusID = 2; // Статус "В работе"
                _db.SaveChanges();
                LoadTasks(task.ProjectStages);
                TaskDetailsPanel.Visibility = Visibility.Collapsed;
            }
            UpdateButtonsVisibility();
        }

        private void CancelTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var task = (Tasks)((Button)sender).Tag;
            if (task != null)
            {
                task.StatusID = 4; // Статус "Отменено"
                _db.SaveChanges();
                LoadTasks(task.ProjectStages);
                TaskDetailsPanel.Visibility = Visibility.Collapsed;
            }
            UpdateButtonsVisibility();
        }

        private void CancelStageButton_Click(object sender, RoutedEventArgs e)
        {
            AddStagePanel.Visibility = Visibility.Collapsed;
            StageDetailsStackPanel.Visibility = Visibility.Visible;
            UpdateButtonsVisibility();
        }

        private void SaveStageButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(StageDescriptionTextBox.Text) &&
                StageStartDatePicker.SelectedDate.HasValue &&
                StageEndDatePicker.SelectedDate.HasValue)
            {
                var startDate = StageStartDatePicker.SelectedDate.Value;
                var endDate = StageEndDatePicker.SelectedDate.Value;

                bool hasOverlap = _selectedProject.ProjectStages.Any(existingStage =>
          (startDate >= existingStage.StartDate && startDate <= existingStage.EndDate) ||
          (endDate >= existingStage.StartDate && endDate <= existingStage.EndDate) ||
          (startDate <= existingStage.StartDate && endDate >= existingStage.EndDate));

                if (hasOverlap)
                {
                    MessageBox.Show("Новый этап пересекается по датам с существующим этапом");
                    return;
                }

                if (startDate >= _selectedProject.ProjectStartDate && endDate <= _selectedProject.ProjectEndDate &&
                    startDate < endDate)
                {
                    var newStage = new ProjectStages
                    {
                        ProjectID = _selectedProject.ProjectID,
                        StageName = $"{_selectedProject.ProjectStages.Count + 1} этап",
                        StageDescription = StageDescriptionTextBox.Text,
                        StartDate = startDate,
                        EndDate = endDate,
                        StatusID = 6 // Статус "Новый"
                    };

                    _db.ProjectStages.Add(newStage);
                    _db.SaveChanges();

                    LoadProjectStages();
                    AddStagePanel.Visibility = Visibility.Collapsed;
                    ShowMainProjectDetails();
                    StageDescriptionTextBox.Clear();
                }
                else
                {
                    MessageBox.Show("Даты этапа должны быть в рамках дат проекта и корректными.");
                }
            }
            UpdateButtonsVisibility();
        }

        private void CancelProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите отменить проект? Все этапы будут отменены.", "Подтверждение", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                foreach (var stage in _selectedProject.ProjectStages)
                {
                    stage.StatusID = 5; // Статус "Отменен"
                    foreach (var task in stage.Tasks)
                    {
                        task.StatusID = 4; // Статус "Отменено"
                    }
                }
                _db.SaveChanges();
                LoadProjectStages();
            }
            UpdateButtonsVisibility();
        }

        private void BackToUserButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
            UpdateButtonsVisibility();
        }

        private void CompleteStageButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedStage = StageDetailsStackPanel.Tag as ProjectStages;
            if (selectedStage != null)
            {
                selectedStage.StatusID = 7; // Статус "Завершен"
                var nextStage = _selectedProject.ProjectStages.FirstOrDefault(s => s.StageID > selectedStage.StageID);
                if (nextStage != null)
                {
                    nextStage.StatusID = 4; // Статус "В работе"
                }
                _db.SaveChanges();
                LoadProjectStages();
            }
            UpdateButtonsVisibility();
        }

        private void UpdateButtonsVisibility()
        {
            if (_selectedProject == null) return;

            // Проверяем статусы всех этапов и задач
            bool allStagesAndTasksCancelled = _selectedProject.ProjectStages.All(s => s.StatusID == 5) &&
                                             _selectedProject.ProjectStages.SelectMany(s => s.Tasks)
                                                                          .All(t => t.StatusID == 4);

            bool allStagesAndTasksCompleted = _selectedProject.ProjectStages.All(s => s.StatusID == 7) &&
                                             _selectedProject.ProjectStages.SelectMany(s => s.Tasks)
                                                                          .All(t => t.StatusID == 3);

            // Проверяем даты проекта
            bool projectDatesOutsideCurrent = _selectedProject.ProjectEndDate < DateTime.Today ||
                                             _selectedProject.ProjectStartDate > DateTime.Today;

            // Проверяем наличие свободных дат для этапа
            bool hasFreeDatesForStage = CheckFreeDatesForNewStage();

            // Обновляем видимость основных кнопок
            AddStageButton.Visibility = !allStagesAndTasksCancelled &&
                                       !allStagesAndTasksCompleted &&
                                       !projectDatesOutsideCurrent &&
                                       hasFreeDatesForStage
                                       ? Visibility.Visible : Visibility.Collapsed;

            CancelProjectButton.Visibility = !allStagesAndTasksCancelled &&
                                            !allStagesAndTasksCompleted
                                            ? Visibility.Visible : Visibility.Collapsed;

            // Для кнопок этапа
            if (StageDetailsStackPanel.Tag is ProjectStages currentStage)
            {
                bool stageDatesOutsideCurrent = currentStage.EndDate < DateTime.Today ||
                                               currentStage.StartDate > DateTime.Today;
                bool stageCompletedOrCancelled = currentStage.StatusID == 7 || currentStage.StatusID == 5;

                AddTaskButton.Visibility = !allStagesAndTasksCancelled &&
                                          !allStagesAndTasksCompleted &&
                                          !projectDatesOutsideCurrent &&
                                          !stageDatesOutsideCurrent &&
                                          !stageCompletedOrCancelled
                                          ? Visibility.Visible : Visibility.Collapsed;

                CompleteStageButton.Visibility = !allStagesAndTasksCancelled &&
                                                !allStagesAndTasksCompleted &&
                                                !stageCompletedOrCancelled &&
                                                currentStage.Tasks.All(t => t.StatusID == 3)
                                                ? Visibility.Visible : Visibility.Collapsed;

                CancelStageButton.Visibility = !allStagesAndTasksCancelled &&
                                              !allStagesAndTasksCompleted &&
                                              !stageCompletedOrCancelled
                                              ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                AddTaskButton.Visibility = Visibility.Collapsed;
                CompleteStageButton.Visibility = Visibility.Collapsed;
                CancelStageButton.Visibility = Visibility.Collapsed;
            }

            // Для кнопок задачи
            if (TaskDetailsPanel.Tag is Tasks currentTask)
            {
                bool taskCompletedOrCancelled = currentTask.StatusID == 3 || currentTask.StatusID == 4;

                CompleteTaskButton.Visibility = !allStagesAndTasksCancelled &&
                                               !allStagesAndTasksCompleted &&
                                               !taskCompletedOrCancelled
                                               ? Visibility.Visible : Visibility.Collapsed;

                ResetTaskButton.Visibility = !allStagesAndTasksCancelled &&
                                            !allStagesAndTasksCompleted &&
                                            !taskCompletedOrCancelled
                                            ? Visibility.Visible : Visibility.Collapsed;

                CancelTaskButton.Visibility = !allStagesAndTasksCancelled &&
                                             !allStagesAndTasksCompleted &&
                                             !taskCompletedOrCancelled
                                             ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private bool CheckFreeDatesForNewStage()
        {
            if (_selectedProject.ProjectStages.Count == 0)
                return true;

            var sortedStages = _selectedProject.ProjectStages.OrderBy(s => s.StartDate).ToList();

            // Проверяем промежуток до первого этапа
            if (_selectedProject.ProjectStartDate < sortedStages[0].StartDate &&
                (sortedStages[0].StartDate - _selectedProject.ProjectStartDate).Value.TotalDays >= 1)
            {
                return true;
            }

            // Проверяем промежутки между этапами
            for (int i = 0; i < sortedStages.Count - 1; i++)
            {
                if ((sortedStages[i + 1].StartDate - sortedStages[i].EndDate).Value.TotalDays >= 1)
                {
                    return true;
                }
            }

            // Проверяем промежуток после последнего этапа
            if (sortedStages.Last().EndDate < _selectedProject.ProjectEndDate &&
                (_selectedProject.ProjectEndDate - sortedStages.Last().EndDate).Value.TotalDays >= 1)
            {
                return true;
            }

            return false;
        }
    }

    public class ProjectViewModel
    {
        public int ProjectID { get; set; }
        public int RequestID { get; set; }
        public Nullable<System.DateTime> ProjectStartDate { get; set; }
        public Nullable<System.DateTime> ProjectEndDate { get; set; }
        public Nullable<int> ProjectManagerID { get; set; }
        public string WorkName { get; set; }
        public int StageCount { get; set; }
        public string Status { get; set; }
    }
}