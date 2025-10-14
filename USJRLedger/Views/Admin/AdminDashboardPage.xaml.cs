using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using USJRLedger.Models;
using USJRLedger.Services;
using USJRLedger.Views.Common;
using USJRLedger.Views.Adviser;

namespace USJRLedger.Views.Admin
{
    public partial class AdminDashboardPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DataService _dataService;
        private readonly SystemResetService _resetService;

        // UI elements
        private Label WelcomeLabel;
        private Label OrgCountLabel;
        private Label ActiveOrgCountLabel;
        private Label AdviserCountLabel;
        private Label TransactionCountLabel;
        private Label EventCountLabel;

        // Layouts that can change orientation dynamically
        private StackLayout ActionButtonsLayout;
        private StackLayout SystemButtonsLayout;

        public AdminDashboardPage(AuthService authService)
        {
            _authService = authService;
            _dataService = new DataService();
            _resetService = new SystemResetService(_dataService);

            CreateUI();

            WelcomeLabel.Text = $"Welcome, {_authService.CurrentUser?.Name ?? "Administrator"}";

            LoadStatsAsync();

            // Handle layout orientation changes
            SizeChanged += OnPageSizeChanged;
        }

        private void CreateUI()
        {
            Title = "Admin Dashboard";

            var grid = new Grid { Padding = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            var topSection = new VerticalStackLayout { Spacing = 20 };

            WelcomeLabel = new Label
            {
                FontSize = 24,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center
            };
            topSection.Children.Add(WelcomeLabel);

            // Action Buttons
            ActionButtonsLayout = new StackLayout { Spacing = 12 };
            AddActionButtons(ActionButtonsLayout);
            topSection.Children.Add(ActionButtonsLayout);

            // System Buttons
            SystemButtonsLayout = new StackLayout { Spacing = 12, Margin = new Thickness(0, 10, 0, 0) };
            AddSystemButtons(SystemButtonsLayout);
            topSection.Children.Add(SystemButtonsLayout);

            grid.Add(topSection, 0, 0);

            // Stats Frame
            var statsFrame = CreateStatsFrame();
            grid.Add(statsFrame, 0, 1);

            Content = new ScrollView { Content = grid };
        }

        private void AddActionButtons(StackLayout layout)
        {
            var createOrgButton = CreatePrimaryButton("Create Organization", OnCreateOrgClicked);
            var viewOrgsButton = CreatePrimaryButton("View Organizations", OnViewOrgsClicked);
            var manageAdvisersButton = CreatePrimaryButton("Manage Advisers", OnManageAdvisersClicked);

            layout.Children.Add(createOrgButton);
            layout.Children.Add(viewOrgsButton);
            layout.Children.Add(manageAdvisersButton);
        }

        private void AddSystemButtons(StackLayout layout)
        {
            var resetButton = new Button
            {
                Text = "RESET ENTIRE SYSTEM",
                BackgroundColor = Colors.Red,
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 8,
                HeightRequest = 48,
                FontSize = 16,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            resetButton.Clicked += OnCompleteSystemResetClicked;

            var logoutButton = new Button
            {
                Text = "Logout",
                BackgroundColor = Colors.DarkRed,
                TextColor = Colors.White,
                CornerRadius = 8,
                HeightRequest = 48,
                FontSize = 16,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            logoutButton.Clicked += OnLogoutClicked;

            layout.Children.Add(resetButton);
            layout.Children.Add(logoutButton);
        }

        private Button CreatePrimaryButton(string text, EventHandler handler)
        {
            var button = new Button
            {
                Text = text,
                BackgroundColor = Color.FromArgb("#5C4DFF"),
                TextColor = Colors.White,
                CornerRadius = 8,
                HeightRequest = 48,
                FontSize = 16,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            button.Clicked += handler;
            return button;
        }

        private Frame CreateStatsFrame()
        {
            var statsFrame = new Frame
            {
                Padding = new Thickness(16),
                BorderColor = Colors.Gray,
                CornerRadius = 12,
                HasShadow = true,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            var statsLayout = new VerticalStackLayout();

            statsLayout.Children.Add(new Label
            {
                Text = "Quick Stats",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            });

            OrgCountLabel = new Label { FontSize = 14, TextColor = Colors.Gray };
            ActiveOrgCountLabel = new Label { FontSize = 14, TextColor = Colors.Gray };
            AdviserCountLabel = new Label { FontSize = 14, TextColor = Colors.Gray };
            TransactionCountLabel = new Label { FontSize = 14, TextColor = Colors.Gray };
            EventCountLabel = new Label { FontSize = 14, TextColor = Colors.Gray };

            statsLayout.Children.Add(OrgCountLabel);
            statsLayout.Children.Add(ActiveOrgCountLabel);
            statsLayout.Children.Add(AdviserCountLabel);
            statsLayout.Children.Add(TransactionCountLabel);
            statsLayout.Children.Add(EventCountLabel);

            statsFrame.Content = statsLayout;
            return statsFrame;
        }

        private async void LoadStatsAsync()
        {
            try
            {
                var organizations = await _dataService.LoadFromFileAsync<Organization>("organizations.json");
                OrgCountLabel.Text = $"Total Organizations: {organizations?.Count ?? 0}";
                ActiveOrgCountLabel.Text = $"Active Organizations: {organizations?.Count(o => o.IsActive) ?? 0}";

                var users = await _dataService.LoadFromFileAsync<User>("users.json");
                AdviserCountLabel.Text = $"Total Advisers: {users?.Count(u => u.Role == UserRole.Adviser) ?? 0}";

                var transactions = await SafeLoadAsync<Transaction>("transactions.json");
                TransactionCountLabel.Text = $"Total Transactions: {transactions?.Count ?? 0}";

                var events = await SafeLoadAsync<Event>("events.json");
                EventCountLabel.Text = $"Total Events: {events?.Count ?? 0}";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load statistics: {ex.Message}", "OK");
            }
        }

        private async Task<List<T>> SafeLoadAsync<T>(string filename)
        {
            try
            {
                return await _dataService.LoadFromFileAsync<T>(filename);
            }
            catch
            {
                return new List<T>();
            }
        }

        private async void OnCompleteSystemResetClicked(object sender, EventArgs e)
        {
            try
            {
                bool confirm = await DisplayAlert(
                    " COMPLETE SYSTEM RESET ",
                    "This will DELETE ALL data including users, organizations, transactions, and events.\n\nThis action cannot be undone.",
                    "Reset Everything", "Cancel");

                if (!confirm)
                {
                    await DisplayAlert("Cancelled", "System reset cancelled.", "OK");
                    return;
                }

                bool secondConfirm = await DisplayAlert(
                    "FINAL CONFIRMATION",
                    "Are you absolutely sure you want to proceed?",
                    "YES, DELETE EVERYTHING", "NO, CANCEL");

                if (!secondConfirm)
                {
                    await DisplayAlert("Cancelled", "System reset cancelled.", "OK");
                    return;
                }

                await DisplayAlert("Processing", "Resetting the system. Please wait...", "OK");

                var results = await PerformSystemReset();

                await DisplayAlert("Reset Complete",
                    $"System reset complete.\n\n" +
                    $"• {results.Item1} organizations removed\n" +
                    $"• {results.Item2} transactions removed\n" +
                    $"• {results.Item3} other files cleared",
                    "OK");

                LoadStatsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Reset failed: {ex.Message}", "OK");
            }
        }

        private async Task<Tuple<int, int, int>> PerformSystemReset()
        {
            int orgCount = 0, transCount = 0, otherCount = 0;

            try
            {
                var orgs = await _dataService.LoadFromFileAsync<Organization>("organizations.json");
                orgCount = orgs?.Count ?? 0;
                await _dataService.SaveToFileAsync(new List<Organization>(), "organizations.json");
            }
            catch { }

            try
            {
                var trans = await _dataService.LoadFromFileAsync<Transaction>("transactions.json");
                transCount = trans?.Count ?? 0;
                await _dataService.SaveToFileAsync(new List<Transaction>(), "transactions.json");
            }
            catch { }

            string[] otherFiles =
            {
                "events.json","budgets.json","expenses.json","income.json",
                "school_years.json","reports.json","notes.json","attendance.json"
            };

            foreach (var file in otherFiles)
            {
                try
                {
                    if (await _dataService.FileExistsAsync(file))
                    {
                        await _dataService.WriteRawJsonAsync(file, "[]");
                        otherCount++;
                    }
                }
                catch { }
            }

            return new Tuple<int, int, int>(orgCount, transCount, otherCount);
        }

        private async void OnCreateOrgClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateOrganizationPage(_authService, _dataService));
        }

        private async void OnViewOrgsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ViewOrganizationsPage(_authService, _dataService));
        }

        private async void OnManageAdvisersClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ManageAdvisersPage(_authService, _dataService));
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            _authService.Logout();
            Application.Current.MainPage = new NavigationPage(new LoginPage(_authService));
        }

        // Called whenever screen orientation changes
        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            UpdateLayoutOrientation();
        }

        private void UpdateLayoutOrientation()
        {
            bool isLandscape = Width > Height;

            if (ActionButtonsLayout != null)
                ActionButtonsLayout.Orientation = isLandscape ? StackOrientation.Horizontal : StackOrientation.Vertical;

            if (SystemButtonsLayout != null)
                SystemButtonsLayout.Orientation = isLandscape ? StackOrientation.Horizontal : StackOrientation.Vertical;
        }
    }
}
