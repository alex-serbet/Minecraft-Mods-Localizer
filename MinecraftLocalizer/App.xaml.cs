using MinecraftLocalizer.Properties;
using System.Globalization;
using System.Windows;

namespace MinecraftLocalizer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }

       
        public void SetCulture(string cultureCode)
        {
            CultureInfo culture = new(cultureCode);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            Settings.Default.ProgramLanguage = cultureCode;
            Settings.Default.Save();
        }

        private static void LoadCulture()
        {
            string savedCulture = Settings.Default.ProgramLanguage;

            if (savedCulture != "ru")
            {
                savedCulture = "en";
            }

            if (!string.IsNullOrEmpty(savedCulture))
            {
                CultureInfo culture = new(savedCulture);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }
        }
    }
}
