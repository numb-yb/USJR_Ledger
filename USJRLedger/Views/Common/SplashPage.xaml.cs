using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using USJRLedger.Services;
using USJRLedger.Views.Common; // for LoginPage

namespace USJRLedger.Views.Common
{
    public partial class SplashPage : ContentPage
    {
        private StackLayout mainLayout;
        private Image logoImage;
        private Label titleLabel;
        private Label subtitleLabel;
        private ActivityIndicator loadingIndicator;

        public SplashPage()
        {
            CreateUI();
        }

        private void CreateUI()
        {
            try
            {
                // Updated color to match your app’s green theme
                BackgroundColor = Color.FromArgb("#008000");

                mainLayout = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 20
                };

                // Logo image
                logoImage = new Image
                {
                    Source = "splash_logo.png",
                    HeightRequest = 150,
                    WidthRequest = 150,
                    HorizontalOptions = LayoutOptions.Center,
                    Opacity = 0 // start hidden for animation
                };
                mainLayout.Children.Add(logoImage);

                // Title
                titleLabel = new Label
                {
                    Text = "USJR LEDGER",
                    FontSize = 32,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 20, 0, 10),
                    Opacity = 0
                };
                mainLayout.Children.Add(titleLabel);

                // Subtitle
                subtitleLabel = new Label
                {
                    Text = "Organization Management System",
                    FontSize = 18,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 0, 0, 30),
                    Opacity = 0
                };
                mainLayout.Children.Add(subtitleLabel);

                // Loading Indicator
                loadingIndicator = new ActivityIndicator
                {
                    IsRunning = true,
                    Color = Colors.White,
                    HorizontalOptions = LayoutOptions.Center
                };
                mainLayout.Children.Add(loadingIndicator);

                Content = mainLayout;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating UI: {ex.Message}");
                Content = new Label
                {
                    Text = "Loading...",
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                //Animate splash elements
                await logoImage.FadeTo(1, 1000, Easing.CubicInOut);
                await titleLabel.FadeTo(1, 700, Easing.CubicInOut);
                await subtitleLabel.FadeTo(1, 700, Easing.CubicInOut);
                await Task.Delay(1200); // stay visible for a moment

                // Proper navigation to LoginPage (no Shell)
                var dataService = new DataService();
                var authService = new AuthService(dataService);

                // Replace the main page with a fresh LoginPage wrapped in NavigationPage
                Application.Current.MainPage = new NavigationPage(new LoginPage(authService));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Navigation error: {ex.Message}");

                // fallback
                Application.Current.MainPage = new ContentPage
                {
                    BackgroundColor = Colors.White,
                    Content = new VerticalStackLayout
                    {
                        Spacing = 20,
                        Padding = new Thickness(20),
                        VerticalOptions = LayoutOptions.Center,
                        Children =
                        {
                            new Label
                            {
                                Text = "USJR Ledger",
                                FontSize = 24,
                                FontAttributes = FontAttributes.Bold,
                                HorizontalOptions = LayoutOptions.Center
                            },
                            new Label
                            {
                                Text = "Please restart the application",
                                HorizontalOptions = LayoutOptions.Center
                            },
                            new Button
                            {
                                Text = "Exit",
                                HorizontalOptions = LayoutOptions.Center,
                                Command = new Command(() => Environment.Exit(0))
                            }
                        }
                    }
                };
            }
        }
    }
}
