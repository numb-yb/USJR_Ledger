using System.Collections.ObjectModel;
using System.Windows.Input;
using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.ViewModels
{
    public class TransactionItemViewModel
    {
        public string Detail { get; set; }
        public string DateString { get; set; }
        public string AmountString { get; set; }
        public string StatusString { get; set; }
        public Color StatusColor { get; set; }
    }

    public class OfficerDashboardViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly OrganizationService _organizationService;
        private readonly SchoolYearService _schoolYearService;
        private readonly TransactionService _transactionService;

        private string _welcomeMessage;
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        private string _positionInfo;
        public string PositionInfo
        {
            get => _positionInfo;
            set => SetProperty(ref _positionInfo, value);
        }

        private string _organizationInfo;
        public string OrganizationInfo
        {
            get => _organizationInfo;
            set => SetProperty(ref _organizationInfo, value);
        }

        private string _schoolYearInfo;
        public string SchoolYearInfo
        {
            get => _schoolYearInfo;
            set => SetProperty(ref _schoolYearInfo, value);
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

        private ObservableCollection<TransactionItemViewModel> _recentTransactions;
        public ObservableCollection<TransactionItemViewModel> RecentTransactions
        {
            get => _recentTransactions;
            set => SetProperty(ref _recentTransactions, value);
        }

        public Organization CurrentOrganization { get; private set; }
        public SchoolYear CurrentSchoolYear { get; private set; }

        public ICommand LogoutCommand { get; }
        public ICommand ViewOrgProfileCommand { get; }
        public ICommand CreateEventCommand { get; }
        public ICommand AddExpenseCommand { get; }
        public ICommand AddIncomeCommand { get; }

        public OfficerDashboardViewModel(AuthService authService, DataService dataService)
        {
            _authService = authService;
            _dataService = dataService;
            _organizationService = new OrganizationService(dataService);
            _schoolYearService = new SchoolYearService(dataService);
            _transactionService = new TransactionService(dataService);

            WelcomeMessage = $"Welcome, {_authService.CurrentUser.Name}";
            PositionInfo = $"Position: {_authService.CurrentUser.Position}";
            RecentTransactions = new ObservableCollection<TransactionItemViewModel>();

            LogoutCommand = new Command(() => _authService.Logout());
            ViewOrgProfileCommand = new Command(OnViewOrgProfile);
            CreateEventCommand = new Command(OnCreateEvent);
            AddExpenseCommand = new Command(OnAddExpense);
            AddIncomeCommand = new Command(OnAddIncome);

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
                    SchoolYearInfo = $"{CurrentSchoolYear.Semester} {CurrentSchoolYear.Year}";
                }
                else
                {
                    SchoolYearInfo = "No Active School Year";
                }

                decimal orgBalance = await _organizationService.GetOrganizationBalanceAsync(CurrentOrganization.Id);
                Balance = $"₱ {orgBalance:N2}";

                var transactions = await _transactionService.GetTransactionsByOrganizationAsync(CurrentOrganization.Id);
                int pendingExpensesCount = transactions.Count(t => t.Type == TransactionType.Expense &&
                                                                t.ApprovalStatus == ApprovalStatus.Pending &&
                                                                t.CreatedBy == _authService.CurrentUser.Id);
                PendingExpenses = pendingExpensesCount.ToString();

                RecentTransactions.Clear();
                foreach (var transaction in transactions.Where(t => t.CreatedBy == _authService.CurrentUser.Id)
                                                     .OrderByDescending(t => t.CreatedDate)
                                                     .Take(10))
                {
                    RecentTransactions.Add(new TransactionItemViewModel
                    {
                        Detail = $"{(transaction.Type == TransactionType.Income ? "Income" : "Expense")}: {transaction.Detail}",
                        DateString = transaction.CreatedDate.ToString("MMM dd, yyyy"),
                        AmountString = $"₱ {transaction.Amount:N2}",
                        StatusString = transaction.ApprovalStatus.ToString(),
                        StatusColor = transaction.ApprovalStatus switch
                        {
                            ApprovalStatus.Approved => Colors.Green,
                            ApprovalStatus.Rejected => Colors.Red,
                            _ => Colors.Orange
                        }
                    });
                }
            }
            else
            {
                OrganizationInfo = "No Organization Assigned";
                SchoolYearInfo = "N/A";
                Balance = "₱ 0.00";
                PendingExpenses = "0";
            }
        }

        private void OnViewOrgProfile()
        {
            // Navigation will be handled in the view
        }

        private void OnCreateEvent()
        {
            // Navigation will be handled in the view
        }

        private void OnAddExpense()
        {
            // Navigation will be handled in the view
        }

        private void OnAddIncome()
        {
            // Navigation will be handled in the view
        }
    }
}