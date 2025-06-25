using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeotekMetallCompleteDesktop
{
    public partial class MyDataPage : Page
    {
        public Users _user;
        public readonly GeotekMetallCompleteEntities1 db;

        public MyDataPage(Users user)
        {
            InitializeComponent();
            _user = user;
            db = new GeotekMetallCompleteEntities1();
            Load();
        }

        private void Load()
        {            
            var UserDetails = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Логин", _user.Login.ToString()),
                new KeyValuePair<string, string>("Имя",  _user.FirstName.ToString()),
                new KeyValuePair<string, string>("Фамилия", _user.LastName.ToString()),
                new KeyValuePair<string, string>("Номер телефона", _user.PhoneNumber.ToString()),
                new KeyValuePair<string, string>("Email", _user.Email.ToString()),
                new KeyValuePair<string, string>("Роль", _user.UserRoles.FirstOrDefault().Roles.RoleName)
            };

            UserDetailsItemsControl.ItemsSource = UserDetails;

            LoginTextBox.LostFocus += General.AddText;
            PasswordTextBox.LostFocus += General.AddText;
            FirstNameTextBox.LostFocus += General.AddText;
            LastNameTextBox.LostFocus += General.AddText;
            PhoneNumberTextBox.LostFocus += General.AddText;
            EmailTextBox.LostFocus += General.AddText;

            LoginTextBox.GotFocus += General.RemoveText;
            PasswordTextBox.GotFocus += General.RemoveText;
            FirstNameTextBox.GotFocus += General.RemoveText;
            LastNameTextBox.GotFocus += General.RemoveText;
            PhoneNumberTextBox.GotFocus += General.RemoveText;
            EmailTextBox.GotFocus += General.RemoveText;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {

            LoginTextBox.Text = _user.Login;
            PasswordTextBox.Text = General.DecryptString(_user.PasswordHash);
            FirstNameTextBox.Text = _user.FirstName;
            LastNameTextBox.Text = _user.LastName;
            PhoneNumberTextBox.Text = _user.PhoneNumber;
            EmailTextBox.Text = _user.Email;
            RoleComboBox.Text = _user.UserRoles.FirstOrDefault().Roles.RoleName;
            InfoDataBorder.Visibility = Visibility.Collapsed;
            EditButton.Visibility = Visibility.Collapsed;
            EditUserStackPanel.Visibility = Visibility.Visible;
            InfoData.Visibility = Visibility.Collapsed;
            EditUserBorder.Visibility = Visibility.Visible;
        }
        private void LoginTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[a-zA-Z0-9_]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ResetErrorMessages();

            if (!ValidateInput())
            {
                return;
            }

            var us = db.Users.FirstOrDefault(u => u.UserID == _user.UserID);
            us.Login = LoginTextBox.Text.Trim();
            us.PasswordHash = General.HashPassword(PasswordTextBox.Text.Trim());
            us.FirstName = FirstNameTextBox.Text.Trim();
            us.LastName = LastNameTextBox.Text.Trim();
            us.PhoneNumber = PhoneNumberTextBox.Text.Trim();
            us.Email = EmailTextBox.Text.Trim();
            try
            {
                db.SaveChanges();
                MessageBox.Show("Данные успешно сохранены!");
                _user = us;
                Load();
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.InnerException?.Message ?? ex.Message}");
            }

            EditUserStackPanel.Visibility = Visibility.Collapsed;
            EditUserBorder.Visibility = Visibility.Collapsed;
            InfoDataBorder.Visibility = Visibility.Visible;
            InfoData.Visibility = Visibility.Visible;
            EditButton.Visibility = Visibility.Visible;

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            EditUserStackPanel.Visibility = Visibility.Collapsed;
            EditUserBorder.Visibility = Visibility.Collapsed;
            InfoDataBorder.Visibility = Visibility.Visible;
            InfoData.Visibility = Visibility.Visible;
            EditButton.Visibility = Visibility.Visible;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;
            email = email.Trim();
            try
            {
                return Regex.IsMatch(email,
                    @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled,
                    TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
        private void EmailTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(EmailTextBox.Text) || EmailTextBox.Text != (string)EmailTextBox.Tag)
            {
                if (!Regex.IsMatch(e.Text, @"^[\x20-\x7E]*$"))
                {
                    e.Handled = true;
                    EmailErrorTextBlock.Text = "Допускаются только латинские символы";
                    EmailErrorTextBlock.Visibility = Visibility.Visible;
                }
                else
                {
                    EmailErrorTextBlock.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void EmailTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || EmailTextBox.Text == (string)EmailTextBox.Tag)
            {
                EmailErrorTextBlock.Text = "Введите email";
                EmailErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            if (!Regex.IsMatch(EmailTextBox.Text, @"^[\x20-\x7E]*$"))
            {
                EmailErrorTextBlock.Text = "Допускаются только латинские символы";
                EmailErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            if (!IsValidEmail(EmailTextBox.Text))
            {
                EmailErrorTextBlock.Text = "Неверный формат электронной почты";
                EmailErrorTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                EmailErrorTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private bool ValidateInput()
        {
            bool isValid = true;

            if (LoginTextBox.Text != _user.Login && db.Users.Any(u => u.Login == LoginTextBox.Text))
            {
                LoginErrorTextBlock.Text = "Логин уже существует.";
                LoginErrorTextBlock.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(LoginTextBox.Text))
            {
                LoginErrorTextBlock.Text = "Пожалуйста, введите логин.";
                LoginErrorTextBlock.Visibility = Visibility.Visible;
                isValid = false;
            }
            if (LoginTextBox.Text.Contains(" "))
            {
                LoginErrorTextBlock.Text = "Логин не может содержать пробелы.";
                LoginErrorTextBlock.Visibility = Visibility.Visible;
                isValid = false;
            }
            if (!Regex.IsMatch(LoginTextBox.Text, @"^[a-zA-Z0-9_]+$"))
            {
                LoginErrorTextBlock.Text = "Логин может содержать только кириллицу, цифры и нижнее подчеркивание.";
                LoginErrorTextBlock.Visibility = Visibility.Visible;
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(PasswordTextBox.Text) || PasswordTextBox.Text.Length < 6)
            {
                PasswordErrorTextBlock.Text = "Пароль должен содержать не менее 6 символов.";
                PasswordErrorTextBlock.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                FirstNameErrorTextBlock.Text = "Пожалуйста, введите имя.";
                FirstNameErrorTextBlock.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                LastNameErrorTextBlock.Text = "Пожалуйста, введите фамилию.";
                LastNameErrorTextBlock.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(PhoneNumberTextBox.Text) || PhoneNumberTextBox.Text.Length != 11 || !PhoneNumberTextBox.Text.StartsWith("89"))
            {
                PhoneNumberErrorTextBlock.Text = "Номер телефона должен содержать 11 цифр и начинаться с '89'.";
                PhoneNumberErrorTextBlock.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || !IsValidEmail(EmailTextBox.Text))
            {
                EmailErrorTextBlock.Text = "Пожалуйста, введите корректный email.";
                EmailErrorTextBlock.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) || !Regex.IsMatch(FirstNameTextBox.Text, "^[а-яА-Яa-zA-Z]+$"))
            {
                FirstNameErrorTextBlock.Text = "Пожалуйста, введите корректное имя (только буквы).";
                FirstNameErrorTextBlock.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text) || !Regex.IsMatch(LastNameTextBox.Text, "^[а-яА-Яa-zA-Z]+$"))
            {
                LastNameErrorTextBlock.Text = "Пожалуйста, введите корректную фамилию (только буквы).";
                LastNameErrorTextBlock.Visibility = Visibility.Visible;
                isValid = false;
            }

            return isValid;
        }

        private void ResetErrorMessages()
        {
            LoginErrorTextBlock.Visibility = Visibility.Collapsed;
            PasswordErrorTextBlock.Visibility = Visibility.Collapsed;
            FirstNameErrorTextBlock.Visibility = Visibility.Collapsed;
            LastNameErrorTextBlock.Visibility = Visibility.Collapsed;
            PhoneNumberErrorTextBlock.Visibility = Visibility.Collapsed;
            EmailErrorTextBlock.Visibility = Visibility.Collapsed;
        }

        private void PhoneNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void NameTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            string newText = textBox.Text + e.Text;
            Regex regex = new Regex("^[а-яА-Яa-zA-Z]+$");
            if (!regex.IsMatch(newText))
            {
                e.Handled = true;
            }
        }
    }
}


