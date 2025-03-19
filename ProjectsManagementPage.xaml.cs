using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GeotekMetallCompleteDesktop
{
    public partial class ProjectsManagementPage : Page
    {
        public Users _user;
        private GeotekMetallCompleteEntities1 _db;
        private List<Projects> _projects;
        private List<ProjectViewModel> _projectViewModels;
        private Projects _selectedProject;

        public ProjectsManagementPage(Users user)
        {
            InitializeComponent();
            _db = new GeotekMetallCompleteEntities1();
            _user = user;

            SearchTextBox.GotFocus += General.RemoveText;
            SearchTextBox.LostFocus += SearchTextBox_LostFocus;

            SortByDeadlineComboBox.SelectedIndex = 0; 
            LoadProjects();
        }

        private void LoadProjects()
        {
            _projects = _db.Projects.Include("Requests").Include("ProjectStages").ToList();
            _projectViewModels = _projects
                .Select(p => new ProjectViewModel
                {
                    ProjectID = p.ProjectID,
                    RequestID = p.RequestID,
                    ProjectStartDate = p.ProjectStartDate,
                    ProjectEndDate = p.ProjectEndDate,
                    ProjectManagerID = p.ProjectManagerID,
                    WorkName = p.Requests.WorkTypes?.WorkTypeName ?? "Не найдено",
                    StageCount = p.ProjectStages?.Count ?? 0 
                }).ToList();

            ProjectsDataGrid.ItemsSource = _projectViewModels;
        }

        private void ApplyFiltersAndSort()
        {
            if (_projectViewModels == null)
            {
                return;
            }

            var searchText = SearchTextBox.Text.ToLower();
            var selectedSort = SortByDeadlineComboBox?.SelectedItem as ComboBoxItem;
            var sortText = selectedSort?.Content?.ToString();

            var filteredProjects = _projectViewModels
                .Where(p => string.IsNullOrEmpty(searchText) || p.WorkName.ToLower().Contains(searchText))
                .ToList();

            if (!string.IsNullOrEmpty(sortText) && sortText != "Сортировка по дате")
            {
                switch (sortText)
                {
                    case "Ближайшие":
                        filteredProjects = filteredProjects.OrderBy(p => p.ProjectStartDate).ToList();
                        break;
                    case "Убывание":
                        filteredProjects = filteredProjects.OrderByDescending(p => p.ProjectStartDate).ToList();
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
            ProjectDetailsStackPanel.Visibility = Visibility.Visible;

            var requestDetails = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Название работы", _selectedProject.Requests.ObjectName),
                new KeyValuePair<string, string>("Адрес", _selectedProject.Requests.Address),
                new KeyValuePair<string, string>("Дата начала", _selectedProject.ProjectStartDate?.ToString("dd.MM.yyyy")),
                new KeyValuePair<string, string>("Описание", _selectedProject.Requests.Description),
                new KeyValuePair<string, string>("Количество этапов", _selectedProject.ProjectStages.Count.ToString())
            };

            RequestDetailsItemsControl.ItemsSource = requestDetails;

            // Загружаем акты и договоры
            ActsOfWorkItemsControl.ItemsSource = _selectedProject.ActsOfWork.ToList();
            ContractsItemsControl.ItemsSource = _selectedProject.Contracts.ToList();

            LoadProjectStages();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            FilterStackPanel.Visibility = Visibility.Visible;
            ProjectsDataGrid.Visibility = Visibility.Visible;
            ProjectDetailsStackPanel.Visibility = Visibility.Collapsed;
        }

        private void LoadProjectStages()
        {
            StageButtonsStackPanel.Children.Clear();
            foreach (var stage in _selectedProject.ProjectStages)
            {
                var stageButton = new Button
                {
                    Content = $"Этап {stage.StageID}: {stage.StageName}",
                    Tag = stage,
                    Background = Brushes.LightGray,
                    Margin = new Thickness(5, 0, 5, 0)
                };
                stageButton.Click += StageButton_Click;
                StageButtonsStackPanel.Children.Add(stageButton);
            }
        }

        private void StageButton_Click(object sender, RoutedEventArgs e)
        {
            var stage = (sender as Button)?.Tag as ProjectStages;
            if (stage != null)
            {
                // Скрываем другие элементы
                RequestDetailsItemsControl.Visibility = Visibility.Collapsed;
                StageButtonsStackPanel.Visibility = Visibility.Collapsed;

                // Показываем информацию о этапе
                StageDetailsStackPanel.Visibility = Visibility.Visible;

                // Заполняем информацию о этапе
                StageNameTextBlock.Text = $"Название: {stage.StageName}";
                StageStartDateTextBlock.Text = $"Дата начала: {stage.StartDate?.ToString("dd.MM.yyyy")}";
                StageEndDateTextBlock.Text = $"Дата окончания: {stage.EndDate?.ToString("dd.MM.yyyy")}";
                StageStatusTextBlock.Text = $"Статус: {stage.Statuses?.StatusName}";

                // Загружаем задачи
                LoadTasks(stage);
            }
        }

        private void LoadTasks(ProjectStages stage)
        {
            TasksItemsControl.ItemsSource = stage.Tasks.ToList();
        }

        private void TaskButton_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button)?.Tag as Tasks;
            if (task != null)
            {
                MessageBox.Show($"Задача: {task.TaskDescription}\nСрок: {task.DueDate?.ToString("dd.MM.yyyy")}\nСтатус: {task.Statuses?.StatusName}");
            }
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var stage = (StageDetailsStackPanel.Tag as ProjectStages);
            if (stage != null)
            {
                var newTask = new Tasks
                {
                    StageID = stage.StageID,
                    TaskDescription = "Новая задача",
                    DueDate = DateTime.Now,
                    StatusID = 1 // Статус "Новый"
                };

                _db.Tasks.Add(newTask);
                _db.SaveChanges();
                LoadTasks(stage);
            }
        }

        private void AddStageButton_Click(object sender, RoutedEventArgs e)
        {
            var newStage = new ProjectStages
            {
                ProjectID = _selectedProject.ProjectID,
                StageName = "Новый этап",
                StageDescription = "Описание нового этапа",
                StartDate = DateTime.Now,
                StatusID = 6 // Статус "Новый"
            };

            _db.ProjectStages.Add(newStage);
            _db.SaveChanges();
            LoadProjectStages();
        }

        private void CancelProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите отменить проект? Все этапы будут отменены.", "Подтверждение", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                foreach (var stage in _selectedProject.ProjectStages)
                {
                    stage.StatusID = 5; // Статус "Отменен"
                }
                _db.SaveChanges();
                LoadProjectStages();
            }
        }

        private void CompleteStageButton_Click(object sender, RoutedEventArgs e)
        {
            //var selectedStage = ProjectStagesStackPanel.Children.OfType<Button>().FirstOrDefault(b => b.IsFocused)?.Tag as ProjectStages;
            //if (selectedStage != null)
            //{
            //    selectedStage.StatusID = 7; // Статус "Завершен"
            //    _db.SaveChanges();
            //    LoadProjectStages();
            //}
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
    }
    
}