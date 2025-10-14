using System.Collections.ObjectModel;
using System.Windows.Input;
using USJRLedger.Models;
using USJRLedger.Services;

namespace USJRLedger.ViewModels
{
    public class AdviserItemViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string OrganizationName { get; set; }
        public bool IsActive { get; set; }
        public string Status => IsActive ? "Active" : "Inactive";
        public Color StatusColor => IsActive ? Colors.Green : Colors.Red;
    }

    public class ManageAdvisersViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        private readonly UserService _userService;
        private readonly OrganizationService _organizationService;

        private ObservableCollection<AdviserItemViewModel> _advisers;
        public ObservableCollection<AdviserItemViewModel> Advisers
        {
            get => _advisers;
            set => SetProperty(ref _advisers, value);
        }

        private ObservableCollection<Organization> _organizations;
        public ObservableCollection<Organization> Organizations
        {
            get => _organizations;
            set => SetProperty(ref _organizations, value);
        }

        private Organization _selectedOrganization;
        public Organization SelectedOrganization
        {
            get => _selectedOrganization;
            set
            {
                if (SetProperty(ref _selectedOrganization, value))
                {
                    OrganizationId = value?.Id;
                }
            }
        }

        // New adviser properties
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string _organizationId;
        public string OrganizationId
        {
            get => _organizationId;
            set => SetProperty(ref _organizationId, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private Color _statusColor;
        public Color StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        public ICommand AddAdviserCommand { get; }
        public ICommand ToggleStatusCommand { get; }
        public ICommand RefreshCommand { get; }

        public ManageAdvisersViewModel(AuthService authService, DataService dataService)
        {
            _authService = authService;
            _userService = new UserService(dataService);
            _organizationService = new OrganizationService(dataService);

            Advisers = new ObservableCollection<AdviserItemViewModel>();
            Organizations = new ObservableCollection<Organization>();

            AddAdviserCommand = new Command(async () => await AddAdviserAsync());
            ToggleStatusCommand = new Command<string>(async (id) => await ToggleAdviserStatusAsync(id));
            RefreshCommand = new Command(async () => await LoadDataAsync());

            LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            IsBusy = true;

            try
            {
                var organizations = await _organizationService.GetAllOrganizationsAsync();
                Organizations.Clear();
                foreach (var org in organizations.Where(o => o.IsActive).OrderBy(o => o.Name))
                {
                    Organizations.Add(org);
                }

                var advisers = await _userService.GetUsersByRoleAsync(UserRole.Adviser);
                var adviserViewModels = new List<AdviserItemViewModel>();

                foreach (var adviser in advisers.OrderBy(a => a.Name))
                {
                    var org = organizations.FirstOrDefault(o => o.Id == adviser.OrganizationId);

                    adviserViewModels.Add(new AdviserItemViewModel
                    {
                        Id = adviser.Id,
                        Name = adviser.Name,
                        Username = adviser.Username,
                        OrganizationName = org?.Name ?? "Unknown Organization",
                        IsActive = adviser.IsActive
                    });
                }

                Advisers.Clear();
                foreach (var adviser in adviserViewModels)
                {
                    Advisers.Add(adviser);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading data: {ex.Message}";
                StatusColor = Colors.Red;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task AddAdviserAsync()
        {
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(OrganizationId))
            {
                StatusMessage = "Please fill all fields";
                StatusColor = Colors.Red;
                return;
            }

            IsBusy = true;

            try
            {
                await _userService.CreateAdviserAsync(Name, Username, Password, OrganizationId);

                StatusMessage = "Adviser added successfully";
                StatusColor = Colors.Green;

                // Clear the form
                Name = string.Empty;
                Username = string.Empty;
                Password = string.Empty;

                // Refresh the list
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding adviser: {ex.Message}";
                StatusColor = Colors.Red;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ToggleAdviserStatusAsync(string adviserId)
        {
            if (string.IsNullOrEmpty(adviserId))
                return;

            IsBusy = true;

            try
            {
                var adviser = Advisers.FirstOrDefault(a => a.Id == adviserId);
                if (adviser != null)
                {
                    await _userService.UpdateUserStatusAsync(adviserId, !adviser.IsActive);

                    // Refresh the list
                    await LoadDataAsync();

                    StatusMessage = "Adviser status updated successfully";
                    StatusColor = Colors.Green;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating adviser status: {ex.Message}";
                StatusColor = Colors.Red;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}