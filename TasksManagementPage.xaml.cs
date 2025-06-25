using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GeotekMetallCompleteDesktop
{
    public partial class TasksManagementPage : Page
    {
        public Users _user;
        private List<Tasks> _allTasks;
        private Projects _selectedProject;
        private bool _hasFiltersApplied = false;

        public TasksManagementPage(Users user)
        {
            InitializeComponent();
            _user = user;
            LoadTasks();
            LoadStatuses();
        }

        private void LoadTasks()
        {
            try
            {
                using (var context = new GeotekMetallCompleteEntities1())
                {
                    _allTasks = context.TaskUsers
                        .Where(tu => tu.UserID == _user.UserID)
                        .Select(tu => tu.Tasks)
                        .Include("ProjectStages")
                        .Include("ProjectStages.Projects")
                        .Include("ProjectStages.Projects.Requests")
                        .Include("ProjectStages.Projects.Requests.WorkTypes")
                        .Include("Statuses")
                        .ToList();

                    TasksListView.ItemsSource = _allTasks;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке задач: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStatuses()
        {
            try
            {
                using (var context = new GeotekMetallCompleteEntities1())
                {
                    var statuses = context.Statuses.ToList();
                    StatusFilterComboBox.Items.Clear();
                    StatusFilterComboBox.Items.Add(new ComboBoxItem { Content = "Все статусы", IsSelected = true });

                    foreach (var status in statuses)
                    {
                        StatusFilterComboBox.Items.Add(new ComboBoxItem { Content = status.StatusName, Tag = status.StatusID });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке статусов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterByProjectBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new GeotekMetallCompleteEntities1())
                {
                    var projects = context.Projects
                        .Include("Requests")
                        .ToList();

                    var selectProjectWindow = new SelectProjectWindow(projects);
                    if (selectProjectWindow.ShowDialog() == true)
                    {
                        _selectedProject = selectProjectWindow.SelectedProject;
                        ApplyFilters();
                        _hasFiltersApplied = true;
                        UpdateResetButtonVisibility();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выборе проекта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatusFilterComboBox.SelectedIndex > 0)
            {
                _hasFiltersApplied = true;
                ApplyFilters();
                UpdateResetButtonVisibility();
            }
            else if (_hasFiltersApplied && StatusFilterComboBox.SelectedIndex == 0)
            {
                _hasFiltersApplied = HasActiveFilters();
                ApplyFilters();
                UpdateResetButtonVisibility();
            }
        }

        private void DueDateFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DueDateFilterComboBox.SelectedIndex > 0)
            {
                _hasFiltersApplied = true;
                ApplyFilters();
                UpdateResetButtonVisibility();
            }
            else if (_hasFiltersApplied && DueDateFilterComboBox.SelectedIndex == 0)
            {
                _hasFiltersApplied = HasActiveFilters();
                ApplyFilters();
                UpdateResetButtonVisibility();
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filteredTasks = _allTasks.AsQueryable();

                if (_selectedProject != null)
                {
                    filteredTasks = filteredTasks.Where(t => t.ProjectStages.ProjectID == _selectedProject.ProjectID);
                }

                var selectedStatus = StatusFilterComboBox.SelectedItem as ComboBoxItem;
                if (selectedStatus != null && selectedStatus.Tag != null)
                {
                    int statusId = (int)selectedStatus.Tag;
                    filteredTasks = filteredTasks.Where(t => t.StatusID == statusId);
                }

                switch (DueDateFilterComboBox.SelectedIndex)
                {
                    case 1:
                        filteredTasks = filteredTasks.OrderBy(t => t.DueDate);
                        break;
                    case 2:
                        filteredTasks = filteredTasks.OrderByDescending(t => t.DueDate);
                        break;
                    default:
                        filteredTasks = filteredTasks.OrderBy(t => t.TaskID);
                        break;
                }
                TasksListView.ItemsSource = filteredTasks.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при применении фильтров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool HasActiveFilters()
        {
            return _selectedProject != null ||
                   (StatusFilterComboBox.SelectedIndex > 0) ||
                   (DueDateFilterComboBox.SelectedIndex > 0);
        }

        private void ResetFiltersBtn_Click(object sender, RoutedEventArgs e)
        {
            _selectedProject = null;
            StatusFilterComboBox.SelectedIndex = 0;
            DueDateFilterComboBox.SelectedIndex = 0;
            _hasFiltersApplied = false;
            ApplyFilters();
            UpdateResetButtonVisibility();
        }

        private void UpdateResetButtonVisibility()
        {
            ResetFiltersBtn.Visibility = _hasFiltersApplied ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TasksListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTask = TasksListView.SelectedItem as Tasks;
            if (selectedTask != null)
            {
                ShowTaskDetails(selectedTask);
            }
        }

        private void ShowTaskDetails(Tasks task)
        {
            TaskNameText.Text = task.TaskDescription;
            TaskDescriptionText.Text = task.TaskDescription;
            ProjectNameText.Text = task.ProjectStages?.Projects?.Requests?.ObjectName ?? "Не указано";
            StageNameText.Text = task.ProjectStages?.StageName ?? "Не указано";
            DueDateText.Text = task.DueDate?.ToString("dd.MM.yyyy") ?? "Не указано";
            StatusText.Text = task.Statuses?.StatusName ?? "Не указано";
            AddressText.Text = task.ProjectStages?.Projects?.Requests?.Address ?? "Не указано";
            WorkTypeText.Text = task.ProjectStages?.Projects?.Requests?.WorkTypes?.WorkTypeName ?? "Не указано";

            Filter.Visibility = Visibility.Collapsed;
            TasksListView.Visibility = Visibility.Collapsed;
            TaskDetailsPanel.Visibility = Visibility.Visible;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            TasksListView.Visibility = Visibility.Visible;
            TaskDetailsPanel.Visibility = Visibility.Collapsed;
            Filter.Visibility = Visibility.Visible;
            TasksListView.SelectedItem = null;
        }
    }
}