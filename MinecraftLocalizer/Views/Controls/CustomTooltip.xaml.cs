using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace MinecraftLocalizer.Views.Controls
{
    public partial class CustomTooltip : UserControl
    {
        private readonly DispatcherTimer _closeTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };

        public CustomTooltip()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            _closeTimer.Tick += OnCloseTimerTick;
        }

        public static readonly DependencyProperty TooltipTextProperty =
            DependencyProperty.Register(nameof(TooltipText), typeof(string), typeof(CustomTooltip), new PropertyMetadata(string.Empty, OnTooltipContentChanged));

        public static readonly DependencyProperty TooltipLinkTextProperty =
            DependencyProperty.Register(nameof(TooltipLinkText), typeof(string), typeof(CustomTooltip), new PropertyMetadata(string.Empty, OnTooltipContentChanged));

        public static readonly DependencyProperty TooltipLinkUriProperty =
            DependencyProperty.Register(nameof(TooltipLinkUri), typeof(string), typeof(CustomTooltip), new PropertyMetadata(string.Empty, OnTooltipContentChanged));

        public static readonly DependencyProperty BadgeContentProperty =
            DependencyProperty.Register(nameof(BadgeContent), typeof(object), typeof(CustomTooltip), new PropertyMetadata("?"));

        public static readonly DependencyProperty BadgeBackgroundProperty =
            DependencyProperty.Register(nameof(BadgeBackground), typeof(Brush), typeof(CustomTooltip), new PropertyMetadata(Brushes.Transparent));

        public static readonly DependencyProperty BadgeCornerRadiusProperty =
            DependencyProperty.Register(nameof(BadgeCornerRadius), typeof(CornerRadius), typeof(CustomTooltip), new PropertyMetadata(new CornerRadius(5)));

        public static readonly DependencyProperty BadgePaddingProperty =
            DependencyProperty.Register(nameof(BadgePadding), typeof(Thickness), typeof(CustomTooltip), new PropertyMetadata(new Thickness(3, 0, 3, 3)));

        public static readonly DependencyProperty BadgeFontSizeProperty =
            DependencyProperty.Register(nameof(BadgeFontSize), typeof(double), typeof(CustomTooltip), new PropertyMetadata(14d));

        public static readonly DependencyProperty BadgeFontWeightProperty =
            DependencyProperty.Register(nameof(BadgeFontWeight), typeof(FontWeight), typeof(CustomTooltip), new PropertyMetadata(FontWeights.Bold));

        public static readonly DependencyProperty BadgeLabelPaddingProperty =
            DependencyProperty.Register(nameof(BadgeLabelPadding), typeof(Thickness), typeof(CustomTooltip), new PropertyMetadata(new Thickness(0)));

        public static readonly DependencyProperty TooltipContentProperty =
            DependencyProperty.Register(nameof(TooltipContent), typeof(object), typeof(CustomTooltip), new PropertyMetadata(null, OnTooltipContentChanged));

        public static readonly DependencyProperty TooltipContentTemplateProperty =
            DependencyProperty.Register(nameof(TooltipContentTemplate), typeof(DataTemplate), typeof(CustomTooltip), new PropertyMetadata(null, OnTooltipContentChanged));

        public string TooltipText
        {
            get => (string)GetValue(TooltipTextProperty);
            set => SetValue(TooltipTextProperty, value);
        }

        public string TooltipLinkText
        {
            get => (string)GetValue(TooltipLinkTextProperty);
            set => SetValue(TooltipLinkTextProperty, value);
        }

        public string TooltipLinkUri
        {
            get => (string)GetValue(TooltipLinkUriProperty);
            set => SetValue(TooltipLinkUriProperty, value);
        }

        public object BadgeContent
        {
            get => GetValue(BadgeContentProperty);
            set => SetValue(BadgeContentProperty, value);
        }

        public Brush BadgeBackground
        {
            get => (Brush)GetValue(BadgeBackgroundProperty);
            set => SetValue(BadgeBackgroundProperty, value);
        }

        public CornerRadius BadgeCornerRadius
        {
            get => (CornerRadius)GetValue(BadgeCornerRadiusProperty);
            set => SetValue(BadgeCornerRadiusProperty, value);
        }

        public Thickness BadgePadding
        {
            get => (Thickness)GetValue(BadgePaddingProperty);
            set => SetValue(BadgePaddingProperty, value);
        }

        public double BadgeFontSize
        {
            get => (double)GetValue(BadgeFontSizeProperty);
            set => SetValue(BadgeFontSizeProperty, value);
        }

        public FontWeight BadgeFontWeight
        {
            get => (FontWeight)GetValue(BadgeFontWeightProperty);
            set => SetValue(BadgeFontWeightProperty, value);
        }

        public Thickness BadgeLabelPadding
        {
            get => (Thickness)GetValue(BadgeLabelPaddingProperty);
            set => SetValue(BadgeLabelPaddingProperty, value);
        }

        public object? TooltipContent
        {
            get => GetValue(TooltipContentProperty);
            set => SetValue(TooltipContentProperty, value);
        }

        public DataTemplate? TooltipContentTemplate
        {
            get => (DataTemplate?)GetValue(TooltipContentTemplateProperty);
            set => SetValue(TooltipContentTemplateProperty, value);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RootBorder.MouseEnter += OnMouseEnter;
            RootBorder.MouseLeave += OnMouseLeave;
            TooltipPopup.MouseEnter += OnPopupMouseEnter;
            TooltipPopup.MouseLeave += OnPopupMouseLeave;
            UpdateTooltipContent();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            RootBorder.MouseEnter -= OnMouseEnter;
            RootBorder.MouseLeave -= OnMouseLeave;
            TooltipPopup.MouseEnter -= OnPopupMouseEnter;
            TooltipPopup.MouseLeave -= OnPopupMouseLeave;
            _closeTimer.Stop();
        }

        private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TooltipPopup.IsOpen = true;
            _closeTimer.Stop();
        }

        private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _closeTimer.Start();
        }

        private void OnPopupMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _closeTimer.Stop();
        }

        private void OnPopupMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _closeTimer.Start();
        }

        private void OnCloseTimerTick(object? sender, EventArgs e)
        {
            if (!RootBorder.IsMouseOver && !TooltipPopup.IsMouseOver)
            {
                TooltipPopup.IsOpen = false;
                _closeTimer.Stop();
            }
        }

        private static void OnTooltipContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CustomTooltip badge)
            {
                badge.UpdateTooltipContent();
            }
        }

        private void UpdateTooltipContent()
        {
            if (TooltipPresenter == null)
                return;

            if (TooltipContentTemplate != null)
            {
                TooltipPresenter.ContentTemplate = TooltipContentTemplate;
                TooltipPresenter.Content = TooltipContent;
                return;
            }

            var textBlock = new TextBlock
            {
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };

            var tooltipText = TooltipText ?? string.Empty;
            var hasExplicitLink = !string.IsNullOrWhiteSpace(TooltipLinkText) && !string.IsNullOrWhiteSpace(TooltipLinkUri);

            if (hasExplicitLink)
            {
                var linkIndex = tooltipText.IndexOf(TooltipLinkText, StringComparison.Ordinal);
                if (linkIndex >= 0)
                {
                    AppendText(textBlock, tooltipText[..linkIndex]);
                    textBlock.Inlines.Add(CreateHyperlink(TooltipLinkText, TooltipLinkUri));
                    AppendText(textBlock, tooltipText[(linkIndex + TooltipLinkText.Length)..]);
                }
                else
                {
                    AppendText(textBlock, tooltipText);
                    textBlock.Inlines.Add(CreateHyperlink(TooltipLinkText, TooltipLinkUri));
                }
            }
            else
            {
                var match = Regex.Match(tooltipText, @"https?://\S+");
                if (match.Success)
                {
                    AppendText(textBlock, tooltipText[..match.Index]);
                    textBlock.Inlines.Add(CreateHyperlink(match.Value, match.Value));
                    AppendText(textBlock, tooltipText[(match.Index + match.Length)..]);
                }
                else
                {
                    AppendText(textBlock, tooltipText);
                }
            }

            TooltipPresenter.ContentTemplate = null;
            TooltipPresenter.Content = textBlock;
        }

        private static void AppendText(TextBlock textBlock, string text)
        {
            if (!string.IsNullOrEmpty(text))
                textBlock.Inlines.Add(new Run(text));
        }

        private Hyperlink CreateHyperlink(string linkText, string linkUri)
        {
            var hyperlink = new Hyperlink(new Run(linkText))
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0x7E, 0xC7, 0xFF)),
                TextDecorations = TextDecorations.Underline,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            if (Uri.TryCreate(linkUri, UriKind.Absolute, out var uri))
            {
                hyperlink.NavigateUri = uri;
                hyperlink.RequestNavigate += OnRequestNavigate;
            }

            return hyperlink;
        }

        private void OnRequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            if (e.Uri == null)
                return;

            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
