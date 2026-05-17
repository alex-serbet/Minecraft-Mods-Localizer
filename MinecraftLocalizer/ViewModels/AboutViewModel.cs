using MinecraftLocalizer.Commands;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace MinecraftLocalizer.ViewModels
{
    public sealed class AboutViewModel : ViewModelBase
    {
        public ICommand CloseWindowCommand { get; private set; }
        public ICommand OpenGitHubCommand { get; private set; }
        public ICommand OpenSupportCommand { get; private set; }

        public string AppVersion { get; } = string.Format(
            Properties.Resources.Version + " {0}",
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0");

        public AboutViewModel()
        {
            CloseWindowCommand = new RelayCommand<object>(CloseWindow);
            OpenGitHubCommand = new RelayCommand(OpenGitHub);
            OpenSupportCommand = new RelayCommand(OpenSupport);
        }

        private void OpenGitHub()
        {
            Process.Start(new ProcessStartInfo("https://github.com/alex-serbet/Minecraft-Mods-Localizer") { UseShellExecute = true });
        }

        private void OpenSupport()
        {
            Process.Start(new ProcessStartInfo("https://t.me/alex_serbet") { UseShellExecute = true });
        }

        private void CloseWindow(object? parameter)
        {
            if (parameter is Window window)
            {
                window.Close();
            }
        }
    }
}
