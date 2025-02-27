using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
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
    public partial class Authorization : Window
    {
        private readonly GeotekMetallCompleteEntities _context;
        public Authorization()
        {
            InitializeComponent();
            _context = new GeotekMetallCompleteEntities();
            login.GotFocus += General.RemoveText;
            login.LostFocus += General.AddText;

            password.GotFocus += General.RemoveText;
            password.LostFocus += General.AddText;
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string loginText = login.Text;
            string passwordText = password.Text;

            if (loginText == (string)login.Tag) { loginText = ""; }
            if (passwordText == (string)password.Tag) { passwordText = ""; }

            if (string.IsNullOrEmpty(loginText) || string.IsNullOrEmpty(passwordText))
            {
                MessageBox.Show("Пожалуйста, введите логин и пароль.");
                return;
            }

            var user = _context.Users.FirstOrDefault(u => u.Login == loginText);
            var role = _context.UserRoles.FirstOrDefault(u=>u.UserID == user.UserID);

            if (user != null)
            {
                string hashedPassword = General.HashPassword(passwordText); 

                if (user.PasswordHash.Trim() == hashedPassword)
                {
                    //MessageBox.Show("Вы вошли в аккаунт");
                    if(role.RoleID == 4)
                    {
                        var admin = new AdminWindow(user);
                        admin.Show();
                        this.Hide();
                    }
                    else
                    {
                        MessageBox.Show("Не определена роль");
                    }
                }
                else
                {
                    MessageBox.Show("Пароль неверный!");
                }
            }
            else
            {
                MessageBox.Show("Пользователь не найден.");
            }
        }
    }
}
