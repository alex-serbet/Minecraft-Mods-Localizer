using MinecraftLocalizer.Commands;
using MinecraftLocalizer.Models.Services;
using MinecraftLocalizer.Views;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace MinecraftLocalizer.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        public class Language
        {
            public string? Content { get; set; }
            public string? Tag { get; set; }
        }


        [GeneratedRegex(@"\{\d+\}")]
        private static partial Regex PromptVariableRegex();


        public event Action<bool>? SettingsClosed;
        public ObservableCollection<Language> Languages { get; set; }
        public ObservableCollection<Language> ProgramLanguages { get; set; }


        private string _selectedSourceLanguage;
        public string SelectedSourceLanguage
        {
            get => _selectedSourceLanguage;
            set => SetProperty(ref _selectedSourceLanguage, value);
        }

        private string _selectedTargetLanguage;
        public string SelectedTargetLanguage
        {
            get => _selectedTargetLanguage;
            set => SetProperty(ref _selectedTargetLanguage, value);
        }

        private string _selectedProgramLanguage;
        public string SelectedProgramLanguage
        {
            get => _selectedProgramLanguage;
            set => SetProperty(ref _selectedProgramLanguage, value);
        }

        private string _directoryPath;
        public string DirectoryPath
        {
            get => _directoryPath;
            set => SetProperty(ref _directoryPath, value);
        }

        private string _prompt;
        public string Prompt
        {
            get => _prompt;
            set => SetProperty(ref _prompt, value);
        }


        public ICommand SaveSettingsCommand { get; private set; }
        public ICommand OpenAboutWindowCommand { get; private set; }
        public ICommand CloseWindowSettingsCommand { get; private set; }
        public ICommand SelectDirectoryPathCommand { get; private set; }


        public SettingsViewModel()
        {
            Languages = [.. GetLanguages()];
            ProgramLanguages = [.. GetProgramLanguages()];

            _selectedSourceLanguage = Properties.Settings.Default.SourceLanguage;
            _selectedTargetLanguage = Properties.Settings.Default.TargetLanguage;
            _selectedProgramLanguage = Properties.Settings.Default.ProgramLanguage;
            _directoryPath = Properties.Settings.Default.DirectoryPath;
            _prompt = Properties.Settings.Default.Prompt;

            SaveSettingsCommand = new RelayCommand<object>(SaveSettings);
            OpenAboutWindowCommand = new RelayCommand(OpenAboutWindow);
            CloseWindowSettingsCommand = new RelayCommand<object>(CloseWindowSettings);
            SelectDirectoryPathCommand = new RelayCommand<object>(SelectDirectoryPath);
        }

        private void SaveSettings(object? parameter)
        {
            var oldSource = Properties.Settings.Default.SourceLanguage;
            var oldTarget = Properties.Settings.Default.TargetLanguage;
            var oldDir = Properties.Settings.Default.DirectoryPath;

            bool isLanguageChanged = SelectedProgramLanguage != Properties.Settings.Default.ProgramLanguage;

            int count = PromptVariableRegex().Matches(Prompt).Count;
            if (count > 1)
            {
                DialogService.ShowError(Properties.Resources.InvalidPromptMessage);
                return;
            }

            Properties.Settings.Default.SourceLanguage = SelectedSourceLanguage;
            Properties.Settings.Default.TargetLanguage = SelectedTargetLanguage;
            Properties.Settings.Default.ProgramLanguage = SelectedProgramLanguage;
            Properties.Settings.Default.DirectoryPath = DirectoryPath;
            Properties.Settings.Default.Prompt = Prompt;
            Properties.Settings.Default.Save();

            var newCulture = new CultureInfo(Properties.Settings.Default.ProgramLanguage);
            Thread.CurrentThread.CurrentUICulture = newCulture;

            string message = Properties.Resources.SavedSettingsMessage;

            if (isLanguageChanged)
            {
                message += "\n" + Properties.Resources.RestartRequiredMessage;
            }

            DialogService.ShowSuccess(message);

            bool directoryChanged = oldDir != Properties.Settings.Default.DirectoryPath;

            (parameter as Window)?.Close();

            SettingsClosed?.Invoke(directoryChanged);
        }

        private void OpenAboutWindow()
        {
            DialogService.ShowDialog<AboutView>(System.Windows.Application.Current.MainWindow, new AboutViewModel());
        }

        private void CloseWindowSettings(object? parameter)
        {
            if (parameter is Window settingsWindow)
            {
                settingsWindow.Close();
                SettingsClosed?.Invoke(false);
            }
        }

        private void SelectDirectoryPath(object? parameter)
        {
            using var folderBrowserDialog = new FolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = folderBrowserDialog.SelectedPath;

                DirectoryPath = selectedPath;
            }
        }

        private static Language[] GetProgramLanguages()
        {
            return
             [
                new Language { Content = "English", Tag = "en_us" },
                new Language { Content = "Русский", Tag = "ru_ru" },
                new Language { Content = "Українська", Tag = "uk_ua" },
                new Language { Content = "简体中文", Tag = "zh-Hans" },
                new Language { Content = "日本語", Tag = "ja_JP" }
             ];
        }

        private static Language[] GetLanguages()
        {
            return
            [
                new Language { Content = "[af_za] Afrikaans", Tag = "af_za" },
                new Language { Content = "[ar_sa] العربية", Tag = "ar_sa" },
                new Language { Content = "[ast_es] Asturianu", Tag = "ast_es" },
                new Language { Content = "[az_az] Azərbaycanca", Tag = "az_az" },
                new Language { Content = "[ba_ru] Башҡортса", Tag = "ba_ru" },
                new Language { Content = "[bar] Boarisch", Tag = "bar" },
                new Language { Content = "[be_by] Беларуская", Tag = "be_by" },
                new Language { Content = "[be_latn] Biełaruskaja", Tag = "be_latn" },
                new Language { Content = "[bg_bg] Български", Tag = "bg_bg" },
                new Language { Content = "[br_fr] Brezhoneg", Tag = "br_fr" },
                new Language { Content = "[brb] Braobans", Tag = "brb" },
                new Language { Content = "[bs_ba] Bosanski", Tag = "bs_ba" },
                new Language { Content = "[ca_es] Català", Tag = "ca_es" },
                new Language { Content = "[cs_cz] Čeština", Tag = "cs_cz" },
                new Language { Content = "[cy_gb] Cymraeg", Tag = "cy_gb" },
                new Language { Content = "[da_dk] Dansk", Tag = "da_dk" },
                new Language { Content = "[de_at] Deitsch", Tag = "de_at" },
                new Language { Content = "[de_ch] Schwiizerdutsch", Tag = "de_ch" },
                new Language { Content = "[de_de] Deutsch", Tag = "de_de" },
                new Language { Content = "[el_gr] Ελληνικά", Tag = "el_gr" },
                new Language { Content = "[en_au] English", Tag = "en_au" },
                new Language { Content = "[en_ca] English", Tag = "en_ca" },
                new Language { Content = "[en_gb] English", Tag = "en_gb" },
                new Language { Content = "[en_nz] English", Tag = "en_nz" },
                new Language { Content = "[en_us] English", Tag = "en_us" },
                new Language { Content = "[enp] Anglish", Tag = "enp" },
                new Language { Content = "[eo_uy] Esperanto", Tag = "eo_uy" },
                new Language { Content = "[es_ar] Español", Tag = "es_ar" },
                new Language { Content = "[es_cl] Español", Tag = "es_cl" },
                new Language { Content = "[es_ec] Español", Tag = "es_ec" },
                new Language { Content = "[es_es] Español", Tag = "es_es" },
                new Language { Content = "[es_mx] Español", Tag = "es_mx" },
                new Language { Content = "[es_uy] Español", Tag = "es_uy" },
                new Language { Content = "[es_ve] Español", Tag = "es_ve" },
                new Language { Content = "[esan] Andalûh", Tag = "esan" },
                new Language { Content = "[et_ee] Eesti", Tag = "et_ee" },
                new Language { Content = "[eu_es] Euskara", Tag = "eu_es" },
                new Language { Content = "[fa_ir] فارسی", Tag = "fa_ir" },
                new Language { Content = "[fi_fi] Suomi", Tag = "fi_fi" },
                new Language { Content = "[fil_ph] Filipino", Tag = "fil_ph" },
                new Language { Content = "[fo_fo] Føroyskt", Tag = "fo_fo" },
                new Language { Content = "[fr_ca] Français", Tag = "fr_ca" },
                new Language { Content = "[fr_fr] Français", Tag = "fr_fr" },
                new Language { Content = "[fra_de] Fränggisch", Tag = "fra_de" },
                new Language { Content = "[fur_it] Furlan", Tag = "fur_it" },
                new Language { Content = "[fy_nl] Frysk", Tag = "fy_nl" },
                new Language { Content = "[ga_ie] Gaeilge", Tag = "ga_ie" },
                new Language { Content = "[gd_gb] Gàidhlig", Tag = "gd_gb" },
                new Language { Content = "[gl_es] Galego", Tag = "gl_es" },
                new Language { Content = "[haw_us] 'Ōlelo Hawai'i", Tag = "haw_us" },
                new Language { Content = "[he_il] עברית", Tag = "he_il" },
                new Language { Content = "[hi_in] हिंदी", Tag = "hi_in" },
                new Language { Content = "[hn_no] Høgnorsk", Tag = "hn_no" },
                new Language { Content = "[hr_hr] Hrvatski", Tag = "hr_hr" },
                new Language { Content = "[hu_hu] Magyar", Tag = "hu_hu" },
                new Language { Content = "[hy_am] Հայերեն", Tag = "hy_am" },
                new Language { Content = "[id_id] Bahasa Indonesia", Tag = "id_id" },
                new Language { Content = "[ig_ng] Igbo", Tag = "ig_ng" },
                new Language { Content = "[io_en] Ido", Tag = "io_en" },
                new Language { Content = "[is_is] Íslenska", Tag = "is_is" },
                new Language { Content = "[isv] Medžuslovjansky", Tag = "isv" },
                new Language { Content = "[it_it] Italiano", Tag = "it_it" },
                new Language { Content = "[ja_jp] 日本語", Tag = "ja_jp" },
                new Language { Content = "[jbo_en] la .lojban.", Tag = "jbo_en" },
                new Language { Content = "[ka_ge] ქართული", Tag = "ka_ge" },
                new Language { Content = "[kk_kz] Қазақша", Tag = "kk_kz" },
                new Language { Content = "[kn_in] ಕನ್ನಡ", Tag = "kn_in" },
                new Language { Content = "[ko_kr] 한국어", Tag = "ko_kr" },
                new Language { Content = "[ksh] Kölsch/Ripoarisch", Tag = "ksh" },
                new Language { Content = "[kw_gb] Kernewek", Tag = "kw_gb" },
                new Language { Content = "[ky_kg] Кыргызча", Tag = "ky_kg" },
                new Language { Content = "[la_la] Latina", Tag = "la_la" },
                new Language { Content = "[lb_lu] Lëtzebuergesch", Tag = "lb_lu" },
                new Language { Content = "[li_li] Limburgs", Tag = "li_li" },
                new Language { Content = "[lmo] Lombard", Tag = "lmo" },
                new Language { Content = "[lo_la] ລາວ", Tag = "lo_la" },
                new Language { Content = "[lt_lt] Lietuvių", Tag = "lt_lt" },
                new Language { Content = "[lv_lv] Latviešu", Tag = "lv_lv" },
                new Language { Content = "[lzh] 文言", Tag = "lzh" },
                new Language { Content = "[mk_mk] Македонски", Tag = "mk_mk" },
                new Language { Content = "[mn_mn] Монгол", Tag = "mn_mn" },
                new Language { Content = "[ms_my] Bahasa Melayu", Tag = "ms_my" },
                new Language { Content = "[mt_mt] Malti", Tag = "mt_mt" },
                new Language { Content = "[nah] Mēxikatlahtōlli", Tag = "nah" },
                new Language { Content = "[nds_de] Plattdüütsh", Tag = "nds_de" },
                new Language { Content = "[nl_be] Vlaams", Tag = "nl_be" },
                new Language { Content = "[nl_nl] Nederlands", Tag = "nl_nl" },
                new Language { Content = "[nn_no] Norsk nynorsk", Tag = "nn_no" },
                new Language { Content = "[no_no] Norsk bokmål", Tag = "no_no" },
                new Language { Content = "[oc_fr] Occitan", Tag = "oc_fr" },
                new Language { Content = "[ovd] Övdalska", Tag = "ovd" },
                new Language { Content = "[pl_pl] Polski", Tag = "pl_pl" },
                new Language { Content = "[pls] Ngiiwa", Tag = "pls" },
                new Language { Content = "[pt_br] Português", Tag = "pt_br" },
                new Language { Content = "[pt_pt] Português", Tag = "pt_pt" },
                new Language { Content = "[qya_aa] Quenya", Tag = "qya_aa" },
                new Language { Content = "[ro_ro] Română", Tag = "ro_ro" },
                new Language { Content = "[rpr] Русскій дореформенный", Tag = "rpr" },
                new Language { Content = "[ru_ru] Русский", Tag = "ru_ru" },
                new Language { Content = "[ry_ua] Руснацькый", Tag = "ry_ua" },
                new Language { Content = "[sah_sah] Сахалыы", Tag = "sah_sah" },
                new Language { Content = "[se_no] Davvisámegiella", Tag = "se_no" },
                new Language { Content = "[sk_sk] Slovenčina", Tag = "sk_sk" },
                new Language { Content = "[sl_si] Slovenščina", Tag = "sl_si" },
                new Language { Content = "[so_so] Af-Soomaali", Tag = "so_so" },
                new Language { Content = "[sq_al] Shqip", Tag = "sq_al" },
                new Language { Content = "[sr_cs] Srpski", Tag = "sr_cs" },
                new Language { Content = "[sr_sp] Српски", Tag = "sr_sp" },
                new Language { Content = "[sv_se] Svenska", Tag = "sv_se" },
                new Language { Content = "[sxu] Säggs’sch", Tag = "sxu" },
                new Language { Content = "[szl] Ślōnskŏ", Tag = "szl" },
                new Language { Content = "[ta_in] தமிழ்", Tag = "ta_in" },
                new Language { Content = "[th_th] ไทย", Tag = "th_th" },
                new Language { Content = "[tl_ph] Tagalog", Tag = "tl_ph" },
                new Language { Content = "[tlh_aa] tlhIngan Hol", Tag = "tlh_aa" },
                new Language { Content = "[tok] toki pona", Tag = "tok" },
                new Language { Content = "[tr_tr] Türkçe", Tag = "tr_tr" },
                new Language { Content = "[tt_ru] Татарча", Tag = "tt_ru" },
                new Language { Content = "[tzo_mx] Bats'i k'op", Tag = "tzo_mx" },
                new Language { Content = "[uk_ua] Українська", Tag = "uk_ua" },
                new Language { Content = "[val_es] Català (Valencià)", Tag = "val_es" },
                new Language { Content = "[vec_it] Vèneto", Tag = "vec_it" },
                new Language { Content = "[vi_vn] Tiếng Việt", Tag = "vi_vn" },
                new Language { Content = "[vp_vl] Viossa", Tag = "vp_vl" },
                new Language { Content = "[yi_de] ייִדיש", Tag = "yi_de" },
                new Language { Content = "[yo_ng] Yorùbá", Tag = "yo_ng" },
                new Language { Content = "[zh_cn] 简体中文", Tag = "zh_cn" },
                new Language { Content = "[zh_hk] 繁體中文", Tag = "zh_hk" },
                new Language { Content = "[zh_tw] 繁體中文", Tag = "zh_tw" },
                new Language { Content = "[zlm_arab] بهاس ملايو", Tag = "zlm_arab" }
            ];
        }
    }
}
