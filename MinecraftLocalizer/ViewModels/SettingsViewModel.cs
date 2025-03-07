using MinecraftLocalizer.Commands;
using MinecraftLocalizer.Models.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;

namespace MinecraftLocalizer.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public class Language
        {
            public string? Content { get; set; }
            public string? Tag { get; set; }
        }

        public ObservableCollection<Language> Languages { get; set; }

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
        
        private string _selectedTranslatorMode;
        public string SelectedTranslatorMode
        {
            get => _selectedTranslatorMode;
            set => SetProperty(ref _selectedTranslatorMode, value);
        }

        private string _directoryPath;
        public string DirectoryPath
        {
            get => _directoryPath;
            set => SetProperty(ref _directoryPath, value);
        }

        public ICommand SaveSettingsCommand { get; private set; }
        public ICommand SelectDirectoryPathCommand { get; private set; }


        public SettingsViewModel()
        {
            Languages = new ObservableCollection<Language>(GetLanguages());

            _selectedSourceLanguage = Properties.Settings.Default.SourceLanguage;
            _selectedTargetLanguage = Properties.Settings.Default.TargetLanguage;
            _selectedProgramLanguage = Properties.Settings.Default.ProgramLanguage;
            _selectedTranslatorMode = Properties.Settings.Default.TranslatorMode;
            _directoryPath = Properties.Settings.Default.DirectoryPath;

            SaveSettingsCommand = new RelayCommand<object>(SaveSettings);
            SelectDirectoryPathCommand = new RelayCommand<object>(SelectDirectoryPath);
        }

        private void SaveSettings(object? parameter)
        {
            Properties.Settings.Default.SourceLanguage = SelectedSourceLanguage;
            Properties.Settings.Default.TargetLanguage = SelectedTargetLanguage;
            Properties.Settings.Default.ProgramLanguage = SelectedProgramLanguage;
            Properties.Settings.Default.TranslatorMode = SelectedTranslatorMode;
            Properties.Settings.Default.DirectoryPath = DirectoryPath;

            Properties.Settings.Default.Save();

            DialogService.ShowSuccess("Настройки успешно сохранены!");

            if (parameter is Window dialogWindow)
            {
                dialogWindow.Close();
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

        private static Language[] GetLanguages()
        {
            return
            [

                new Language { Content = "[af_za] Afrikaans (Suid-Afrika)", Tag = "af_za" },
                new Language { Content = "[ar_sa] العربية (العالم العربي)", Tag = "ar_sa" },
                new Language { Content = "[ast_es] Asturianu (Asturies)", Tag = "ast_es" },
                new Language { Content = "[az_az] Azərbaycanca (Azərbaycan)", Tag = "az_az" },
                new Language { Content = "[ba_ru] Башҡортса (Башҡортостан, Рәсәй)", Tag = "ba_ru" },
                new Language { Content = "[bar] Boarisch (Bayern)", Tag = "bar" },
                new Language { Content = "[be_by] Беларуская (Беларусь)", Tag = "be_by" },
                new Language { Content = "[be_latn] Biełaruskaja (Biełaruś)", Tag = "be_latn" },
                new Language { Content = "[bg_bg] Български (България)", Tag = "bg_bg" },
                new Language { Content = "[br_fr] Brezhoneg (Breizh)", Tag = "br_fr" },
                new Language { Content = "[brb] Braobans (Braobant)", Tag = "brb" },
                new Language { Content = "[bs_ba] Bosanski (Bosna i Hercegovina)", Tag = "bs_ba" },
                new Language { Content = "[ca_es] Català (Catalunya)", Tag = "ca_es" },
                new Language { Content = "[cs_cz] Čeština (Česko)", Tag = "cs_cz" },
                new Language { Content = "[cy_gb] Cymraeg (Cymru)", Tag = "cy_gb" },
                new Language { Content = "[da_dk] Dansk (Danmark)", Tag = "da_dk" },
                new Language { Content = "[de_at] Deitsch (Österreich)", Tag = "de_at" },
                new Language { Content = "[de_ch] Schwiizerdutsch (Schwiiz)", Tag = "de_ch" },
                new Language { Content = "[de_de] Deutsch (Deutschland)", Tag = "de_de" },
                new Language { Content = "[el_gr] Ελληνικά (Ελλάδα)", Tag = "el_gr" },
                new Language { Content = "[en_au] English (Australia)", Tag = "en_au" },
                new Language { Content = "[en_ca] English (Canada)", Tag = "en_ca" },
                new Language { Content = "[en_gb] English (United Kingdom)", Tag = "en_gb" },
                new Language { Content = "[en_nz] English (New Zealand)", Tag = "en_nz" },
                new Language { Content = "[en_us] English (US)", Tag = "en_us" },
                new Language { Content = "[enp] Anglish (Oned Riches)", Tag = "enp" },
                new Language { Content = "[eo_uy] Esperanto (Esperantujo)", Tag = "eo_uy" },
                new Language { Content = "[es_ar] Español (Argentina)", Tag = "es_ar" },
                new Language { Content = "[es_cl] Español (Chile)", Tag = "es_cl" },
                new Language { Content = "[es_ec] Español (Ecuador)", Tag = "es_ec" },
                new Language { Content = "[es_es] Español (España)", Tag = "es_es" },
                new Language { Content = "[es_mx] Español (México)", Tag = "es_mx" },
                new Language { Content = "[es_uy] Español (Uruguay)", Tag = "es_uy" },
                new Language { Content = "[es_ve] Español (Venezuela)", Tag = "es_ve" },
                new Language { Content = "[esan] Andalûh (Andaluçía)", Tag = "esan" },
                new Language { Content = "[et_ee] Eesti (Eesti)", Tag = "et_ee" },
                new Language { Content = "[eu_es] Euskara (Euskal Herria)", Tag = "eu_es" },
                new Language { Content = "[fa_ir] فارسی (ایران)", Tag = "fa_ir" },
                new Language { Content = "[fi_fi] Suomi (Suomi)", Tag = "fi_fi" },
                new Language { Content = "[fil_ph] Filipino (Pilipinas)", Tag = "fil_ph" },
                new Language { Content = "[fo_fo] Føroyskt (Føroyar)", Tag = "fo_fo" },
                new Language { Content = "[fr_ca] Français (Canada)", Tag = "fr_ca" },
                new Language { Content = "[fr_fr] Français (France)", Tag = "fr_fr" },
                new Language { Content = "[fra_de] Fränggisch (Franggn)", Tag = "fra_de" },
                new Language { Content = "[fur_it] Furlan (Friûl)", Tag = "fur_it" },
                new Language { Content = "[fy_nl] Frysk (Fryslân)", Tag = "fy_nl" },
                new Language { Content = "[ga_ie] Gaeilge (Éire)", Tag = "ga_ie" },
                new Language { Content = "[gd_gb] Gàidhlig (Alba)", Tag = "gd_gb" },
                new Language { Content = "[gl_es] Galego (Galicia / Galiza)", Tag = "gl_es" },
                new Language { Content = "[haw_us] 'Ōlelo Hawai'i (Hawai'i)", Tag = "haw_us" },
                new Language { Content = "[he_il] עברית (ישראל)", Tag = "he_il" },
                new Language { Content = "[hi_in] हिंदी (भारत)", Tag = "hi_in" },
                new Language { Content = "[hn_no] Høgnorsk (Norig)", Tag = "hn_no" },
                new Language { Content = "[hr_hr] Hrvatski (Hrvatska)", Tag = "hr_hr" },
                new Language { Content = "[hu_hu] Magyar (Magyarország)", Tag = "hu_hu" },
                new Language { Content = "[hy_am] Հայերեն (Հայաստան)", Tag = "hy_am" },
                new Language { Content = "[id_id] Bahasa Indonesia (Indonesia)", Tag = "id_id" },
                new Language { Content = "[ig_ng] Igbo (Naigeria)", Tag = "ig_ng" },
                new Language { Content = "[io_en] Ido (Idia)", Tag = "io_en" },
                new Language { Content = "[is_is] Íslenska (Ísland)", Tag = "is_is" },
                new Language { Content = "[isv] Medžuslovjansky (Slovjanščina)", Tag = "isv" },
                new Language { Content = "[it_it] Italiano (Italia)", Tag = "it_it" },
                new Language { Content = "[ja_jp] 日本語 (日本)", Tag = "ja_jp" },
                new Language { Content = "[jbo_en] la .lojban. (la jbogu'e)", Tag = "jbo_en" },
                new Language { Content = "[ka_ge] ქართული (საქართველო)", Tag = "ka_ge" },
                new Language { Content = "[kk_kz] Қазақша (Қазақстан)", Tag = "kk_kz" },
                new Language { Content = "[kn_in] ಕನ್ನಡ (ಭಾರತ)", Tag = "kn_in" },
                new Language { Content = "[ko_kr] 한국어 (대한민국)", Tag = "ko_kr" },
                new Language { Content = "[ksh] Kölsch/Ripoarisch (Rhingland)", Tag = "ksh" },
                new Language { Content = "[kw_gb] Kernewek (Kernow)", Tag = "kw_gb" },
                new Language { Content = "[ky_kg] Кыргызча (Кыргызстан)", Tag = "ky_kg" },
                new Language { Content = "[la_la] Latina (Latium)", Tag = "la_la" },
                new Language { Content = "[lb_lu] Lëtzebuergesch (Lëtzebuerg)", Tag = "lb_lu" },
                new Language { Content = "[li_li] Limburgs (Limburg)", Tag = "li_li" },
                new Language { Content = "[lmo] Lombard (Lombardia)", Tag = "lmo" },
                new Language { Content = "[lo_la] ລາວ (ປະເທດລາວ)", Tag = "lo_la" },
                new Language { Content = "[lt_lt] Lietuvių (Lietuva)", Tag = "lt_lt" },
                new Language { Content = "[lv_lv] Latviešu (Latvija)", Tag = "lv_lv" },
                new Language { Content = "[lzh] 文言（華夏）", Tag = "lzh" },
                new Language { Content = "[mk_mk] Македонски (Северна Македонија)", Tag = "mk_mk" },
                new Language { Content = "[mn_mn] Монгол (Монгол Улс)", Tag = "mn_mn" },
                new Language { Content = "[ms_my] Bahasa Melayu (Malaysia)", Tag = "ms_my" },
                new Language { Content = "[mt_mt] Malti (Malta)", Tag = "mt_mt" },
                new Language { Content = "[nah] Mēxikatlahtōlli (Mēxiko)", Tag = "nah" },
                new Language { Content = "[nds_de] Plattdüütsh (Düütschland)", Tag = "nds_de" },
                new Language { Content = "[nl_be] Vlaams (België)", Tag = "nl_be" },
                new Language { Content = "[nl_nl] Nederlands (Nederland)", Tag = "nl_nl" },
                new Language { Content = "[nn_no] Norsk nynorsk (Noreg)", Tag = "nn_no" },
                new Language { Content = "[no_no‌[JEonly]nb_no‌[BEonly]] Norsk bokmål (Norge)", Tag = "no_no‌" },
                new Language { Content = "[oc_fr] Occitan (Occitània)", Tag = "oc_fr" },
                new Language { Content = "[ovd] Övdalska (Swerre)", Tag = "ovd" },
                new Language { Content = "[pl_pl] Polski (Polska)", Tag = "pl_pl" },
                new Language { Content = "[pls] Ngiiwa (Ndanìngà)", Tag = "pls" },
                new Language { Content = "[pt_br] Português (Brasil)", Tag = "pt_br" },
                new Language { Content = "[pt_pt] Português (Portugal)", Tag = "pt_pt" },
                new Language { Content = "[qya_aa] Quenya (Arda)", Tag = "qya_aa" },
                new Language { Content = "[ro_ro] Română (România)", Tag = "ro_ro" },
                new Language { Content = "[rpr] Русскій дореформенный (Россійская имперія)", Tag = "rpr" },
                new Language { Content = "[ru_ru] Русский (Россия)", Tag = "ru_ru" },
                new Language { Content = "[ry_ua] Руснацькый (Пудкарпатя, Украина)", Tag = "ry_ua" },
                new Language { Content = "[sah_sah] Сахалыы (Cаха Сирэ)", Tag = "sah_sah" },
                new Language { Content = "[se_no] Davvisámegiella (Sápmi)", Tag = "se_no" },
                new Language { Content = "[sk_sk] Slovenčina (Slovensko)", Tag = "sk_sk" },
                new Language { Content = "[sl_si] Slovenščina (Slovenija)", Tag = "sl_si" },
                new Language { Content = "[so_so] Af-Soomaali (Soomaaliya)", Tag = "so_so" },
                new Language { Content = "[sq_al] Shqip (Shqiperia)", Tag = "sq_al" },
                new Language { Content = "[sr_cs] Srpski (Srbija)", Tag = "sr_cs" },
                new Language { Content = "[sr_sp] Српски (Србија)", Tag = "sr_sp" },
                new Language { Content = "[sv_se] Svenska (Sverige)", Tag = "sv_se" },
                new Language { Content = "[sxu] Säggs’sch (Saggsn)", Tag = "sxu" },
                new Language { Content = "[szl] Ślōnskŏ (Gōrny Ślōnsk)", Tag = "szl" },
                new Language { Content = "[ta_in] தமிழ் (இந்தியா)", Tag = "ta_in" },
                new Language { Content = "[th_th] ไทย (ประเทศไทย)", Tag = "th_th" },
                new Language { Content = "[tl_ph] Tagalog (Pilipinas)", Tag = "tl_ph" },
                new Language { Content = "[tlh_aa] tlhIngan Hol (tlhIngan wo')", Tag = "tlh_aa" },
                new Language { Content = "[tok] toki pona (ma pona)", Tag = "tok" },
                new Language { Content = "[tr_tr] Türkçe (Türkiye)", Tag = "tr_tr" },
                new Language { Content = "[tt_ru] Татарча (Татарстан, Рәсәй)", Tag = "tt_ru" },
                new Language { Content = "[tzo_mx] Bats'i k'op (Jobel)", Tag = "tzo_mx" },
                new Language { Content = "[uk_ua] Українська (Україна)", Tag = "uk_ua" },
                new Language { Content = "[val_es] Català (Valencià) (País Valencià)", Tag = "val_es" },
                new Language { Content = "[vec_it] Vèneto (Veneto)", Tag = "vec_it" },
                new Language { Content = "[vi_vn] Tiếng Việt (Việt Nam)", Tag = "vi_vn" },
                new Language { Content = "[vp_vl] Viossa (Vilant)", Tag = "vp_vl" },
                new Language { Content = "[yi_de] ייִדיש (אשכנזיש יידן)", Tag = "yi_de" },
                new Language { Content = "[yo_ng] Yorùbá (Nàìjíríà)", Tag = "yo_ng" },
                new Language { Content = "[zh_cn] 简体中文（中国大陆）", Tag = "zh_cn" },
                new Language { Content = "[zh_hk] 繁體中文（香港特別行政區）", Tag = "zh_hk" },
                new Language { Content = "[zh_tw] 繁體中文（台灣）", Tag = "zh_tw" },
                new Language { Content = "[zlm_arab] بهاس ملايو (مليسيا)", Tag = "zlm_arab" }
                
            ];
        }
    }
}
