using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GeotekMetallCompleteDesktop
{
    public partial class SelectProjectWindow : Window
    {
        public Projects SelectedProject { get; private set; }
        private List<dynamic> _allProjects;

        public SelectProjectWindow(List<Projects> projects)
        {
            InitializeComponent();

            _allProjects = projects.Select(p => new
            {
                Project = p,
                ProjectID = p.ProjectID,
                Requests = p.Requests,
                Status = GetProjectStatus(p)
            }).ToList<dynamic>();

            StatusFilterComboBox.Items.Add("Все");

            var statuses = _allProjects
                .Select(p => p.Status.ToString())
                .Distinct()
                .ToList();

            foreach (var status in statuses)
            {
                StatusFilterComboBox.Items.Add(status);
            }

            StatusFilterComboBox.SelectedIndex = 0;

            ProjectsListView.ItemsSource = _allProjects;
        }

        private void FilterProjects()
        {
            string selectedStatus = StatusFilterComboBox.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedStatus)) return;

            if (selectedStatus == "Все")
            {
                ProjectsListView.ItemsSource = _allProjects;
            }
            else
            {
                ProjectsListView.ItemsSource = _allProjects
                    .Where(p => p.Status == selectedStatus)
                    .ToList();
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            dynamic selected = ProjectsListView.SelectedItem;
            if (selected != null)
            {
                SelectedProject = selected.Project;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Выберите проект.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterProjects();
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
    }
}