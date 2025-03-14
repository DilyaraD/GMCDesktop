using System;
using System.Collections.Generic;
using System.Globalization;
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
    public partial class UsersView
    {
        public int UserID { get; set; }
        public string Login { get; set; }
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }

    public partial class userManagementPage : Page
    {
        private List<UsersView> _users;
        private List<UsersView> _originalUsers; 

        private bool _isAddingNewUser = false;
        private Users _currentUser;
        public Users _user;

        public userManagementPage(Users user)
        {
            InitializeComponent();
            LoadUsers();
            LoadRoles();
            ShowUserList();
            searchTextBox.GotFocus += General.RemoveText;
            searchTextBox.LostFocus += SearchTextBox_LostFocus;
            _user = user;

            EmailTextBox.LostFocus += EmailTextBox_LostFocus;
            PhoneNumberTextBox.LostFocus += PhoneNumberTextBox_LostFocus;
            PasswordTextBox.LostFocus += PasswordTextBox_LostFocus;

            LoginTextBox.GotFocus += General.RemoveText;
            LoginTextBox.LostFocus += General.AddText;

            PasswordTextBox.GotFocus += General.RemoveText;
            PasswordTextBox.LostFocus += General.AddText;

            FirstNameTextBox.GotFocus += General.RemoveText;
            FirstNameTextBox.LostFocus += General.AddText;

            LastNameTextBox.GotFocus += General.RemoveText;
            LastNameTextBox.LostFocus += General.AddText;

            PhoneNumberTextBox.GotFocus += General.RemoveText;
            PhoneNumberTextBox.LostFocus += General.AddText;

            EmailTextBox.GotFocus += General.RemoveText;
            EmailTextBox.LostFocus += General.AddText;

            LoginTextBox.GotFocus += General.RemoveText;
            LoginTextBox.LostFocus += LoginTextBox_LostFocus;
        }

        private void LoadUsers()
        {
            try
            {
                using (var db = new GeotekMetallCompleteEntities1())
                {
                    _users = db.Users.Where(u => u.UserRoles.All(ur => ur.RoleID != 2))
                        .Select(u => new UsersView
                    {
                        UserID = u.UserID, 
                        Login = u.Login,
                        PasswordHash = u.PasswordHash,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        Role = u.UserRoles.FirstOrDefault().Roles.RoleName
                    }).ToList();
                    _originalUsers = _users.ToList();

                    userList.ItemsSource = _users;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка вывода списка пользователей: {ex.Message}");
            }
        }

        private void LoadRoles()
        {
            try
            {
                using (var db = new GeotekMetallCompleteEntities1())
                {
                    RoleComboBox.ItemsSource = db.Roles.Where(ur => ur.RoleID != 2).ToList();
                    RoleComboBox.DisplayMemberPath = "RoleName";
                    RoleComboBox.SelectedValuePath = "RoleID";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения списка ролей: {ex.Message}");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            _isAddingNewUser = true;
            ClearForm();
            ShowAddUserForm();
        }

        private void ShowAddUserForm()
        {
            ClearForm();
            if (AddUserStackPanel.Visibility != Visibility.Visible || NameText.Text == "Редактировать данные пользователя")
            {
                UserListStackPanel.Visibility = Visibility.Collapsed;
                AddUserStackPanel.Visibility = Visibility.Visible;
                AddUserStackPanel1.Visibility = Visibility.Visible;
                NameText.Text = "Добавить пользователя";
                RoleComboBox.SelectedIndex = -1;
                General.AddText(LoginTextBox, new EventArgs());
                General.AddText(PasswordTextBox, new EventArgs());
                General.AddText(FirstNameTextBox, new EventArgs());
                General.AddText(LastNameTextBox, new EventArgs());
                General.AddText(EmailTextBox, new EventArgs());
                General.AddText(PhoneNumberTextBox, new EventArgs());
            }
            else {
                UserListStackPanel.Visibility = Visibility.Visible;
                AddUserStackPanel.Visibility = Visibility.Collapsed;
                AddUserStackPanel1.Visibility = Visibility.Collapsed;
            }
            UpdateSearchVisibility();
        }

        private void PhoneNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _isAddingNewUser = false;
            ShowUserList();
            ClearForm();
            UpdateSearchVisibility();
        }

        private void ShowUserList()
        {
            UserListStackPanel.Visibility = Visibility.Visible;
            AddUserStackPanel.Visibility = Visibility.Collapsed;
            AddUserStackPanel1.Visibility = Visibility.Collapsed;
            LoadUsers();
            UpdateSearchVisibility();

        }

        private void SearchButton_Checked(object sender, RoutedEventArgs e)
        {
            searchTextBox.Visibility = Visibility.Visible;
            searchButton.Content = "Скрыть";
            UpdateSearchVisibility();

        }

        private void SearchButton_Unchecked(object sender, RoutedEventArgs e)
        {
            searchTextBox.Visibility = Visibility.Collapsed;
            searchButton.Content = "Поиск";
            searchTextBox.Text = "Введите логин или имя";
            searchTextBox.Foreground = Brushes.Gray;
            userList.ItemsSource = _originalUsers;
            UpdateSearchVisibility();

        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text;

            if (_originalUsers == null || userList == null) 
            {
                return; 
            }

            if (string.IsNullOrWhiteSpace(searchText) || searchText == "Введите логин или имя")
            {
                userList.ItemsSource = _originalUsers;
            }
            else
            {
                var filteredUsers = _originalUsers.Where(u =>
                    u.Login.ToLower().Contains(searchText.ToLower()) ||
                    u.FirstName.ToLower().Contains(searchText.ToLower()) ||
                    u.LastName.ToLower().Contains(searchText.ToLower())
                ).ToList();

                userList.ItemsSource = filteredUsers;
            }
            UpdateSearchVisibility();

        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            General.AddText(sender, e);

            if (string.IsNullOrWhiteSpace(searchTextBox.Text) || searchTextBox.Text == "Введите логин или имя")
            {
                LoadUsers();
            }
            UpdateSearchVisibility();

        }

        private void UserList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (userList.SelectedItem is UsersView selectedUserView)
            {
                using (var db = new GeotekMetallCompleteEntities1())
                {
                    var selectedUser = db.Users.FirstOrDefault(u => u.UserID == selectedUserView.UserID);

                    if (selectedUser != null)
                    {
                        _currentUser = selectedUser;
                        ShowEditUserForm(selectedUser);
                    }
                    else
                    {
                        MessageBox.Show("Пользователь не найден в базе данных.");
                    }
                }
            }
            UpdateSearchVisibility();

        }

        private void ShowEditUserForm(Users user)
        {
            ClearForm();
            LoginExistsTextBlock.Visibility = Visibility.Collapsed;
            FirstNameErrorTextBlock.Visibility = Visibility.Collapsed;
            LastNameErrorTextBlock.Visibility = Visibility.Collapsed; 
            PasswordErrorTextBlock.Visibility = Visibility.Collapsed;
            PhoneNumberErrorTextBlock.Visibility = Visibility.Collapsed; 
            EmailErrorTextBlock.Visibility= Visibility.Collapsed;

            RoleErrorTextBlock.Visibility= Visibility.Collapsed;
            UserListStackPanel.Visibility = Visibility.Collapsed;
            AddUserStackPanel.Visibility = Visibility.Visible;
            AddUserStackPanel1.Visibility = Visibility.Visible;
            DeleteButton.Visibility = Visibility.Visible;
            _isAddingNewUser = false;

            NameText.Text = "Редактировать данные пользователя";

            LoginTextBox.Text = user.Login;
            LoginTextBox.Foreground = Brushes.Black;

            PasswordTextBox.Text = General.DecryptString(user.PasswordHash);
            PasswordTextBox.Foreground = Brushes.Black;

            FirstNameTextBox.Text = user.FirstName;
            FirstNameTextBox.Foreground = Brushes.Black;

            LastNameTextBox.Text = user.LastName;
            LastNameTextBox.Foreground = Brushes.Black;

            PhoneNumberTextBox.Text = user.PhoneNumber;
            PhoneNumberTextBox.Foreground = Brushes.Black;

            EmailTextBox.Text = user.Email;
            EmailTextBox.Foreground = Brushes.Black;

            using (var db = new GeotekMetallCompleteEntities1())
            {
                var userRole = db.UserRoles
    .Where(ur => ur.UserID == user.UserID && ur.RoleID != 2)
    .FirstOrDefault();
                if (userRole != null)
                {
                    RoleComboBox.SelectedValue = userRole.RoleID;
                }
                else
                {
                    RoleComboBox.SelectedIndex = -1;
                }
            }
            UpdateSearchVisibility();

        }

        private void LoginTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            using (var db = new GeotekMetallCompleteEntities1())
            {
                if (_isAddingNewUser)
                {
                    if (db.Users.Any(u => u.Login == LoginTextBox.Text))
                    {
                        LoginExistsTextBlock.Text = "Логин уже существует.";
                        LoginExistsTextBlock.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        LoginExistsTextBlock.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    if (_currentUser != null && LoginTextBox.Text != _currentUser.Login)
                    {
                        if (db.Users.Any(u => u.Login == LoginTextBox.Text))
                        {
                            LoginExistsTextBlock.Text = "Логин уже существует.";
                            LoginExistsTextBlock.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            LoginExistsTextBlock.Visibility = Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        LoginExistsTextBlock.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            EmailErrorTextBlock.Visibility = Visibility.Collapsed;
            PhoneNumberErrorTextBlock.Visibility = Visibility.Collapsed;
            PasswordErrorTextBlock.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(LoginTextBox.Text) || LoginTextBox.Text == "Логин")
            {
                LoginExistsTextBlock.Text = "Поле логина обязательно для заполнения.";
                LoginExistsTextBlock.Visibility = Visibility.Visible;
                return;
            }
            if (string.IsNullOrWhiteSpace(PasswordTextBox.Text) || PasswordTextBox.Text == "Пароль")
            {
                PasswordErrorTextBlock.Text = "Поле пароля обязательно для заполнения.";
                PasswordErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) || FirstNameTextBox.Text == "Имя")
            {
                FirstNameErrorTextBlock.Text = "Поле имени обязательно для заполнения.";
                FirstNameErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text) || LastNameTextBox.Text == "Фамилия")
            {
                LastNameErrorTextBlock.Text = "Поле имени обязательно для заполнения.";
                LastNameErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            if (string.IsNullOrWhiteSpace(PhoneNumberTextBox.Text) || PhoneNumberTextBox.Text == "Телефон")
            {
                PhoneNumberErrorTextBlock.Text = "Поле телефона обязательно для заполнения.";
                PhoneNumberErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || EmailTextBox.Text == "Электронная почта")
            {
                EmailErrorTextBlock.Text = "Поле электронной почты обязательно для заполнения.";
                EmailErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }


            if (RoleComboBox.SelectedItem == null)
            {
                RoleErrorTextBlock.Text = "Обязательно выбрать должность пользователя.";
                RoleErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }
            
            if (PasswordTextBox.Text.Length < 6)
            {
                PasswordErrorTextBlock.Text = "Пароль должен содержать больше 6 символов.";
                PasswordErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            if (PhoneNumberTextBox.Text.Length != 11 || !PhoneNumberTextBox.Text.StartsWith("89"))
            {
                PhoneNumberErrorTextBlock.Text = "Номер телефона должно содержать 11 цифр и начинаться с '89'.";
                PhoneNumberErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            if (!IsValidEmail(EmailTextBox.Text))
            {
                EmailErrorTextBlock.Text = "Неверный формат электронной почты.";
                EmailErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                using (var db = new GeotekMetallCompleteEntities1())
                {
                    if (_isAddingNewUser)
                    {
                        if (db.Users.Any(u => u.Login == LoginTextBox.Text))
                        {
                            LoginExistsTextBlock.Text = "Логин уже существует.";
                            LoginExistsTextBlock.Visibility = Visibility.Visible;
                            return;
                        }

                        var newUser = new Users
                        {
                            Login = LoginTextBox.Text,
                            PasswordHash = General.HashPassword(PasswordTextBox.Text),
                            FirstName = FirstNameTextBox.Text,
                            LastName = LastNameTextBox.Text,
                            PhoneNumber = PhoneNumberTextBox.Text,
                            Email = EmailTextBox.Text
                        };

                        db.Users.Add(newUser);
                        db.SaveChanges();

                        int selectedRoleID = (int)RoleComboBox.SelectedValue;
                        var userRole = new UserRoles
                        {
                            UserID = newUser.UserID,
                            RoleID = selectedRoleID
                        };
                        db.UserRoles.Add(userRole);
                        db.SaveChanges();

                        MessageBox.Show("Пользователь успешно добавлен!");
                    }
                    else
                    {
                        var existingUser = db.Users.FirstOrDefault(u => u.Login == LoginTextBox.Text);

                        if (existingUser != null)
                        {
                            existingUser.Login = LoginTextBox.Text;
                            if (PasswordTextBox.Text != null)
                            {
                                existingUser.PasswordHash = General.HashPassword(PasswordTextBox.Text);
                            }
                            existingUser.FirstName = FirstNameTextBox.Text;
                            existingUser.LastName = LastNameTextBox.Text;
                            existingUser.PhoneNumber = PhoneNumberTextBox.Text;
                            existingUser.Email = EmailTextBox.Text;

                            var userRole = db.UserRoles.FirstOrDefault(ur => ur.UserID == existingUser.UserID);
                            if (userRole != null)
                            {
                                userRole.RoleID = (int)RoleComboBox.SelectedValue;
                            }
                            else
                            {
                                int selectedRoleID = (int)RoleComboBox.SelectedValue;
                                userRole = new UserRoles
                                {
                                    UserID = existingUser.UserID,
                                    RoleID = selectedRoleID
                                };
                                db.UserRoles.Add(userRole);
                            }

                            db.SaveChanges();
                            MessageBox.Show("Пользователь успешно обновлен!");
                        }
                        else
                        {
                            MessageBox.Show("Пользователь не найден.");
                            return;
                        }
                    }

                    LoadUsers();
                    ShowUserList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения пользователя: {ex.Message}");
            }
            UpdateSearchVisibility();

        }

        private void EmailTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!IsValidEmail(EmailTextBox.Text))
            {
                EmailErrorTextBlock.Text = "Неверный формат электронной почты.";
                EmailErrorTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                EmailErrorTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void PhoneNumberTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (PhoneNumberTextBox.Text.Length != 11 || !PhoneNumberTextBox.Text.StartsWith("89"))
            {
                PhoneNumberErrorTextBlock.Text = "Номер телефона должно содержать 11 цифр и начинаться с '89'.";
                PhoneNumberErrorTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                PhoneNumberErrorTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void PasswordTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (PasswordTextBox.Text.Length < 6)
            {
                PasswordErrorTextBlock.Text = "Пароль должен содержать больше 6 символов.";
                PasswordErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }
            else
            {
                PasswordErrorTextBlock.Visibility = Visibility.Collapsed;
            }
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

        private void ClearForm()
        {
            LoginTextBox.Clear();
            PasswordTextBox.Clear();
            FirstNameTextBox.Clear();
            LastNameTextBox.Clear();
            PhoneNumberTextBox.Clear();
            EmailTextBox.Clear();
            RoleComboBox.SelectedIndex = -1;

            EmailErrorTextBlock.Visibility = Visibility.Collapsed;
            PhoneNumberErrorTextBlock.Visibility = Visibility.Collapsed;
            PasswordErrorTextBlock.Visibility = Visibility.Collapsed;
            LoginExistsTextBlock.Visibility = Visibility.Collapsed;
            FirstNameErrorTextBlock.Visibility = Visibility.Collapsed;
            LastNameErrorTextBlock.Visibility = Visibility.Collapsed;
            RoleErrorTextBlock.Visibility = Visibility.Collapsed;

            UpdateSearchVisibility();

        }

        private void UpdateSearchVisibility()
        {
            if (AddUserStackPanel.Visibility == Visibility.Visible)
            {
                searchTextBox.Visibility = Visibility.Collapsed;
                searchButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                searchButton.Visibility = Visibility.Visible;
                if (searchButton.IsChecked.HasValue && searchButton.IsChecked.Value) 
                {
                    searchTextBox.Visibility = Visibility.Visible;
                }
                else
                {
                    searchTextBox.Visibility = Visibility.Collapsed;
                }
            }
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

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null)
            {
                MessageBox.Show("Не выбран пользователь для удаления.");
                return;
            }

            MessageBoxResult result = MessageBox.Show($"Вы уверены в удалении пользователя ({_currentUser.Login})?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new GeotekMetallCompleteEntities1())
                    {
                        var userToDelete = db.Users.FirstOrDefault(u => u.UserID == _currentUser.UserID);

                        if (userToDelete != null)
                        {
                            var userRoles = db.UserRoles.Where(ur => ur.UserID == userToDelete.UserID);
                            db.Users.Remove(userToDelete);

                            db.SaveChanges();

                            MessageBox.Show("Пользователь успешно удален!");
                            ShowUserList();
                            ClearForm();
                        }
                        else
                        {
                            MessageBox.Show("Пользователь не найден.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления пользователя: {ex.Message}");
                }
            }
            UpdateSearchVisibility();
            _currentUser = null;
        }
    }
}
