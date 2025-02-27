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
    }
}
