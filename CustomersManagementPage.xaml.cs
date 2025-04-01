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
using static GeotekMetallCompleteDesktop.requestManagementPage;

namespace GeotekMetallCompleteDesktop
{
    public partial class CustomersManagementPage : Page
    {
        public Users _user;
        private GeotekMetallCompleteEntities1 _db;
        private List<Users> _users;
        public CustomersManagementPage(Users user)
        {
            InitializeComponent();
            _user = user;
            SearchTextBox.GotFocus += General.RemoveText;
            SearchTextBox.LostFocus += SearchTextBox_LostFocus;
            _db = new GeotekMetallCompleteEntities1();
            LoadCustomers();
        }

        private void LoadCustomers()
        {
            _users = _db.Users
                    .Where(u => u.UserRoles.Any(ur => ur.RoleID == 2))
                    .ToList();

            CustomersDataGrid.ItemsSource = _users;
        }



        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            General.AddText(sender, e);

            if (string.IsNullOrWhiteSpace(SearchTextBox.Text) || SearchTextBox.Text == "Поиск")
            {
                CustomersDataGrid.ItemsSource = _users;
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
            SearchTextBox.Text = "Поиск";
            SearchTextBox.Foreground = Brushes.Gray;
            CustomersDataGrid.ItemsSource = _users;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFiltersAndSort();
        }

        private void ApplyFiltersAndSort()
        {
            if (_users == null)
            {
                return;
            }

            var searchText = SearchTextBox.Text.ToLower();

            var filteredUsers = _users
                .Where(p => string.IsNullOrEmpty(searchText) || p.FirstName.ToLower().Contains(searchText) || p.LastName.ToLower().Contains(searchText))
                .ToList();


            CustomersDataGrid.ItemsSource = filteredUsers;
        }

        private void LoadUserProjects(Users user)
        {
            // Загружаем проекты, где UserID в Requests совпадает с UserID выбранного пользователя
            var projects = _db.Projects
                .Include("Requests") // Включаем связанные данные из Requests
                .Where(p => p.Requests.UserID == user.UserID)
                .ToList();

            // Отладочный вывод
            Console.WriteLine($"Найдено проектов: {projects.Count}");

            // Привязываем проекты к ListView
            UserProjectsListView.ItemsSource = projects;
        }

        private void CustomersDataGrid_SelectionChanged(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("Событие выбора пользователя вызвано");
            var selectedUser = CustomersDataGrid.SelectedItem as Users;
            if (selectedUser != null)
            {
                // Скрываем список пользователей
                CustomersDataGrid.Visibility = Visibility.Collapsed;
                CustomersDataGrid2.Visibility = Visibility.Collapsed;
                FilterStackPanel.Visibility = Visibility.Collapsed;

                // Показываем информацию о пользователе
                UserDetailsStackPanel.Visibility = Visibility.Visible;

                // Заполняем информацию о пользователе
                UserInfoTextBlock.Text = $"Логин: {selectedUser.Login}\nИмя: {selectedUser.FirstName}\nФамилия: {selectedUser.LastName}\nEmail: {selectedUser.Email}";

                // Загружаем проекты пользователя
                LoadUserProjects(selectedUser);

                // Загружаем заявки пользователя
                LoadUserRequests(selectedUser);
            }
        }
        private void UserRequestsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedRequest = UserRequestsListView.SelectedItem as RequestViewModel;
            if (selectedRequest != null)
            {
                // Переход на страницу управления заявками с передачей выбранной заявки
                var requestManagementPage = new requestManagementPage(_user, selectedRequest);
                NavigationService.Navigate(requestManagementPage);
            }
            else
            {
                MessageBox.Show("Заявка не выбрана или данные не привязаны.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UserProjectsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedProject = UserProjectsListView.SelectedItem as Projects;
            if (selectedProject != null)
            {
                // Переход на страницу управления проектами
                var projectsManagementPage = new ProjectsManagementPage(_user, selectedProject);
                NavigationService.Navigate(projectsManagementPage);
            }
        }

        private void LoadUserRequests(Users user)
        {
            // Загружаем заявки, где UserID совпадает с UserID выбранного пользователя
            var requests = _db.Requests
                .Include("WorkTypes") // Включаем связанные данные из WorkTypes
                .Where(r => r.UserID == user.UserID)
                .Select(r => new RequestViewModel
                {
                RequestID = r.RequestID,
                UserID = r.UserID,
                ObjectName = r.ObjectName,
                Address = r.Address,
                Area = r.Area,
                Floors = r.Floors,
                ObjectType = r.ObjectType,
                RoomCount = r.RoomCount,
                Description = r.Description,
                Deadline = r.Deadline,
                ApprovalReason = r.ApprovalReason,
                StatusID = r.StatusID,
                Request = r,
                WorkTypeName = r.WorkTypes.WorkTypeName,
            }).ToList();

            // Отладочный вывод
            Console.WriteLine($"Найдено заявок: {requests.Count}");

            // Привязываем заявки к ListView
            UserRequestsListView.ItemsSource = requests;
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Возврат к списку пользователей
            UserDetailsStackPanel.Visibility = Visibility.Collapsed;
            CustomersDataGrid.Visibility = Visibility.Visible;
            CustomersDataGrid2.Visibility = Visibility.Visible;
            FilterStackPanel.Visibility = Visibility.Visible;
        }
    }
}
