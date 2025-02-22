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
        }

        private void RemoveText(object sender, EventArgs e)
        {
            TextBox instance = (TextBox)sender;
            if (instance.Text == instance.Tag.ToString())
            {
                instance.Text = "";
                instance.Foreground = Brushes.Black; 
            }
        }

        private void AddText(object sender, EventArgs e)
        {
            TextBox instance = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(instance.Text))
            {
                instance.Text = instance.Tag.ToString();
                instance.Foreground = Brushes.Gray;
            }
        }

        public static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
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
                string hashedPassword = HashPassword(passwordText); 

                if (user.PasswordHash.Trim() == hashedPassword)
                {
                    MessageBox.Show("Вы вошли в аккаунт");
                    if(role.RoleID == 4)
                    {
                        var admin = new AdminWindow();
                        admin.Show();
                        this.Hide();
                    }
                    else
                    {
                        MessageBox.Show("no role");
                    }
                }
                else
                {
                    MessageBox.Show("Провал");
                }
            }
            else
            {
                MessageBox.Show("Пользователь не найден.");
            }
        }
    }
}
