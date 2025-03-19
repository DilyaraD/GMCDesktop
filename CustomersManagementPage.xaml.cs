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

        private void CustomersDataGrid_SelectionChanged(object sender, MouseButtonEventArgs e)
        {
            
        }
    }
}
