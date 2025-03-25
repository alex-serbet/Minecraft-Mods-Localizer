using MinecraftLocalizer.Properties;
using System.Globalization;
using System.Windows;

namespace MinecraftLocalizer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            LoadCulture();
            base.OnStartup(e);
        }

        private static void LoadCulture()
        {
            string savedCulture = Settings.Default.ProgramLanguage;

            CultureInfo culture = new(savedCulture);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}
