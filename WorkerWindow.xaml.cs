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
using System.Windows.Shapes;

namespace GeotekMetallCompleteDesktop
{
    public partial class WorkerWindow : Window
    {
        public Users _user;
        public WorkerWindow(Users user)
        {
            InitializeComponent();
            _user = user;
            name.Content = user.Login;
            ContentName.Text = "Вы вошли в аккаунт, " + _user.FirstName + "!\nВыберите кнопку перехода.";
        }

        private void Button_TasksManagementPage_Click(object sender, RoutedEventArgs e)
        {
            ContentName.Text = "Мои задачи";
            ContentFrame.Navigate(new TasksManagementPage(_user));
        } 
        
        private void Button_ProjectsPage_Click(object sender, RoutedEventArgs e)
        {
            ContentName.Text = "Проекты в работе";
            ContentFrame.Navigate(new ProjectsPage(_user));
        }

        private void Button_MyDataPage_Click(object sender, RoutedEventArgs e)
        {
            ContentName.Text = "Мои данные";
            ContentFrame.Navigate(new MyDataPage(_user));
        }

        private void Button_Exit_Click(object sender, RoutedEventArgs e)
        {
            var ex = new Authorization();
            ex.Show();
            this.Close();
        }
    }
}
