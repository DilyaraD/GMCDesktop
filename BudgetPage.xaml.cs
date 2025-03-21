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
        }

        private void LoadTransactions()
        {
            TransactionsListView.ItemsSource = _db.BudgetTransactions.Include("Projects").Include("Users").ToList();
        }

        private void AddTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            var projects = _db.Projects.ToList();
            var selectProjectWindow = new SelectProjectWindow(projects);
            if (selectProjectWindow.ShowDialog() == true)
            {
                _selectedProject = selectProjectWindow.SelectedProject;
                SelectedProjectTextBlock.Text = _selectedProject.Requests.ObjectName;
                TransactionsListView.Visibility = Visibility.Collapsed;
                AddTransactionPanel.Visibility = Visibility.Visible;
            }
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
            }
        }

        private void ResetProjectButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedProject = null;
            LoadTransactions();
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

        private void SaveTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProject == null)
            {
                MessageBox.Show("Выберите проект.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(AmountTextBox.Text) || !decimal.TryParse(AmountTextBox.Text, out decimal amount))
            {
                MessageBox.Show("Введите корректную сумму.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(DescriptionTextBox.Text))
            {
                MessageBox.Show("Введите описание транзакции.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newTransaction = new BudgetTransactions
            {
                ProjectID = _selectedProject.ProjectID,
                TransactionDate = DateTime.Now,
                Amount = amount,
                Description = DescriptionTextBox.Text,
                RecordedBy = _user.UserID
            };

            _db.BudgetTransactions.Add(newTransaction);
            _db.SaveChanges();

            LoadTransactions();
            CancelButton_Click(null, null);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            TransactionsListView.Visibility = Visibility.Visible;
            AddTransactionPanel.Visibility = Visibility.Collapsed;
            _selectedProject = null;
            SelectedProjectTextBlock.Text = string.Empty;
            AmountTextBox.Text = string.Empty;
            DescriptionTextBox.Text = string.Empty;
        }

        private void AmountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$");
            if (!regex.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text)))
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