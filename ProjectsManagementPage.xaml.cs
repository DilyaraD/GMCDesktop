using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;

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
        private string GetProjectStatus(Projects project) //получение статуса проекта
        {
            if (project.ProjectStages == null || !project.ProjectStages.Any())
                return "Новый";

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
            if (stagesWithoutTasksCompleted)
                return "Завершен";

            bool anyInProgress = project.ProjectStages.Any(s => s.StatusID == 4 || s.StatusID == 6 || s.StatusID == 8) ||
                                project.ProjectStages.SelectMany(s => s.Tasks)
                                                    .Any(t => t.StatusID == 2 || t.StatusID == 4);
            if (anyInProgress)
                return "В работе";

            return "Не определен";
        }

        private void LoadProjects() //загрузка всех проектов в список
        {
            if (_db == null)
            {
                MessageBox.Show("Ошибка: подключение к БД не инициализировано");
                return;
            }

            try
            {
                var stagesToComplete = _db.ProjectStages
                    .Where(s => !s.Tasks.Any() &&
                           s.EndDate.HasValue &&
                           s.EndDate.Value <= DateTime.Now &&
                           s.StatusID != 7 &&
                           s.StatusID != 5)
                    .ToList();

                foreach (var stage in stagesToComplete)
                {
                    stage.StatusID = 7;
                }

                if (stagesToComplete.Any())
                {
                    _db.SaveChanges();
                }

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

        private void ApplyFiltersAndSort() //объединение фильтрации
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

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e) // фильтр по поиску
        {
            General.AddText(sender, e);

            if (string.IsNullOrWhiteSpace(SearchTextBox.Text) || SearchTextBox.Text == "Поиск по названию работы")
            {
                ProjectsDataGrid.ItemsSource = _projectViewModels;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) // изменение списка по поиску
        {
            ApplyFiltersAndSort();
        }

        private void SortByDeadlineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) //фильтр по дедлайну
        {
            if (SortByDeadlineComboBox?.SelectedItem != null)
            {
                ApplyFiltersAndSort();
            }
        }

        private void SearchToggleButton_Checked(object sender, RoutedEventArgs e) //вывод поля для ввода поиска
        {
            SearchTextBox.Visibility = Visibility.Visible;
            SearchToggleButton.Content = "Скрыть";
        }

        private void SearchToggleButton_Unchecked(object sender, RoutedEventArgs e) //закрытие поля для ввода поиска
        {
            SearchTextBox.Visibility = Visibility.Collapsed;
            SearchToggleButton.Content = "Поиск";
            SearchTextBox.Text = "Поиск по названию работы";
            SearchTextBox.Foreground = Brushes.Gray;
            ProjectsDataGrid.ItemsSource = _projectViewModels;
        }

        private void ProjectsDataGrid_SelectionChanged(object sender, MouseButtonEventArgs e) //вывод деталей проекта
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

        private void ShowProjectDetails() //вывод данных проекта
        {
            FilterStackPanel.Visibility = Visibility.Collapsed;
            ProjectsDataGrid.Visibility = Visibility.Collapsed;
            ProjectsDataGrid2.Visibility = Visibility.Collapsed;
            ProjectDetailsStackPanel.Visibility = Visibility.Visible;
            RequestDetailsItemsControl.Visibility = Visibility.Visible;
            ActsOfWorkTitle.Visibility = Visibility.Visible;
            ActsOfWorkItemsControl.Visibility = Visibility.Visible;
            ContractsTitle.Visibility = Visibility.Visible;
            ContractsItemsControl.Visibility = Visibility.Visible;
            ProjectDetailsTitle.Visibility = Visibility.Visible;
            BackButton.Visibility = Visibility.Visible;
            AddStageButton.Visibility = Visibility.Visible;
            CancelProjectButton.Visibility = Visibility.Visible;
            MainButtonsPanel.Visibility = Visibility.Visible;

            var requestDetails = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Название работы", _selectedProject.Requests.ObjectName),
                new KeyValuePair<string, string>("Адрес", _selectedProject.Requests.Address),
                new KeyValuePair<string, string>("Дата начала", _selectedProject.ProjectStartDate?.ToString("dd.MM.yyyy")),
                new KeyValuePair<string, string>("Дата окончания", _selectedProject.ProjectEndDate?.ToString("dd.MM.yyyy")),
                new KeyValuePair<string, string>("Описание", _selectedProject.Requests.Description),
                new KeyValuePair<string, string>("Количество этапов", _selectedProject.ProjectStages.Count.ToString()),
                new KeyValuePair<string, string>("Статус проекта", GetProjectStatus(_selectedProject))
            };

            RequestDetailsItemsControl.ItemsSource = requestDetails;

            var acts = _selectedProject.ActsOfWork.ToList();
            ActsOfWorkItemsControl.ItemsSource = acts;
            ActsOfWorkTitle.Text = acts.Any() ? "Акты выполненных работ" : "Нет прикрепленных актов";
            ActsOfWorkTitle.Visibility = Visibility.Visible;
            ActsOfWorkItemsControl.Visibility = acts.Any() ? Visibility.Visible : Visibility.Collapsed;

            var contracts = _selectedProject.Contracts.ToList();
            ContractsItemsControl.ItemsSource = contracts;
            ContractsTitle.Text = contracts.Any() ? "Договоры" : "Нет прикрепленных договоров";
            ContractsTitle.Visibility = Visibility.Visible;
            ContractsItemsControl.Visibility = contracts.Any() ? Visibility.Visible : Visibility.Collapsed;

            ShowMainProjectDetails();
            LoadProjectStages();
            UpdateButtonsVisibility();
        }

        private void ShowMainProjectDetails() //вывод подробностей проекта
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
            MainButtonsPanel.Visibility = Visibility.Visible;
            BackButton.Visibility = Visibility.Visible;
        }

        private void LoadProjectStages() //вывод всех этапов
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
        private void UpdateButtonsVisibility() // Обновление видимости кнопок в зависимости от текущего контекста
        {
            if (_selectedProject == null) return; // Если проект не выбран, выходим из метода

            // --- Случай 1: Просмотр деталей этапа ---
            if (StageDetailsStackPanel.Visibility == Visibility.Visible)
            {
                // В этом режиме:
                AddTaskButton.Visibility = Visibility.Visible; // Кнопка добавления задачи видна
                CancelProjectButton.Visibility = Visibility.Collapsed; // Кнопка отмены проекта скрыта
                CompleteStageButton.Visibility = Visibility.Collapsed; // Кнопка завершения этапа скрыта
            }

            // --- Случай 2: Просмотр деталей задачи ---
            if (TaskDetailsPanel.Visibility == Visibility.Visible)
            {
                // В этом режиме:
                AddStageButton.Visibility = Visibility.Collapsed; // Кнопка добавления этапа скрыта
                CancelProjectButton.Visibility = Visibility.Collapsed; // Кнопка отмены проекта скрыта
                MainButtonsPanel.Visibility = Visibility.Collapsed; // Основная панель кнопок скрыта

                // Скрываем разделы с актами и договорами
                ActsOfWorkTitle.Visibility = Visibility.Collapsed;
                ActsOfWorkItemsControl.Visibility = Visibility.Collapsed;
                ContractsTitle.Visibility = Visibility.Collapsed;
                ContractsItemsControl.Visibility = Visibility.Collapsed;
            }

            // --- Случай 3: Форма добавления задачи ---
            if (AddTaskPanel.Visibility == Visibility.Visible)
            {
                // В этом режиме скрываем все кнопки навигации:
                BackButton.Visibility = Visibility.Collapsed;
                CancelStageButton.Visibility = Visibility.Collapsed;
                AddStageButton.Visibility = Visibility.Collapsed;
                CancelProjectButton.Visibility = Visibility.Collapsed;
                AddTaskButton.Visibility = Visibility.Collapsed;
                MainButtonsPanel.Visibility = Visibility.Collapsed;
            }

            // Проверяем условия для отображения кнопок добавления этапа и отмены проекта
            bool projectDatesValid = _selectedProject.ProjectEndDate >= DateTime.Today;
            bool hasValidStageStatuses = _selectedProject.ProjectStages.Count == 0 ||
                _selectedProject.ProjectStages.Any(s => s.StatusID == 4 || s.StatusID == 6 || s.StatusID == 7);
            bool hasFreeTimeForNewStage = CheckFreeDatesForNewStage();

            // --- Режим просмотра деталей этапа ---
            if (StageDetailsStackPanel.Visibility == Visibility.Visible &&
                StageDetailsStackPanel.Tag is ProjectStages currentStage)
            {
                bool stageNotStarted = currentStage.StartDate > DateTime.Today;
                bool stageEnded = currentStage.EndDate < DateTime.Today;
                bool stageInProgress = !stageNotStarted && !stageEnded;
                bool stageCompletedOrCancelled = currentStage.StatusID == 7 || currentStage.StatusID == 5;
                bool hasUncompletedTasks = currentStage.Tasks.Any(t => t.StatusID != 3 && t.StatusID != 5);

                // Кнопка "Добавить задачу" видна когда:
                // 1. Этап не завершен/не отменен
                // 2. И выполняется одно из:
                //    - Этап в процессе
                //    - Этап еще не начался (можно заранее создать задачи)
                //    - Этап закончился, но есть незавершенные задачи
                AddTaskButton.Visibility = !stageCompletedOrCancelled &&
                                         (stageInProgress || stageNotStarted ||
                                         (stageEnded && hasUncompletedTasks))
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                // Кнопка "Завершить этап" видна когда:
                // - Все задачи завершены или отменены
                // - И этап не завершен/не отменен
                CompleteStageButton.Visibility = !stageCompletedOrCancelled &&
                                               currentStage.Tasks.All(t => t.StatusID == 3 || t.StatusID == 5)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                // Кнопка "Отменить этап" видна всегда, кроме завершенных/отмененных этапов
                CancelStageButton.Visibility = !stageCompletedOrCancelled
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }


            // --- Основной режим (не детали задачи/этапа, не формы добавления) ---
            if (StageDetailsStackPanel.Visibility != Visibility.Visible &&
                TaskDetailsPanel.Visibility != Visibility.Visible &&
                AddTaskPanel.Visibility != Visibility.Visible)
            {
                // Кнопка добавления этапа видна только если:
                // - даты проекта валидны
                // - статусы этапов позволяют добавить новый
                // - есть свободное время для нового этапа
                AddStageButton.Visibility = projectDatesValid && hasValidStageStatuses && hasFreeTimeForNewStage
                    ? Visibility.Visible : Visibility.Collapsed;

                // Кнопка отмены проекта видна только если:
                // - нет этапов ИЛИ
                // - не все этапы отменены или завершены
                bool showCancelButton = _selectedProject.ProjectStages.Count == 0 ||
                              !_selectedProject.ProjectStages.All(s => s.StatusID == 5 || s.StatusID == 7);

                CancelProjectButton.Visibility = showCancelButton
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }





































        private void LoadTasks(ProjectStages stage)
        {
            TasksItemsControl.ItemsSource = stage.Tasks.ToList();
            NoTasksText.Visibility = stage.Tasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            TasksItemsControl.Visibility = stage.Tasks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CancelProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите отменить проект? Все этапы и задачи будут отменены.",
                                       "Подтверждение",
                                       MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (var stage in _selectedProject.ProjectStages)
                    {
                        stage.StatusID = 5; // Статус "Отменен" для этапа
                        foreach (var task in stage.Tasks)
                        {
                            task.StatusID = 5; // Статус "Отменено" для задачи
                        }
                    }
                    _db.SaveChanges();
                    LoadProjectStages();
                    MessageBox.Show("Проект и все его этапы/задачи отменены.");
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
                        task.StatusID = 7; // Статус "Завершен"

                        var stage = StageDetailsStackPanel.Tag as ProjectStages;
                        if (stage != null)
                        {
                            bool allTasksCompletedOrCancelled = stage.Tasks.All(t =>
                                t.StatusID == 7 || t.StatusID == 5);

                            if (allTasksCompletedOrCancelled)
                            {
                                stage.StatusID = 7; // Автоматически завершаем этап
                            }

                            _db.SaveChanges();
                            LoadTasks(stage);
                            TaskDetailsPanel.Visibility = Visibility.Collapsed;

                            if (allTasksCompletedOrCancelled)
                            {
                                MessageBox.Show("Задача завершена. Этап автоматически завершен, так как все задачи завершены или отменены.");
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
                var result = MessageBox.Show("Отменить задачу?", "Подтверждение",
                                           MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        task.StatusID = 5; // Статус "Отменено"
                        _db.SaveChanges();

                        var stage = StageDetailsStackPanel.Tag as ProjectStages;
                        if (stage != null)
                        {
                            LoadTasks(stage);
                        }
                        TaskDetailsPanel.Visibility = Visibility.Collapsed;
                        MessageBox.Show("Задача отменена.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при отмене задачи: {ex.Message}");
                    }
                }
            }
        }


        private void TaskButton_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button)?.Tag as Tasks;
            if (task != null)
            {
                TaskDetailsPanel.Tag = task;

                StageDetailsTitle.Visibility = Visibility.Collapsed;
                StageNameTextBlock.Visibility = Visibility.Collapsed;
                StageStartDateTextBlock.Visibility = Visibility.Collapsed;
                StageEndDateTextBlock.Visibility = Visibility.Collapsed;
                StageStatusTextBlock.Visibility = Visibility.Collapsed;
                TasksItemsControl.Visibility = Visibility.Collapsed;
                TasksTitle.Visibility = Visibility.Collapsed;
                AddTaskButton.Visibility = Visibility.Collapsed;
                CompleteStageButton.Visibility = Visibility.Collapsed;
                CancelStageButton.Visibility = Visibility.Collapsed;
                TaskDetailsPanel.Visibility = Visibility.Visible;

                TaskDescriptionText.Text = "Описание задачи: " + task.TaskDescription;
                TaskDueDateText.Text = "Дата готовности: " + task.DueDate?.ToString("dd.MM.yyyy HH:mm") ?? "Не указан";
                TaskStatusText.Text = "Статус: " + task.Statuses?.StatusName ?? "Не определен";

                var taskUsers = _db.TaskUsers.Include("Users").Where(tu => tu.TaskID == task.TaskID).ToList();
                TaskWorkersItemsControl.ItemsSource = taskUsers;
                TaskWorkersTitle.Visibility = taskUsers.Any() ? Visibility.Visible : Visibility.Collapsed;
                TaskWorkersItemsControl.Visibility = taskUsers.Any() ? Visibility.Visible : Visibility.Collapsed;

                LoadTaskReports(task);
                var reports = _db.TaskReports.Where(tr => tr.TaskID == task.TaskID).ToList();
                ResetReportButton.Visibility = (task.StatusID == 8 && reports.Any()) ? Visibility.Visible : Visibility.Collapsed;

                CompleteTaskButton.Visibility = (task.StatusID == 8 && reports.Any()) ? Visibility.Visible : Visibility.Collapsed;
                CancelTaskButton.Visibility = task.StatusID == 8 ? Visibility.Visible : Visibility.Collapsed;
                BackFromTaskButton.Visibility = task.StatusID != 5 ? Visibility.Visible : Visibility.Collapsed;
            }

            UpdateButtonsVisibility();
        }


        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaskDetailsPanel.Visibility == Visibility.Visible)
            {
                BackFromTaskButton_Click(sender, e);
            }
            else if (StageDetailsStackPanel.Visibility == Visibility.Visible)
            {
                StageDetailsStackPanel.Visibility = Visibility.Collapsed;
                StageButtonsStackPanel.Visibility = Visibility.Visible;
                ShowMainProjectDetails();
                UpdateButtonsVisibility();
            }
            else if (AddStagePanel.Visibility == Visibility.Visible)
            {
                AddStagePanel.Visibility = Visibility.Collapsed;
                ShowMainProjectDetails();
                UpdateButtonsVisibility();
            }
            else if (AddTaskPanel.Visibility == Visibility.Visible)
            {
                AddTaskPanel.Visibility = Visibility.Collapsed;
                StageDetailsStackPanel.Visibility = Visibility.Visible;
                UpdateButtonsVisibility();
            }
            else
            {
                FilterStackPanel.Visibility = Visibility.Visible;
                ProjectsDataGrid.Visibility = Visibility.Visible;
                ProjectsDataGrid2.Visibility = Visibility.Visible;
                ProjectDetailsStackPanel.Visibility = Visibility.Collapsed;
                LoadProjects();
            }
        }

        private void BackFromTaskButton_Click(object sender, RoutedEventArgs e)
        {
            TaskDetailsPanel.Visibility = Visibility.Collapsed;

            TasksTitle.Visibility = Visibility.Visible;
            TasksItemsControl.Visibility = Visibility.Visible;
            NoTasksText.Visibility = TasksItemsControl.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            AddTaskButton.Visibility = Visibility.Visible;
            CompleteStageButton.Visibility = Visibility.Visible;
            CancelStageButton.Visibility = Visibility.Visible;

            MainButtonsPanel.Visibility = Visibility.Visible;
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

                StageNameTextBlock.Text = $"Название: {stage.StageName} \n Описание: {stage.StageDescription}";
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
            MainButtonsPanel.Visibility = Visibility.Collapsed;

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
            MainButtonsPanel.Visibility = Visibility.Collapsed;

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
            MainButtonsPanel.Visibility = Visibility.Visible;
            ActsOfWorkTitle.Visibility = Visibility.Visible;
            ActsOfWorkItemsControl.Visibility = Visibility.Visible;
            ContractsTitle.Visibility = Visibility.Visible;
            ContractsItemsControl.Visibility = Visibility.Visible;
            ProjectDetailsTitle.Visibility = Visibility.Visible;
            UpdateButtonsVisibility();
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
            MainButtonsPanel.Visibility = Visibility.Visible;
            ActsOfWorkTitle.Visibility = Visibility.Collapsed;
            ActsOfWorkItemsControl.Visibility = Visibility.Collapsed;
            ContractsTitle.Visibility = Visibility.Collapsed;
            ContractsItemsControl.Visibility = Visibility.Collapsed;
            ProjectDetailsTitle.Visibility = Visibility.Collapsed;
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
                StatusID = 8 // Статус "В работе"
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
                    MainButtonsPanel.Visibility = Visibility.Visible;

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

        private void DeleteReportButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is TaskReportFiles reportFile)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить этот отчет?", "Подтверждение удаления",
                                           MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var report = _db.TaskReports.FirstOrDefault(r => r.TaskReportFiles.Any(f => f.FileID == reportFile.FileID));

                        if (report != null)
                        {
                            _db.TaskReportFiles.Remove(reportFile);

                            if (report.TaskReportFiles.Count == 1)
                            {
                                _db.TaskReports.Remove(report);
                            }

                            _db.SaveChanges();

                            var task = TaskDetailsPanel.Tag as Tasks;
                            if (task != null)
                            {
                                LoadTaskReports(task);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении отчета: {ex.Message}");
                    }
                }
            }
        }

        private void LoadTaskReports(Tasks task)
        {
            try
            {
                var reports = _db.TaskReports.Where(tr => tr.TaskID == task.TaskID).ToList();
                TaskReportsItemsControl.ItemsSource = reports;

                NoReportsText.Visibility = reports.Any() ? Visibility.Collapsed : Visibility.Visible;
                TaskReportsItemsControl.Visibility = reports.Any() ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке отчетов: {ex.Message}");
            }
        }

        private void ResetReportButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is TaskReports report)
            {
                var result = MessageBox.Show("Вы уверены, что хотите сбросить этот отчет? Отчет будет удален.",
                                           "Подтверждение сброса",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        foreach (var file in report.TaskReportFiles.ToList())
                        {
                            _db.TaskReportFiles.Remove(file);
                        }

                        _db.TaskReports.Remove(report);
                        _db.SaveChanges();

                        var task = TaskDetailsPanel.Tag as Tasks;
                        if (task != null)
                        {
                            LoadTaskReports(task);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сбросе отчета: {ex.Message}");
                    }
                }
            }
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
                        StatusID = 4 // Статус "Выполняется"
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
                _db.SaveChanges();
                LoadProjectStages();
            }
            UpdateButtonsVisibility();
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