using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GeotekMetallCompleteDesktop
{
    public partial class ProjectsManagementPage : Page
    {
        public Users _user;
        private readonly GeotekMetallCompleteEntities1 _db;
        private List<Projects> _projects;
        private List<ProjectViewModel> _projectViewModels;
        private Projects _selectedProject;
        private ProjectStages _selectedStage;
        private readonly bool _isOpenedFromCustomerPage;

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
            _isOpenedFromCustomerPage = selectedProject != null;
            _user = user;

            StageStartDatePicker.DisplayDateStart = DateTime.Today.AddDays(1);
            StageEndDatePicker.DisplayDateStart = DateTime.Today.AddDays(1);
            TaskDueDatePicker.DisplayDateStart = DateTime.Today.AddDays(1);

            if (selectedProject != null)
            {
                _selectedProject = selectedProject;
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
        private string GetProjectStatus(Projects project)
        {
            if (project.ProjectStages == null || !project.ProjectStages.Any())
            {
                if (project.ProjectStartDate > DateTime.Today)
                    return "В разработке";

                return "Новый";
            }

            bool allCancelled = project.ProjectStages.All(s => s.StatusID == 5) &&
                               project.ProjectStages.SelectMany(s => s.Tasks)
                                                   .All(t => t.StatusID == 5);
            if (allCancelled)
                return "Отменен";

            bool allCompleted = project.ProjectStages.All(s => s.StatusID == 7) &&
                               project.ProjectStages.SelectMany(s => s.Tasks)
                                                   .All(t => t.StatusID == 3 || t.StatusID == 5);
            if (allCompleted)
                return "Завершен";

            bool anyDelayed = project.ProjectStages.Any(s =>
                s.EndDate.HasValue &&
                s.EndDate.Value < DateTime.Now &&
                s.StatusID != 7 &&
                s.StatusID != 5);
            if (anyDelayed)
                return "Задерживается";

            bool stagesWithoutTasksCompleted = project.ProjectStages
                .Where(s => !s.Tasks.Any())
                .All(s => s.EndDate.HasValue && s.EndDate.Value <= DateTime.Now);
            if (stagesWithoutTasksCompleted && project.ProjectStages.All(s => s.StatusID == 7 || s.StatusID == 5))
                return "Завершен";

            bool projectNotStarted = project.ProjectStartDate > DateTime.Today ||
                                   (project.ProjectStages.All(s => s.StartDate > DateTime.Today) &&
                                    project.ProjectStages.All(s => s.StatusID != 4 && s.StatusID != 6 && s.StatusID != 8));
            if (projectNotStarted)
                return "В разработке";

            bool anyInProgress = project.ProjectStages.Any(s => s.StatusID == 4 || s.StatusID == 6 || s.StatusID == 8) ||
                                project.ProjectStages.SelectMany(s => s.Tasks)
                                                    .Any(t => t.StatusID == 2 || t.StatusID == 4);
            if (anyInProgress)
                return "В работе";

            if (project.ProjectEndDate >= DateTime.Today)
                return "В работе";

            if (project.ProjectStages.All(s => s.StatusID == 7))
                return "Завершен";

            return "Не определен";
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
                var stagesWithoutTasksToComplete = _db.ProjectStages
                    .Where(s => !s.Tasks.Any() &&
                           s.EndDate.HasValue &&
                           s.EndDate.Value <= DateTime.Now &&
                           s.StatusID != 7 &&
                           s.StatusID != 5)
                    .ToList();

                foreach (var stage in stagesWithoutTasksToComplete)
                {
                    stage.StatusID = 5;
                }

                var stagesWithTasksToComplete = _db.ProjectStages
                    .Where(s => s.Tasks.Any() &&
                           s.EndDate.HasValue &&
                           s.EndDate.Value <= DateTime.Now &&
                           s.StatusID != 7 &&
                           s.StatusID != 5)
                    .ToList()
                    .Where(s => s.Tasks.All(t => t.StatusID == 7 || t.StatusID == 5))
                    .ToList();

                foreach (var stage in stagesWithTasksToComplete)
                {
                    stage.StatusID = 7;
                }

                var stagesWithIncorrectStatus = _db.ProjectStages
                    .Where(s => (s.StatusID == 7 || s.StatusID == 5) && s.Tasks.Any(t => t.StatusID == 8))
                    .ToList();

                foreach (var stage in stagesWithIncorrectStatus)
                {
                    stage.StatusID = 4;

                    foreach (var task in stage.Tasks.Where(t => t.StatusID == 8))
                    {
                        task.StatusID = 8;
                    }
                }

                if (stagesWithoutTasksToComplete.Any() || stagesWithTasksToComplete.Any() || stagesWithIncorrectStatus.Any())
                {
                    _db.SaveChanges();
                }

                _projects = _db.Projects
                    .Include("Requests")
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
                        Status = GetProjectStatus(p)
                    }).ToList();

                ProjectsDataGrid.ItemsSource = _projectViewModels;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки проектов: {ex.Message}");
                _projectViewModels = new List<ProjectViewModel>();
            }
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
                        break;
                    case "Убывание":
                        filteredProjects = filteredProjects.OrderByDescending(p => p.ProjectStartDate).ToList();
                        break;
                    case "Сортировка по дате":
                        filteredProjects = filteredProjects.OrderBy(p => p.ProjectID).ToList();
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
            ProjectsDataGrid2.Visibility = Visibility.Collapsed;
            ProjectDetailsStackPanel.Visibility = Visibility.Visible;
            AddStagePanel.Visibility = Visibility.Collapsed;
            AddTaskPanel.Visibility = Visibility.Collapsed;
            StageDetailsStackPanel.Visibility = Visibility.Collapsed;
            TaskDetailsPanel.Visibility = Visibility.Collapsed;

            var selProj = _db.Projects.FirstOrDefault(p=>p.ProjectID==_selectedProject.ProjectID);

            var requestDetails = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Название работы", selProj.Requests.ObjectName),
                new KeyValuePair<string, string>("Адрес", selProj.Requests.Address),
                new KeyValuePair<string, string>("Дата начала", selProj.ProjectStartDate?.ToString("dd.MM.yyyy")),
                new KeyValuePair<string, string>("Дата окончания", selProj.ProjectEndDate?.ToString("dd.MM.yyyy")),
                new KeyValuePair<string, string>("Описание", selProj.Requests.Description),
                new KeyValuePair<string, string>("Количество этапов", selProj.ProjectStages.Count.ToString()),
                new KeyValuePair<string, string>("Статус проекта", GetProjectStatus(selProj))
            };

            RequestDetailsItemsControl.ItemsSource = requestDetails;

            var acts = selProj.ActsOfWork.ToList();
            ActsOfWorkItemsControl.ItemsSource = acts;
            ActsOfWorkTitle.Text = acts.Any() ? "Акты выполненных работ" : "Нет прикрепленных актов";
            ActsOfWorkTitle.Visibility = Visibility.Visible;
            ActsOfWorkItemsControl.Visibility = acts.Any() ? Visibility.Visible : Visibility.Collapsed;

            var contracts = selProj.Contracts.ToList();
            ContractsItemsControl.ItemsSource = contracts;
            ContractsTitle.Text = contracts.Any() ? "Договоры" : "Нет прикрепленных договоров";
            ContractsTitle.Visibility = Visibility.Visible;
            ContractsItemsControl.Visibility = contracts.Any() ? Visibility.Visible : Visibility.Collapsed;

            LoadProjectStages();
            UpdateButtonsVisibility();
        }

        private void LoadProjectStages()
        {
            StageButtonsStackPanel.Children.Clear();
            var selProj = _db.Projects.FirstOrDefault(p => p.ProjectID == _selectedProject.ProjectID);
            foreach (var stage in selProj.ProjectStages)
            {
                var stageButton = new Button
                {
                    Content = $"{stage.StageName}",
                    Tag = stage
                };
                stageButton.Click += StageButton_Click;
                StageButtonsStackPanel.Children.Add(stageButton);
            }
        }

        private void StageButton_Click(object sender, RoutedEventArgs e)
        {
            var stage = (sender as Button)?.Tag as ProjectStages;

            if (stage != null) { _selectedStage = stage; } else return;

            if (_selectedStage != null)
            {
                StageDetailsStackPanel.Tag = _selectedStage;
                LoadTasksAndDetails(stage);
            }

            UpdateButtonsVisibility();
        }

        private void LoadTasksAndDetails(ProjectStages stage)
        {
            FilterStackPanel.Visibility = Visibility.Collapsed;
            ProjectsDataGrid2.Visibility = Visibility.Collapsed;
            ProjectDetailsStackPanel.Visibility = Visibility.Collapsed;
            AddStagePanel.Visibility = Visibility.Collapsed;
            AddTaskPanel.Visibility = Visibility.Collapsed;
            StageDetailsStackPanel.Visibility = Visibility.Visible;
            TaskDetailsPanel.Visibility = Visibility.Collapsed;

            var status = _db.Statuses.FirstOrDefault(s => s.StatusID == stage.StatusID);

            var StageDetails = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Название",_selectedStage.StageName),
                new KeyValuePair<string, string>("Дата начала", _selectedStage.StartDate?.ToString("dd.MM.yyyy")),
                new KeyValuePair<string, string>("Дата окончания", _selectedStage.EndDate?.ToString("dd.MM.yyyy")),
                new KeyValuePair<string, string>("Описание", _selectedStage.StageDescription),
                new KeyValuePair<string, string>("Статус проекта", status.StatusName)
            };

            StageDetailsItemsControl.ItemsSource = StageDetails;

            TasksItemsControl.ItemsSource = stage.Tasks.ToList();
            NoTasksText.Visibility = stage.Tasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            TasksItemsControl.Visibility = stage.Tasks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BackFromStageButton_Click(object sender, RoutedEventArgs e)
        {
            FilterStackPanel.Visibility = Visibility.Collapsed;
            ProjectsDataGrid2.Visibility = Visibility.Collapsed;
            ProjectDetailsStackPanel.Visibility = Visibility.Visible;
            AddStagePanel.Visibility = Visibility.Collapsed;
            AddTaskPanel.Visibility = Visibility.Collapsed;
            StageDetailsStackPanel.Visibility = Visibility.Collapsed;
            TaskDetailsPanel.Visibility = Visibility.Collapsed;
        }

        private void TaskButton_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button)?.Tag as Tasks;
            if (task != null)
            {
                TaskDetailsPanel.Tag = task;

                FilterStackPanel.Visibility = Visibility.Collapsed;
                ProjectsDataGrid2.Visibility = Visibility.Collapsed;
                ProjectDetailsStackPanel.Visibility = Visibility.Collapsed;
                AddStagePanel.Visibility = Visibility.Collapsed;
                AddTaskPanel.Visibility = Visibility.Collapsed;
                StageDetailsStackPanel.Visibility = Visibility.Collapsed;
                TaskDetailsPanel.Visibility = Visibility.Visible;
                var status = _db.Statuses.FirstOrDefault(s => s.StatusID == task.StatusID);
                var taskDetails = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Описание задачи", task.TaskDescription),
                    new KeyValuePair<string, string>("Дата готовности",  task.DueDate?.ToString("dd.MM.yyyy") ?? "Не указан"),
                    new KeyValuePair<string, string>("Статус", status.StatusName ?? "Не определен")
                };

                TaskDetailsItemsControl.ItemsSource = taskDetails;

                var taskUsers = _db.TaskUsers.Include("Users").Where(tu => tu.TaskID == task.TaskID).ToList();
                TaskWorkersItemsControl.ItemsSource = taskUsers;
                TaskWorkersTitle.Visibility = taskUsers.Any() ? Visibility.Visible : Visibility.Collapsed;
                TaskWorkersItemsControl.Visibility = taskUsers.Any() ? Visibility.Visible : Visibility.Collapsed;

                LoadTaskReports(task);
                var reports = _db.TaskReports.Where(tr => tr.TaskID == task.TaskID).ToList();
                ResetReportButton.Visibility = (task.StatusID == 8 && reports.Any()) ? Visibility.Visible : Visibility.Collapsed;

                CompleteTaskButton.Visibility = (task.StatusID == 8 && reports.Any()) ? Visibility.Visible : Visibility.Collapsed;
                CancelTaskButton.Visibility = task.StatusID == 8 ? Visibility.Visible : Visibility.Collapsed;
            }

            UpdateButtonsVisibility();
        }

        private void BackFromTaskButton_Click(object sender, RoutedEventArgs e)
        {
            FilterStackPanel.Visibility = Visibility.Collapsed;
            ProjectsDataGrid2.Visibility = Visibility.Collapsed;
            ProjectDetailsStackPanel.Visibility = Visibility.Collapsed;
            AddStagePanel.Visibility = Visibility.Collapsed;
            AddTaskPanel.Visibility = Visibility.Collapsed;
            StageDetailsStackPanel.Visibility = Visibility.Visible;
            NoTasksText.Visibility = TasksItemsControl.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            TaskDetailsPanel.Visibility = Visibility.Collapsed;
        }

        private void AddStageButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectDATETextBlock.Text = $"Даты проекта: {_selectedProject.ProjectStartDate?.ToString("dd.MM.yyyy")} - {_selectedProject.ProjectEndDate?.ToString("dd.MM.yyyy")}";
            if (_selectedProject.ProjectStages.Any())
            {
                var stagesText = "Этапы:\n";
                int stageNumber = 1;

                foreach (var stage in _selectedProject.ProjectStages.OrderBy(s => s.StartDate))
                {
                    stagesText += $"{stage.StageName} с {stage.StartDate?.ToString("dd.MM.yyyy")} до {stage.EndDate?.ToString("dd.MM.yyyy")}\n";
                    stageNumber++;
                }
                StagesInfoTextBlock.Text = stagesText;
            }
            else
            {
                var stagesText = "Этапов нет в рамках этого проекта";
                StagesInfoTextBlock.Text = stagesText;
            }
            FilterStackPanel.Visibility = Visibility.Collapsed;
            ProjectsDataGrid2.Visibility = Visibility.Collapsed;
            ProjectDetailsStackPanel.Visibility = Visibility.Collapsed;
            AddStagePanel.Visibility = Visibility.Visible;
            AddTaskPanel.Visibility = Visibility.Collapsed;
            StageDetailsStackPanel.Visibility = Visibility.Collapsed;
            TaskDetailsPanel.Visibility = Visibility.Collapsed;
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
        private void CancelAddStageButton_Click(object sender, RoutedEventArgs e)
        {
            FilterStackPanel.Visibility = Visibility.Collapsed;
            ProjectsDataGrid2.Visibility = Visibility.Collapsed;
            ProjectDetailsStackPanel.Visibility = Visibility.Visible;
            AddStagePanel.Visibility = Visibility.Collapsed;
            AddTaskPanel.Visibility = Visibility.Collapsed;
            StageDetailsStackPanel.Visibility = Visibility.Collapsed;
            TaskDetailsPanel.Visibility = Visibility.Collapsed;
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
                        StatusID = 4
                    };

                    _db.ProjectStages.Add(newStage);
                    _db.SaveChanges();
                    _selectedProject = _db.Projects
            .Include("ProjectStages")
            .Include("ProjectStages.Tasks")
            .FirstOrDefault(p => p.ProjectID == _selectedProject.ProjectID);
                    ShowProjectDetails();
                    LoadProjectStages();
                    AddStagePanel.Visibility = Visibility.Collapsed;
                    ProjectDetailsStackPanel.Visibility = Visibility.Visible;
                    StageDescriptionTextBox.Clear();
                }
                else
                {
                    MessageBox.Show("Даты этапа должны быть в рамках дат проекта и корректными.");
                }
            }
            else
            {
                MessageBox.Show("Заполните все поля.");
            }
            UpdateButtonsVisibility();
        }
        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var stage = StageDetailsStackPanel.Tag as ProjectStages;
            ProjectDATETextBlock.Text = $"Даты проекта: {_selectedProject.ProjectStartDate?.ToString("dd.MM.yyyy")} - {_selectedProject.ProjectEndDate?.ToString("dd.MM.yyyy")}";
            StagesInfoTextBlock2.Text = $"Даты выбранного этапа: {stage.StartDate?.ToString("dd.MM.yyyy")} -  {stage.EndDate?.ToString("dd.MM.yyyy")}";

            FilterStackPanel.Visibility = Visibility.Collapsed;
            ProjectsDataGrid2.Visibility = Visibility.Collapsed;
            ProjectDetailsStackPanel.Visibility = Visibility.Collapsed;
            AddStagePanel.Visibility = Visibility.Collapsed;
            AddTaskPanel.Visibility = Visibility.Visible;
            StageDetailsStackPanel.Visibility = Visibility.Collapsed;
            TaskDetailsPanel.Visibility = Visibility.Collapsed;

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

        private void CancelAddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            FilterStackPanel.Visibility = Visibility.Collapsed;
            ProjectsDataGrid2.Visibility = Visibility.Collapsed;
            ProjectDetailsStackPanel.Visibility = Visibility.Collapsed;
            AddStagePanel.Visibility = Visibility.Collapsed;
            AddTaskPanel.Visibility = Visibility.Collapsed;
            StageDetailsStackPanel.Visibility = Visibility.Visible;
            TaskDetailsPanel.Visibility = Visibility.Collapsed;
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
                StatusID = 8
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
                    _selectedProject = _db.Projects
            .Include("ProjectStages")
            .Include("ProjectStages.Tasks")
            .FirstOrDefault(p => p.ProjectID == _selectedProject.ProjectID);
                    LoadTasksAndDetails(stage);
                    FilterStackPanel.Visibility = Visibility.Collapsed;
                    ProjectsDataGrid2.Visibility = Visibility.Collapsed;
                    ProjectDetailsStackPanel.Visibility = Visibility.Collapsed;
                    AddStagePanel.Visibility = Visibility.Collapsed;
                    AddTaskPanel.Visibility = Visibility.Collapsed;
                    StageDetailsStackPanel.Visibility = Visibility.Visible;
                    TaskDetailsPanel.Visibility = Visibility.Collapsed;

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

        private void CancelProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите отменить проект? Все этапы и задачи будут отменены.", "Подтверждение", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (var stage in _selectedProject.ProjectStages)
                    {
                        stage.StatusID = 5;
                        foreach (var task in stage.Tasks)
                        {
                            task.StatusID = 5;
                        }
                    }
                    _db.SaveChanges();
                    LoadProjectStages();
                    MessageBox.Show("Проект отменен.");
                    _selectedProject = _db.Projects
            .Include("ProjectStages")
            .Include("ProjectStages.Tasks")
            .FirstOrDefault(p => p.ProjectID == _selectedProject.ProjectID);
                    ShowProjectDetails();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при отмене проекта: {ex.Message}");
                }
            }
        }

        private void CompleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var task = TaskDetailsPanel.Tag as Tasks;
            if (task != null && task.StatusID != 7)
            {
                var result = MessageBox.Show("Завершить задачу?", "Подтверждение",
                                           MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        task.StatusID = 7;

                        var stage = StageDetailsStackPanel.Tag as ProjectStages;
                        if (stage != null)
                        {
                            bool allTasksCompletedOrCancelled = stage.Tasks.All(t =>
                                t.StatusID == 7 || t.StatusID == 5);

                            bool stageEndDatePassed = stage.EndDate.HasValue &&
                                                   stage.EndDate.Value.Date <= DateTime.Today;

                            if (allTasksCompletedOrCancelled && stageEndDatePassed)
                            {
                                stage.StatusID = 7;
                            }

                            _db.SaveChanges();
                            _selectedProject = _db.Projects
            .Include("ProjectStages")
            .Include("ProjectStages.Tasks")
            .FirstOrDefault(p => p.ProjectID == _selectedProject.ProjectID);
                            LoadTasksAndDetails(stage);
                            TaskDetailsPanel.Visibility = Visibility.Collapsed;

                            if (allTasksCompletedOrCancelled && stageEndDatePassed)
                            {
                                MessageBox.Show("Задача завершена. Этап также завершен, так как все задачи завершены.");
                            }
                            else if (allTasksCompletedOrCancelled)
                            {
                                MessageBox.Show("Задача завершена.");
                            }
                            else
                            {
                                MessageBox.Show("Задача завершена.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при завершении задачи: {ex.Message}");
                    }
                }
            }
        }



        private void CancelTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var task = TaskDetailsPanel.Tag as Tasks;
            if (task != null && task.StatusID != 5)
            {
                var result = MessageBox.Show("Отменить задачу?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        task.StatusID = 5;
                        _db.SaveChanges();

                        var stage = StageDetailsStackPanel.Tag as ProjectStages;
                        if (stage != null)
                        {
                            LoadTasksAndDetails(stage);
                        }
                        TaskDetailsPanel.Visibility = Visibility.Collapsed;
                        MessageBox.Show("Задача отменена.");
                        _selectedProject = _db.Projects
            .Include("ProjectStages")
            .Include("ProjectStages.Tasks")
            .FirstOrDefault(p => p.ProjectID == _selectedProject.ProjectID);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при отмене задачи: {ex.Message}");
                    }
                }
            }
        }

        private void UpdateButtonsVisibility()
        {
            if (_selectedProject == null) return;

            bool projectDatesValid = _selectedProject.ProjectEndDate >= DateTime.Today;
            bool hasValidStageStatuses = _selectedProject.ProjectStages.Count == 0 ||
                _selectedProject.ProjectStages.Any(s => s.StatusID == 4 || s.StatusID == 6 || s.StatusID == 7);
            bool hasFreeTimeForNewStage = CheckFreeDatesForNewStage();

            if (!projectDatesValid)
            {
                AddStageButton.Visibility = Visibility.Collapsed;
                CancelProjectButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                AddStageButton.Visibility = projectDatesValid && hasValidStageStatuses && hasFreeTimeForNewStage
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                bool showCancelButton = _selectedProject.ProjectStages.Count == 0 ||
                                       !_selectedProject.ProjectStages.All(s => s.StatusID == 5 || s.StatusID == 7);
                CancelProjectButton.Visibility = showCancelButton
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            if (StageDetailsStackPanel.Visibility == Visibility.Visible &&
                StageDetailsStackPanel.Tag is ProjectStages currentStage)
            {
                bool stageNotStarted = currentStage.StartDate > DateTime.Today;
                bool stageEnded = currentStage.EndDate < DateTime.Today;
                bool stageInProgress = !stageNotStarted && !stageEnded;
                bool stageCompletedOrCancelled = currentStage.StatusID == 7 || currentStage.StatusID == 5;
                bool hasUncompletedTasks = currentStage.Tasks.Any(t => t.StatusID != 3 && t.StatusID != 5);

                AddTaskButton.Visibility = !stageCompletedOrCancelled &&
                                         (stageInProgress || stageNotStarted ||
                                         (stageEnded && hasUncompletedTasks))
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            else
            {
                AddTaskButton.Visibility = Visibility.Collapsed;
            }

            if (StageDetailsStackPanel.Visibility != Visibility.Visible &&
                TaskDetailsPanel.Visibility != Visibility.Visible &&
                AddTaskPanel.Visibility != Visibility.Visible)
            {
                AddStageButton.Visibility = projectDatesValid && hasValidStageStatuses && hasFreeTimeForNewStage
                    ? Visibility.Visible : Visibility.Collapsed;

                bool showCancelButton = _selectedProject.ProjectStages.Count == 0 ||
                              !_selectedProject.ProjectStages.All(s => s.StatusID == 5 || s.StatusID == 7);

                CancelProjectButton.Visibility = showCancelButton
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ProjectsDataGrid2.Visibility = Visibility.Visible;
            ProjectDetailsStackPanel.Visibility = Visibility.Collapsed;
            AddStagePanel.Visibility = Visibility.Collapsed;
            AddTaskPanel.Visibility = Visibility.Collapsed;
            StageDetailsStackPanel.Visibility = Visibility.Collapsed;
            TaskDetailsPanel.Visibility = Visibility.Collapsed;
            FilterStackPanel.Visibility = Visibility.Visible;
            LoadProjects();
        }

        private void ResetReportButton_Click(object sender, RoutedEventArgs e)
        {
            var task = TaskDetailsPanel.Tag as Tasks;
            if (task != null)
            {
                var result = MessageBox.Show("Вы уверены, что хотите сбросить отчет для этой задачи? Все будет удалено.", "Подтверждение сброса",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var reports = _db.TaskReports.Where(tr => tr.TaskID == task.TaskID).ToList();

                        foreach (var report in reports)
                        {
                            foreach (var file in report.TaskReportFiles.ToList())
                            {
                                _db.TaskReportFiles.Remove(file);
                            }

                            _db.TaskReports.Remove(report);
                        }

                        _db.SaveChanges();
                        _selectedProject = _db.Projects
                            .Include("ProjectStages")
                            .Include("ProjectStages.Tasks")
                            .FirstOrDefault(p => p.ProjectID == _selectedProject.ProjectID);
                        LoadTaskReports(task);

                        ResetReportButton.Visibility = Visibility.Collapsed;
                        CompleteTaskButton.Visibility = Visibility.Collapsed;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сбросе отчетов: {ex.Message}");
                    }
                }
            }
        }

        private bool CheckFreeDatesForNewStage()
        {
            if (_selectedProject.ProjectStages.Count == 0)
                return true;

            var sortedStages = _selectedProject.ProjectStages.OrderBy(s => s.StartDate).ToList();

            if (_selectedProject.ProjectStartDate < sortedStages[0].StartDate &&
                (sortedStages[0].StartDate - _selectedProject.ProjectStartDate).Value.TotalDays >= 1)
            {
                return true;
            }

            for (int i = 0; i < sortedStages.Count - 1; i++)
            {
                if ((sortedStages[i + 1].StartDate - sortedStages[i].EndDate).Value.TotalDays >= 1)
                {
                    return true;
                }
            }

            if (sortedStages.Last().EndDate < _selectedProject.ProjectEndDate &&
                (_selectedProject.ProjectEndDate - sortedStages.Last().EndDate).Value.TotalDays >= 1)
            {
                return true;
            }

            return false;
        }

        private void LoadTaskReports(Tasks task)
        {
            try
            {
                var reports = _db.TaskReports.Where(tr => tr.TaskID == task.TaskID).ToList();
                TaskReportsItemsControl.ItemsSource = reports;
                TaskReportsTitle.Visibility = reports.Any() ? Visibility.Visible : Visibility.Collapsed;
                NoReportsText.Visibility = reports.Any() ? Visibility.Collapsed : Visibility.Visible;
                TaskReportsItemsControl.Visibility = reports.Any() ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке отчетов: {ex.Message}");
            }
        }

        private void BackToUserButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();            
            UpdateButtonsVisibility();
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

            Panel.SetZIndex(ImagePreviewOverlay, int.MaxValue);
            ImagePreviewOverlay.Visibility = Visibility.Visible;
        }

        private void ClosePreviewButton_Click(object sender, RoutedEventArgs e)
        {
            ImagePreviewOverlay.Visibility = Visibility.Collapsed;
            PreviewImage.Source = null;
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