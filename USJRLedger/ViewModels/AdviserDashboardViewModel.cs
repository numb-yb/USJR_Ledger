using System.Collections.ObjectModel;
using System.Windows.Input;
using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.ViewModels
{
    public class NotificationItemViewModel
    {
        public string Detail { get; set; }
        public string DateString { get; set; }
    }

    public class AdviserDashboardViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly OrganizationService _organizationService;
        private readonly SchoolYearService _schoolYearService;
        private readonly TransactionService _transactionService;
        private readonly UserService _userService;

        private string _welcomeMessage;
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        private string _organizationInfo;
        public string OrganizationInfo
        {
            get => _organizationInfo;
            set => SetProperty(ref _organizationInfo, value);
        }

        private string _schoolYearStatus;
        public string SchoolYearStatus
        {
            get => _schoolYearStatus;
            set => SetProperty(ref _schoolYearStatus, value);
        }

        private Color _schoolYearStatusColor;
        public Color SchoolYearStatusColor
        {
            get => _schoolYearStatusColor;
            set => SetProperty(ref _schoolYearStatusColor, value);
        }

        private string _balance;
        public string Balance
        {
            get => _balance;
            set => SetProperty(ref _balance, value);
        }

        private string _pendingExpenses;
        public string PendingExpenses
        {
            get => _pendingExpenses;
            set => SetProperty(ref _pendingExpenses, value);
        }

        private ObservableCollection<NotificationItemViewModel> _notifications;
        public ObservableCollection<NotificationItemViewModel> Notifications
        {
            get => _notifications;
            set => SetProperty(ref _notifications, value);
        }

        public Organization CurrentOrganization { get; private set; }
        public SchoolYear CurrentSchoolYear { get; private set; }

        public ICommand LogoutCommand { get; }
        public ICommand ViewOrgProfileCommand { get; }
        public ICommand ManageOfficersCommand { get; }
        public ICommand SchoolYearCommand { get; }
        public ICommand ExpenseRequestsCommand { get; }
        public ICommand EventsCommand { get; }

        public AdviserDashboardViewModel(AuthService authService, DataService dataService)
        {
            _authService = authService;
            _dataService = dataService;
            _organizationService = new OrganizationService(dataService);
            _schoolYearService = new SchoolYearService(dataService);
            _transactionService = new TransactionService(dataService);

            WelcomeMessage = $"Welcome, {_authService.CurrentUser.Name}";
            Notifications = new ObservableCollection<NotificationItemViewModel>();

            LogoutCommand = new Command(() => _authService.Logout());
            ViewOrgProfileCommand = new Command(OnViewOrgProfile);
            ManageOfficersCommand = new Command(OnManageOfficers);
            SchoolYearCommand = new Command(OnSchoolYear);
            ExpenseRequestsCommand = new Command(OnExpenseRequests);
            EventsCommand = new Command(OnEvents);

            LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            var organizations = await _organizationService.GetAllOrganizationsAsync();
            CurrentOrganization = organizations.FirstOrDefault(o => o.Id == _authService.CurrentUser.OrganizationId);

            if (CurrentOrganization != null)
            {
                OrganizationInfo = $"Organization: {CurrentOrganization.Name} ({CurrentOrganization.Department})";

                var schoolYears = await _schoolYearService.GetSchoolYearsByOrganizationAsync(CurrentOrganization.Id);
                CurrentSchoolYear = schoolYears.FirstOrDefault(sy => sy.IsActive);

                if (CurrentSchoolYear != null)
                {
                    SchoolYearStatus = $"Active: {CurrentSchoolYear.Semester} {CurrentSchoolYear.Year}";
                    SchoolYearStatusColor = Colors.Green;
                }
                else
                {
                    SchoolYearStatus = "No Active School Year";
                    SchoolYearStatusColor = Colors.Red;
                }

                decimal orgBalance = await _organizationService.GetOrganizationBalanceAsync(CurrentOrganization.Id);
                Balance = $"₱ {orgBalance:N2}";

                var pendingExpensesList = await _transactionService.GetPendingExpensesAsync(CurrentOrganization.Id);
                PendingExpenses = pendingExpensesList.Count.ToString();

                Notifications.Clear();
                foreach (var expense in pendingExpensesList.OrderByDescending(e => e.CreatedDate).Take(5))
                {
                    var user = await _userService.GetUserByIdAsync(expense.CreatedBy);
                    Notifications.Add(new NotificationItemViewModel
                    {
                        Detail = $"Expense Request: {expense.Detail} - ₱{expense.Amount:N2} by {user?.Name ?? "Unknown"}",
                        DateString = expense.CreatedDate.ToString("MMM dd, yyyy")
                    });
                }
            }
            else
            {
                OrganizationInfo = "No Organization Assigned";
                SchoolYearStatus = "N/A";
                Balance = "₱ 0.00";
                PendingExpenses = "0";
            }
        }

        private void OnViewOrgProfile()
        {
            // Navigation will be handled in the view
        }

        private void OnManageOfficers()
        {
            // Navigation will be handled in the view
        }

        private void OnSchoolYear()
        {
            // Navigation will be handled in the view
        }

        private void OnExpenseRequests()
        {
            // Navigation will be handled in the view
        }

        private void OnEvents()
        {
            // Navigation will be handled in the view
        }
    }
}