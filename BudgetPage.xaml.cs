using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeotekMetallCompleteDesktop
{
    public partial class BudgetPage : Page
    {
        public Users _user;
        private GeotekMetallCompleteEntities1 _db;
        private Projects _selectedProject;

        public BudgetPage(Users user)
        {
            InitializeComponent();
            _user = user;
            _db = new GeotekMetallCompleteEntities1();
            LoadTransactions();
            ResetProjectButton.Visibility = Visibility.Collapsed;
        }

        private void LoadTransactions()
        {
            TransactionsListView.ItemsSource = _db.BudgetTransactions.Include("Projects").Include("Users").ToList();
        }

        private void AddTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            TransactionsListView.Visibility = Visibility.Collapsed;
            AddTransactionPanel.Visibility = Visibility.Visible;
            ResetProjectButton.Visibility = Visibility.Collapsed;
            SelectBtm.Visibility = Visibility.Collapsed;
        }

        private void SelectProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var projects = _db.Projects.ToList();
            var selectProjectWindow = new SelectProjectWindow(projects);
            if (selectProjectWindow.ShowDialog() == true)
            {
                _selectedProject = selectProjectWindow.SelectedProject;
                var filteredTransactions = _db.BudgetTransactions
                    .Where(t => t.ProjectID == _selectedProject.ProjectID)
                    .ToList();
                TransactionsListView.ItemsSource = filteredTransactions;
                ResetProjectButton.Visibility = Visibility.Visible;
            }
        }

        private void ResetProjectButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedProject = null;
            LoadTransactions();
            ResetProjectButton.Visibility = Visibility.Collapsed;
        }

        private void ChangeProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var projects = _db.Projects.ToList();
            var selectProjectWindow = new SelectProjectWindow(projects);
            if (selectProjectWindow.ShowDialog() == true)
            {
                _selectedProject = selectProjectWindow.SelectedProject;
                SelectedProjectTextBlock.Text = _selectedProject.Requests.ObjectName;
            }
        }

        private void TransactionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TransactionTypeComboBox.SelectedItem != null)
            {
                var selectedItem = (ComboBoxItem)TransactionTypeComboBox.SelectedItem;
                if (selectedItem.Content.ToString() == "Другое")
                {
                    CustomDescriptionPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    CustomDescriptionPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SaveTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedProject == null)
                {
                    ShowMessage("Ошибка", "Выберите проект.", MessageBoxImage.Warning);
                    return;
                }

               
                if (string.IsNullOrEmpty(AmountTextBox.Text) || !decimal.TryParse(AmountTextBox.Text, out decimal amount))
                {
                    ShowMessage("Ошибка", "Введите корректную сумму.", MessageBoxImage.Warning);
                    return;
                }

                if (amount < 1m)
                {
                    ShowMessage("Ошибка", "Сумма не может быть меньше 1 рубля", MessageBoxImage.Warning);
                    AmountTextBox.Focus();
                    return;
                }

                if (amount > 10_000_000m)
                {
                    ShowMessage("Ошибка", "Сумма не может превышать 10 000 000 рублей", MessageBoxImage.Warning);
                    AmountTextBox.Focus();
                    return;
                }

                string description;
                if (TransactionTypeComboBox.SelectedItem == null)
                {
                    ShowMessage("Ошибка", "Выберите тип транзакции.", MessageBoxImage.Warning);
                    return;
                }

                var selectedType = (ComboBoxItem)TransactionTypeComboBox.SelectedItem;
                if (selectedType.Content.ToString() == "Другое")
                {
                    if (string.IsNullOrEmpty(DescriptionTextBox.Text))
                    {
                        ShowMessage("Ошибка", "Введите название транзакции.", MessageBoxImage.Warning);
                        return;
                    }
                    description = DescriptionTextBox.Text;
                }
                else
                {
                    description = selectedType.Content.ToString();
                }

                var newTransaction = new BudgetTransactions
                {
                    ProjectID = _selectedProject.ProjectID,
                    TransactionDate = DateTime.Now,
                    Amount = amount,
                    Description = description,
                    RecordedBy = _user.UserID
                };

                _db.BudgetTransactions.Add(newTransaction);
                _db.SaveChanges();

                ShowMessage("Успешно", "Транзакция успешно добавлена!", MessageBoxImage.Information);

                LoadTransactions();
                CancelButton_Click(null, null);
            }
            catch (Exception ex)
            {
                ShowMessage("Ошибка", $"Не удалось добавить транзакцию: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private void ShowMessage(string title, string message, MessageBoxImage icon)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            TransactionsListView.Visibility = Visibility.Visible;
            AddTransactionPanel.Visibility = Visibility.Collapsed;
            FilterPanel.Visibility = Visibility.Visible;
            ResetProjectButton.Visibility = Visibility.Visible;
            SelectBtm.Visibility = Visibility.Visible;
            _selectedProject = null;
            SelectedProjectTextBlock.Text = string.Empty;
            AmountTextBox.Text = string.Empty;
            DescriptionTextBox.Text = string.Empty;
            TransactionTypeComboBox.SelectedIndex = -1;
            CustomDescriptionPanel.Visibility = Visibility.Collapsed;
        }

        private void AmountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$");
            if (!regex.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text)))
            {
                e.Handled = true;
            }
        
            TextBox textBox = sender as TextBox;

            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            if (textBox.Text.Length == 0 && e.Text == "0")
            {
                e.Handled = true;
            }
        }

        private void AmountTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

    }
}