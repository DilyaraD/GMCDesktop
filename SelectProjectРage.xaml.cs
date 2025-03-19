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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GeotekMetallCompleteDesktop
{
    public partial class SelectProjectWindow : Window
    {
        public Projects SelectedProject { get; private set; }

        public SelectProjectWindow(List<Projects> projects)
        {
            InitializeComponent();
            ProjectsListView.ItemsSource = projects;
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedProject = ProjectsListView.SelectedItem as Projects;
            if (SelectedProject != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Выберите проект.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
