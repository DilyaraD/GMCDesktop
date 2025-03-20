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

namespace GeotekMetallCompleteDesktop
{
    public partial class AdminWindow : Window
    {
        public Users _user;
        public AdminWindow(Users user)
        {
            InitializeComponent();
            _user = user;
            name.Content = user.Login;
            ContentName.Text = "Вы вошли в аккаунт, "+ _user.FirstName+"!\nВыберите кнопку перехода.";
        }

        private void Button_UserManagement_Click(object sender, RoutedEventArgs e)
        {
            ContentName.Text = "Управление пользователями";
            ContentFrame.Navigate(new userManagementPage(_user));
        }

        private void Button_RequestsManagement_Click(object sender, RoutedEventArgs e)
        {
            ContentName.Text = "Управление заявками";
            ContentFrame.Navigate(new requestManagementPage(_user));
        }
        private void Button_MyDataPage_Click(object sender, RoutedEventArgs e)
        {
            ContentName.Text = "Мои данные";
            ContentFrame.Navigate(new MyDataPage(_user));
        }

        private void Button_ProjectsManagementPage_Click(object sender, RoutedEventArgs e)
        {
            ContentName.Text = "Управление проектами";
            ContentFrame.Navigate(new ProjectsManagementPage(_user));
        }

        private void Button_FinanceManagementPage_Click(object sender, RoutedEventArgs e)
        {
            ContentName.Text = "Управление финансами";
            ContentFrame.Navigate(new FinanceManagementPage(_user));
        }

        private void Button_CustomersManagementPage_Click(object sender, RoutedEventArgs e)
        {
            ContentName.Text = "Клиенты";
            ContentFrame.Navigate(new CustomersManagementPage(_user));
        }

        private void Button_Exit_Click(object sender, RoutedEventArgs e)
        {
            var ex = new Authorization();
            ex.Show();
            this.Close();
        }
    }
}
