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
    public partial class SelectWorkersWindow : Window
    {
        public List<Users> SelectedWorkers { get; private set; } = new List<Users>();

        public SelectWorkersWindow(List<Users> workers)
        {
            InitializeComponent();
            WorkersListBox.ItemsSource = workers.Select(w => new WorkerViewModel
            {
                User = w,
                FullName = $"{w.LastName} {w.FirstName}"
            }).ToList();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedWorkers = WorkersListBox.ItemsSource
                .Cast<WorkerViewModel>()
                .Where(w => w.IsSelected)
                .Select(w => w.User)
                .ToList();
            DialogResult = true;
        }
    }

    public class WorkerViewModel
    {
        public Users User { get; set; }
        public string FullName { get; set; }
        public bool IsSelected { get; set; }
    }
}