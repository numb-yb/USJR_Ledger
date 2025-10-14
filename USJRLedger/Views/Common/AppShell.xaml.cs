using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Xml;

namespace USJRLedger
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            // Manual implementation of InitializeComponent
            ManualInitializeComponent();

            // Register routes for navigation
            RegisterRoutes();
        }

        private void ManualInitializeComponent()
        {
            try
            {
                // Load the XAML manually
                var assembly = GetType().Assembly;
                string resourceName = "USJRLedger.AppShell.xaml";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        // If the resource exists, load it
                     
                    }
                    else
                    {
                        // If resource doesn't exist, create a basic shell structure programmatically
                        Console.WriteLine($"Warning: AppShell.xaml not found as embedded resource. Creating shell programmatically.");

                        // Set Shell properties programmatically
                        FlyoutBehavior = FlyoutBehavior.Disabled;
                        BackgroundColor = Microsoft.Maui.Graphics.Colors.White;

                        // Create ShellContent for LoginPage
                        var loginContent = new ShellContent
                        {
                            Title = "Login",
                            Route = "LoginPage"
                        };

                        // Add content to shell
                        Items.Add(loginContent);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ManualInitializeComponent: {ex.Message}");
            }
        }

        private void RegisterRoutes()
        {
            // Register main routes - using fully qualified names to avoid reference issues
            try
            {
                Routing.RegisterRoute("LoginPage", typeof(USJRLedger.Views.Common.LoginPage));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering routes: {ex.Message}");
            }
        }
    }
}