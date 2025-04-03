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
    public partial class AccountantWindow : Window
    {
        public Users _user;
        public AccountantWindow(Users user)
        {
            InitializeComponent();
            _user = user;
            name.Content = user.Login;
            ContentName.Text = "Вы вошли в аккаунт, " + _user.FirstName + "!\nВыберите кнопку перехода.";
        }

        private void Button_BudgetPage_Click(object sender, RoutedEventArgs e)
        {
            ContentName.Text = "Управление бюджетом проектов";
            ContentFrame.Navigate(new BudgetPage(_user));
        }

        private void Button_ActsOfWorkPage_Click(object sender, RoutedEventArgs e)
        {
            ContentName.Text = "Управление актами выполненных работ";
            ContentFrame.Navigate(new ActsOfWorkPage(_user));
        }

        private void Button_MyDataPage_Click(object sender, RoutedEventArgs e)
        {
            ContentName.Text = "Мои данные";
            ContentFrame.Navigate(new MyDataPage(_user));
        }

        private void Button_ContractsPage_Click(object sender, RoutedEventArgs e)
        {
            ContentName.Text = "Управление договорами";
            ContentFrame.Navigate(new ContractsPage(_user));
        }

        private void Button_FinanceManagementPage_Click(object sender, RoutedEventArgs e)
        {
            ContentName.Text = "Управление финансовыми отчётами";
            ContentFrame.Navigate(new FinanceManagementPage(_user));
        }

        private void Button_Exit_Click(object sender, RoutedEventArgs e)
        {
            var ex = new Authorization();
            ex.Show();
            this.Close();            
        }
    }
}
