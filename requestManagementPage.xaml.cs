using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static GeotekMetallCompleteDesktop.requestManagementPage;

namespace GeotekMetallCompleteDesktop
{
    public partial class requestManagementPage : Page
    {
        public Users _user;
        private List<Requests> _requests;
        private Requests _selectedRequest;
        private List<WorkTypes> _workTypes;
        private List<RequestViewModel> _requestViewModels;
        private bool _isApproving = false;
        private readonly GeotekMetallCompleteEntities1 _context;

        public requestManagementPage(Users user)
        {
            InitializeComponent();
            _context = new GeotekMetallCompleteEntities1();
            _user = user;
            LoadData();
            FilterByStatusComboBox.SelectedIndex = 0;
            SortByDeadlineComboBox.SelectedIndex = 0;
            FilterByWorkTypeComboBox.SelectedIndex = 0;
        }

        private void LoadData()
        {
            using (var db = new GeotekMetallCompleteEntities1())
            {
                _requests = db.Requests.ToList();
                _workTypes = db.WorkTypes.ToList(); 

                FilterByWorkTypeComboBox.Items.Clear();
                FilterByWorkTypeComboBox.Items.Add(new ComboBoxItem { Content = "Все типы работ" });
                foreach (var workType in _workTypes)
                {
                    FilterByWorkTypeComboBox.Items.Add(new ComboBoxItem { Content = workType.WorkTypeName });
                }

                var requestViewModels = _requests.Select(r => new RequestViewModel
                {
                    RequestID = r.RequestID,
                    UserID = r.UserID,
                    ObjectName = r.ObjectName,
                    Address = r.Address,
                    Area = r.Area,
                    Floors = r.Floors,
                    ObjectType = r.ObjectType,
                    RoomCount = r.RoomCount,
                    Description = r.Description,
                    Deadline = r.Deadline,
                    ApprovalReason = r.ApprovalReason,
                    StatusID = r.StatusID,
                    Request = r,
                    WorkTypeName = _workTypes.FirstOrDefault(wt => wt.WorkTypeID == r.Purpose)?.WorkTypeName ?? "Не указано"
                }).ToList();

                RequestList.ItemsSource = requestViewModels;
            }
        }

        public class RequestViewModel
        {
            public int RequestID { get; set; }
            public Nullable<int> UserID { get; set; }
            public string ObjectName { get; set; }
            public string Address { get; set; }
            public decimal Area { get; set; }
            public Nullable<int> Floors { get; set; }
            public string ObjectType { get; set; }
            public Nullable<int> Purpose { get; set; }
            public Nullable<int> RoomCount { get; set; }
            public string Description { get; set; }
            public System.DateTime Deadline { get; set; }
            public string ApprovalReason { get; set; }
            public Nullable<int> StatusID { get; set; }
            public Requests Request { get; set; }
            public string WorkTypeName { get; set; }
        }

        private void FilterByStatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SortByDeadlineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortByDeadlineComboBox.SelectedIndex == 0) 
            {
                return;
            }

            ApplyFilters();
        }

        private void FilterByWorkTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }
        private void ApplyFilters()
        {
            var selectedStatus = (FilterByStatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            var selectedSort = (SortByDeadlineComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            var selectedWorkType = (FilterByWorkTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            List<RequestViewModel> filteredRequests =  _requests.Select(r => new RequestViewModel
            {
                RequestID = r.RequestID,
                UserID = r.UserID,
                ObjectName = r.ObjectName,
                Address= r.Address,
                Area =r.Area,
                Floors = r.Floors,
                ObjectType = r.ObjectType,
                RoomCount= r.RoomCount,
                Description= r.Description,
                Deadline =r.Deadline,
                ApprovalReason =r.ApprovalReason,
                StatusID=r.StatusID,
                Request = r,
                WorkTypeName = _workTypes.FirstOrDefault(wt => wt.WorkTypeID == r.Purpose)?.WorkTypeName ?? "Не указано"
            }).ToList();
            
            switch (selectedStatus)
            {
                case "Новые":
                    filteredRequests = filteredRequests.Where(r => r.Request.StatusID == 1).ToList();
                    break;
                case "Принятые":
                    filteredRequests = filteredRequests.Where(r => r.Request.StatusID == 2).ToList();
                    break;
                case "Отклоненные":
                    filteredRequests = filteredRequests.Where(r => r.Request.StatusID == 3).ToList();
                    break;
            }

            switch (selectedSort)
            {
                case "Ближайшие":
                    filteredRequests = filteredRequests.OrderBy(r => r.Request.Deadline).ToList();
                    break;
                case "Убывание":
                    filteredRequests = filteredRequests.OrderByDescending(r => r.Request.Deadline).ToList();
                    break;
            }

            if (selectedWorkType != "Все типы работ")
            {
                filteredRequests = filteredRequests.Where(r => r.WorkTypeName == selectedWorkType).ToList();
            }

            RequestList.ItemsSource = filteredRequests;
        }

        private void RequestList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = RequestList.SelectedItem as RequestViewModel;

            if (selectedItem != null)
            {
                _selectedRequest = selectedItem.Request;

                var requestDetails = new List<KeyValuePair<string, string>>();

                if (!string.IsNullOrEmpty(_selectedRequest.ObjectName))
                    requestDetails.Add(new KeyValuePair<string, string>("Название объекта", _selectedRequest.ObjectName));

                if (!string.IsNullOrEmpty(_selectedRequest.Address))
                    requestDetails.Add(new KeyValuePair<string, string>("Адрес", _selectedRequest.Address));

                if (_selectedRequest.Area != 0)
                    requestDetails.Add(new KeyValuePair<string, string>("Площадь", _selectedRequest.Area.ToString()));

                if (_selectedRequest.Floors.HasValue)
                    requestDetails.Add(new KeyValuePair<string, string>("Этажность", _selectedRequest.Floors.ToString()));

                if (!string.IsNullOrEmpty(_selectedRequest.ObjectType))
                    requestDetails.Add(new KeyValuePair<string, string>("Тип объекта", _selectedRequest.ObjectType));

                if (_selectedRequest.Purpose.HasValue)
                {
                    var purposeName = _workTypes.FirstOrDefault(wt => wt.WorkTypeID == _selectedRequest.Purpose)?.WorkTypeName ?? "Не указано";
                    requestDetails.Add(new KeyValuePair<string, string>("Цель", purposeName));
                }

                if (_selectedRequest.RoomCount.HasValue)
                    requestDetails.Add(new KeyValuePair<string, string>("Количество комнат", _selectedRequest.RoomCount.ToString()));

                if (!string.IsNullOrEmpty(_selectedRequest.Description))
                    requestDetails.Add(new KeyValuePair<string, string>("Описание", _selectedRequest.Description));

                requestDetails.Add(new KeyValuePair<string, string>("Дедлайн", _selectedRequest.Deadline.ToString("dd.MM.yyyy")));


                if ((_selectedRequest.StatusID == 2 || _selectedRequest.StatusID == 3) && !string.IsNullOrEmpty(_selectedRequest.ApprovalReason))
                {
                    string type = null;
                    if(_selectedRequest.StatusID == 2)
                    { type = "одобрения"; }
                    else { type = "отказа"; }

                    requestDetails.Add(new KeyValuePair<string, string>("Причина " + type, _selectedRequest.ApprovalReason));
                }

                if (_selectedRequest.StatusID.HasValue)
                {
                    string statusName = _context.Statuses.FirstOrDefault(wt => wt.StatusID == _selectedRequest.StatusID)?.StatusName;
                    requestDetails.Add(new KeyValuePair<string, string>("Статус", statusName));
                }

                RequestDetailsItemsControl.ItemsSource = requestDetails;

                FilterStackPanel.Visibility = Visibility.Collapsed;
                RequestListStackPanel.Visibility = Visibility.Collapsed;
                RequestDetailsStackPanel.Visibility = Visibility.Visible;
                ReasonInputStackPanel.Visibility = Visibility.Collapsed;
                CreateProjectStackPanel.Visibility = Visibility.Collapsed;

                if (_selectedRequest.StatusID == 1)
                {
                    ApproveButton.Visibility = Visibility.Visible;
                    RejectButton.Visibility = Visibility.Visible;
                }
                else
                {
                    ApproveButton.Visibility = Visibility.Collapsed;
                    RejectButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            _isApproving = true; 
            FilterStackPanel.Visibility = Visibility.Collapsed;
            ReasonInputStackPanel.Visibility = Visibility.Visible;
            RequestDetailsStackPanel.Visibility = Visibility.Collapsed;

            LoadManagers();
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            _isApproving = false; 
            FilterStackPanel.Visibility = Visibility.Collapsed;
            ReasonInputStackPanel.Visibility = Visibility.Visible;
            RequestDetailsStackPanel.Visibility = Visibility.Collapsed;
        }

        private void ConfirmReasonButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ReasonTextBox.Text))
            {
                MessageBox.Show("Причина не может быть пустой.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_selectedRequest != null)
            {
                using (var db = new GeotekMetallCompleteEntities1())
                {
                    var request = db.Requests.Find(_selectedRequest.RequestID);
                    if (request != null)
                    {
                        if (_isApproving)
                        {
                            request.ApprovalReason = ReasonTextBox.Text;
                            CreateProjectStackPanel.Visibility = Visibility.Visible;
                            ReasonInputStackPanel.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            request.StatusID = 3; // Отклонено
                            request.ApprovalReason = ReasonTextBox.Text;
                            MessageBox.Show("Заявка отклонена и сохранена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                            CloseAllPanels();
                            ShowRequestList();
                        }

                        db.SaveChanges();
                        LoadData();
                    }
                }
            }
            ReasonTextBox.Text = string.Empty;
        }

        private void CloseAllPanels()
        {
            FilterStackPanel.Visibility = Visibility.Collapsed;
            RequestListStackPanel.Visibility = Visibility.Collapsed;
            RequestDetailsStackPanel.Visibility = Visibility.Collapsed;
            ReasonInputStackPanel.Visibility = Visibility.Collapsed;
            CreateProjectStackPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowRequestList()
        {
            FilterStackPanel.Visibility = Visibility.Visible;
            RequestListStackPanel.Visibility = Visibility.Visible;
            LoadData();
        }

        private void CancelReasonButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPanels();
            RequestDetailsStackPanel.Visibility = Visibility.Visible;
            ReasonTextBox.Text= string.Empty;
        }

        private void CreateProjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRequest != null && ProjectStartDatePicker.SelectedDate != null && ProjectEndDatePicker.SelectedDate != null)
            {
                var selectedManager = ProjectManagerComboBox.SelectedItem as ManagerViewModel;
                if (selectedManager == null)
                {
                    MessageBox.Show("Выберите менеджера проекта.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var startDate = ProjectStartDatePicker.SelectedDate.Value;
                var endDate = ProjectEndDatePicker.SelectedDate.Value;

                if (startDate < DateTime.Today)
                {
                    MessageBox.Show("Дата начала не может быть в прошлом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (startDate > DateTime.Today.AddYears(3))
                {
                    MessageBox.Show("Дата начала не может быть более чем через 3 года.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if ((endDate - startDate).TotalDays < 2)
                {
                    MessageBox.Show("Между началом и концом проекта должно быть не менее 2 дней.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (var db = new GeotekMetallCompleteEntities1())
                {
                    var project = new Projects
                    {
                        RequestID = _selectedRequest.RequestID,
                        ProjectStartDate = startDate,
                        ProjectEndDate = endDate,
                        ProjectManagerID = selectedManager.UserID 
                    };

                    db.Projects.Add(project);

                    var request = db.Requests.Find(_selectedRequest.RequestID);
                    if (request != null)
                    {
                        request.StatusID = 2; // Принято
                    }

                    db.SaveChanges();
                    MessageBox.Show("Проект успешно создан.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Заполните всю информацию.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CloseAllPanels();
            ShowRequestList();
            LoadData();
        }

        public class ManagerViewModel
        {
            public int UserID { get; set; }
            public string FullName { get; set; }
        }

        private void LoadManagers()
        {
            using (var db = new GeotekMetallCompleteEntities1())
            {
                var managers = db.Users
                    .Where(u => u.UserRoles.Any(ur => ur.RoleID == 1))
                    .Select(u => new ManagerViewModel
                    {
                        UserID = u.UserID,
                        FullName = u.FirstName + " " + u.LastName 
                    })
                    .ToList();

                ProjectManagerComboBox.ItemsSource = managers;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPanels();
            ShowRequestList();
            LoadData();
        }

        private void CancelProjectButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllPanels();
            RequestDetailsStackPanel.Visibility = Visibility.Visible;
        }
    }
}


