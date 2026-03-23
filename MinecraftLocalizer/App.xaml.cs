using MinecraftLocalizer.Interfaces.Ai;
using MinecraftLocalizer.Models.Composition;
using MinecraftLocalizer.Properties;
using MinecraftLocalizer.Views;
using System.Diagnostics;
using System.Globalization;
using System.Windows;

namespace MinecraftLocalizer
{
    public partial class App : System.Windows.Application
    {
        private readonly ApplicationBootstrapper _bootstrapper = new();
        private IGpt4FreeService? _gpt4FreeService;

        protected override void OnStartup(StartupEventArgs e)
        {
            LoadCulture();
            base.OnStartup(e);

            MainWindow window = _bootstrapper.CreateMainWindow();
            _gpt4FreeService = _bootstrapper.Gpt4FreeService;
            MainWindow = window;
            window.Show();
        }

        private static void LoadCulture()
        {
            string savedCulture = Settings.Default.ProgramLanguage;

            CultureInfo culture = new(savedCulture);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _gpt4FreeService?.Shutdown();
                _gpt4FreeService?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error killing GPT4Free process on exit: {ex.Message}");
            }

            base.OnExit(e);
        }
    }
}

