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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GeotekMetallCompleteDesktop
{
    public partial class Authorization : Window
    {
        private readonly GeotekMetallCompleteEntities1 _context;

        private string passwordText = "";

        public Authorization()
        {
            InitializeComponent();
            _context = new GeotekMetallCompleteEntities1();

            login.GotFocus += General.RemoveText;
            login.LostFocus += General.AddText;
            placeholderText.GotFocus += General.RemoveText;
            placeholderText.LostFocus += General.AddText;

            passwordBox.GotFocus += (s, e) => placeholderText.Visibility = Visibility.Collapsed;
            passwordBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrEmpty(passwordBox.Password))
                    placeholderText.Visibility = Visibility.Visible;
            };
        }


        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string loginText = login.Text;

            if (loginText == (string)login.Tag) { loginText = ""; }

            if (string.IsNullOrEmpty(loginText) || string.IsNullOrEmpty(passwordText))
            {
                MessageBox.Show("Пожалуйста, введите логин и пароль.");
                return;
            }

            var user = _context.Users.FirstOrDefault(u => u.Login == loginText);

            if (user != null)
            {
                var role = _context.UserRoles.FirstOrDefault(u => u.UserID == user.UserID);
                string hashedPassword = General.HashPassword(passwordText);

                if (user.PasswordHash.Trim() == hashedPassword)
                {
                    if (role.RoleID == 4)
                    {
                        var admin = new AdminWindow(user);
                        admin.Show();
                        this.Hide();
                    }
                    else if (role.RoleID == 1)
                    {
                        var admin = new WorkerWindow(user);
                        admin.Show();
                        this.Hide();
                    }
                    else if (role.RoleID == 3)
                    {
                        var admin = new AccountantWindow(user);
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

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            passwordText = passwordBox.Password;
            placeholderText.Visibility = string.IsNullOrEmpty(passwordText) ? Visibility.Visible : Visibility.Hidden;
        }

        private void ShowPasswordCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            placeholderText.Visibility = Visibility.Collapsed;
            passwordTextBox.Text = passwordText;
            passwordTextBox.Visibility = Visibility.Visible;
            passwordBox.Visibility = Visibility.Collapsed;
            passwordTextBox.Focus();
        }

        private void ShowPasswordCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            passwordBox.Password = passwordText;
            passwordBox.Visibility = Visibility.Visible;
            passwordTextBox.Visibility = Visibility.Collapsed;
            placeholderText.Visibility = string.IsNullOrEmpty(passwordText)
                ? Visibility.Visible
                : Visibility.Collapsed;
            passwordBox.Focus();
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            passwordText = passwordTextBox.Text;
        }
    }
}