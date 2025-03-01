using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    public partial class MyDataPage : Page
    {
        public Users _user;
        public readonly GeotekMetallCompleteEntities db;

        public MyDataPage(Users user)
        {
            InitializeComponent();
            _user = user;
            db =new GeotekMetallCompleteEntities();
            Load();
        }

        private void Load()
        {
            LoginDisplayTextBlock.Text = _user.Login;
            FirstNameDisplayTextBlock.Text = _user.FirstName;
            LastNameDisplayTextBlock.Text = _user.LastName;
            PhoneNumberDisplayTextBlock.Text = _user.PhoneNumber;
            EmailDisplayTextBlock.Text = _user.Email;
            RoleComboBox.Text = _user.UserRoles.FirstOrDefault().Roles.RoleName;

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
            InfoDataBorder.Visibility = Visibility.Collapsed;
            EditButton.Visibility = Visibility.Collapsed;
            EditUserStackPanel.Visibility = Visibility.Visible;
            InfoData.Visibility = Visibility.Collapsed;
            EditUserBorder.Visibility = Visibility.Visible;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ResetErrorMessages();

            if (!ValidateInput())
            {
                return;
            }

            _user.Login = LoginTextBox.Text;
            _user.PasswordHash = General.HashPassword(PasswordTextBox.Text);
            _user.FirstName = FirstNameTextBox.Text;
            _user.LastName = LastNameTextBox.Text;
            _user.PhoneNumber = PhoneNumberTextBox.Text;
            _user.Email = EmailTextBox.Text;

            LoginDisplayTextBlock.Text = _user.Login;
            FirstNameDisplayTextBlock.Text = _user.FirstName;
            LastNameDisplayTextBlock.Text = _user.LastName;
            PhoneNumberDisplayTextBlock.Text = _user.PhoneNumber;
            EmailDisplayTextBlock.Text = _user.Email;

            MessageBox.Show("Данные успешно сохранены!");
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
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                return Regex.IsMatch(email,
                    @"^(([^<>()[\]\\.,;:\s@\""]+(\.[^<>()[\]\\.,;:\s@\""]+)*)|(\"".+\""))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
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


