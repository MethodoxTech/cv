using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Styling;
using System.Collections.Generic;

namespace CheckVersion.GUI
{
    /// <summary>
    /// The app's palette and control styling, declared in code because this project has no XAML.
    /// </summary>
    /// <remarks>
    /// Fluent supplies the control templates; everything here is the skin over them. Styles that target
    /// `.Template().OfType&lt;...&gt;()` reach into a template part, which is how Fluent's own hover and
    /// focus visuals are expressed — setting the property on the control alone would only change the
    /// resting state.
    /// </remarks>
    public static class AppTheme
    {
        #region Palette
        public static readonly Color Background = Color.FromRgb(0x1B, 0x1D, 0x21);
        public static readonly Color Surface = Color.FromRgb(0x23, 0x26, 0x2B);
        public static readonly Color SurfaceRaised = Color.FromRgb(0x2A, 0x2E, 0x34);
        public static readonly Color SurfaceSunken = Color.FromRgb(0x16, 0x18, 0x1B);
        public static readonly Color Line = Color.FromRgb(0x35, 0x39, 0x40);
        public static readonly Color LineStrong = Color.FromRgb(0x45, 0x4A, 0x53);

        public static readonly Color Text = Color.FromRgb(0xE6, 0xE8, 0xEA);
        public static readonly Color TextDim = Color.FromRgb(0x99, 0xA0, 0xA9);
        public static readonly Color TextFaint = Color.FromRgb(0x6C, 0x73, 0x7C);

        public static readonly Color Accent = Color.FromRgb(0x4C, 0x8D, 0xFF);
        public static readonly Color AccentHover = Color.FromRgb(0x62, 0x9C, 0xFF);
        public static readonly Color AccentPressed = Color.FromRgb(0x3B, 0x78, 0xE0);

        public static readonly Color New = Color.FromRgb(0x5F, 0xD6, 0x8A);
        public static readonly Color Updated = Color.FromRgb(0xE8, 0xC4, 0x6A);
        public static readonly Color Moved = Color.FromRgb(0x6F, 0xC3, 0xE8);
        public static readonly Color Deleted = Color.FromRgb(0xE8, 0x73, 0x6A);

        public static readonly IBrush BackgroundBrush = Solid(Background);
        public static readonly IBrush SurfaceBrush = Solid(Surface);
        public static readonly IBrush SurfaceRaisedBrush = Solid(SurfaceRaised);
        public static readonly IBrush SurfaceSunkenBrush = Solid(SurfaceSunken);
        public static readonly IBrush LineBrush = Solid(Line);
        public static readonly IBrush LineStrongBrush = Solid(LineStrong);
        public static readonly IBrush TextBrush = Solid(Text);
        public static readonly IBrush TextDimBrush = Solid(TextDim);
        public static readonly IBrush TextFaintBrush = Solid(TextFaint);
        public static readonly IBrush AccentBrush = Solid(Accent);
        public static readonly IBrush NewBrush = Solid(New);
        public static readonly IBrush UpdatedBrush = Solid(Updated);
        public static readonly IBrush MovedBrush = Solid(Moved);
        public static readonly IBrush DeletedBrush = Solid(Deleted);
        #endregion

        #region Metrics
        public const double CornerSmall = 4;
        public const double CornerMedium = 6;
        public const double CornerLarge = 8;
        public const double FontSmall = 11.5;
        public const double FontBody = 13;
        public const double FontHeading = 13.5;
        public const double FontTitle = 15;

        public static readonly FontFamily MonoFont = new("Cascadia Mono, Consolas, Menlo, DejaVu Sans Mono, monospace");
        #endregion

        #region Styles
        /// <summary>
        /// Everything that cannot be set per-instance: hover and selection visuals, and the control
        /// defaults that would otherwise have to be repeated at every construction site.
        /// </summary>
        public static Styles BuildStyles()
        {
            Styles styles = [];

            foreach (Style style in TextStyles())
                styles.Add(style);
            foreach (Style style in ButtonStyles())
                styles.Add(style);
            foreach (Style style in InputStyles())
                styles.Add(style);
            foreach (Style style in ListStyles())
                styles.Add(style);
            foreach (Style style in TabStyles())
                styles.Add(style);
            foreach (Style style in MiscStyles())
                styles.Add(style);

            return styles;
        }

        private static IEnumerable<Style> TextStyles()
        {
            yield return Make(x => x.OfType<TextBlock>(),
                (TextBlock.FontSizeProperty, FontBody),
                (TextBlock.ForegroundProperty, TextBrush));

            // Section label above a group of controls.
            yield return Make(x => x.OfType<TextBlock>().Class("h2"),
                (TextBlock.FontSizeProperty, FontHeading),
                (TextBlock.FontWeightProperty, FontWeight.SemiBold),
                (TextBlock.ForegroundProperty, TextBrush));

            // Window-level heading.
            yield return Make(x => x.OfType<TextBlock>().Class("h1"),
                (TextBlock.FontSizeProperty, FontTitle),
                (TextBlock.FontWeightProperty, FontWeight.SemiBold));

            // Secondary explanatory text.
            yield return Make(x => x.OfType<TextBlock>().Class("caption"),
                (TextBlock.FontSizeProperty, FontSmall),
                (TextBlock.ForegroundProperty, TextDimBrush));

            yield return Make(x => x.OfType<TextBlock>().Class("mono"),
                (TextBlock.FontFamilyProperty, MonoFont),
                (TextBlock.FontSizeProperty, FontSmall));
        }

        private static IEnumerable<Style> ButtonStyles()
        {
            yield return Make(x => x.OfType<Button>(),
                (Button.BackgroundProperty, SurfaceRaisedBrush),
                (Button.ForegroundProperty, TextBrush),
                (Button.BorderBrushProperty, LineBrush),
                (Button.BorderThicknessProperty, new Thickness(1)),
                (Button.CornerRadiusProperty, new CornerRadius(CornerMedium)),
                (Button.PaddingProperty, new Thickness(14, 7)),
                (Button.FontSizeProperty, FontBody),
                (Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center));

            yield return Make(x => x.OfType<Button>().Class(":pointerover").Template().OfType<ContentPresenter>(),
                (ContentPresenter.BackgroundProperty, Solid(Color.FromRgb(0x33, 0x38, 0x3F))),
                (ContentPresenter.BorderBrushProperty, LineStrongBrush));

            yield return Make(x => x.OfType<Button>().Class(":pressed").Template().OfType<ContentPresenter>(),
                (ContentPresenter.BackgroundProperty, Solid(Color.FromRgb(0x1F, 0x22, 0x27))),
                (ContentPresenter.BorderBrushProperty, LineBrush));

            yield return Make(x => x.OfType<Button>().Class(":disabled").Template().OfType<ContentPresenter>(),
                (ContentPresenter.BackgroundProperty, Solid(Color.FromRgb(0x24, 0x27, 0x2C))),
                (ContentPresenter.ForegroundProperty, TextFaintBrush),
                (ContentPresenter.BorderBrushProperty, Solid(Color.FromRgb(0x2C, 0x30, 0x36))));

            // Primary action.
            yield return Make(x => x.OfType<Button>().Class("accent"),
                (Button.BackgroundProperty, AccentBrush),
                (Button.ForegroundProperty, Brushes.White),
                (Button.BorderBrushProperty, AccentBrush),
                (Button.FontWeightProperty, FontWeight.SemiBold));

            yield return Make(x => x.OfType<Button>().Class("accent").Class(":pointerover").Template().OfType<ContentPresenter>(),
                (ContentPresenter.BackgroundProperty, Solid(AccentHover)),
                (ContentPresenter.BorderBrushProperty, Solid(AccentHover)),
                (ContentPresenter.ForegroundProperty, Brushes.White));

            yield return Make(x => x.OfType<Button>().Class("accent").Class(":pressed").Template().OfType<ContentPresenter>(),
                (ContentPresenter.BackgroundProperty, Solid(AccentPressed)),
                (ContentPresenter.BorderBrushProperty, Solid(AccentPressed)),
                (ContentPresenter.ForegroundProperty, Brushes.White));

            // Toolbar / low-emphasis action.
            yield return Make(x => x.OfType<Button>().Class("quiet"),
                (Button.BackgroundProperty, Brushes.Transparent),
                (Button.BorderBrushProperty, Brushes.Transparent),
                (Button.ForegroundProperty, TextDimBrush),
                (Button.PaddingProperty, new Thickness(10, 6)));

            yield return Make(x => x.OfType<Button>().Class("quiet").Class(":pointerover").Template().OfType<ContentPresenter>(),
                (ContentPresenter.BackgroundProperty, SurfaceRaisedBrush),
                (ContentPresenter.BorderBrushProperty, Brushes.Transparent),
                (ContentPresenter.ForegroundProperty, TextBrush));
        }

        private static IEnumerable<Style> InputStyles()
        {
            yield return Make(x => x.OfType<TextBox>(),
                (TextBox.ForegroundProperty, TextBrush),
                (TextBox.CaretBrushProperty, AccentBrush),
                (TextBox.FontSizeProperty, FontBody),
                (TextBox.PaddingProperty, new Thickness(10, 7)),
                (TextBox.MinHeightProperty, 0d));

            yield return Make(x => x.OfType<TextBox>().Template().OfType<Border>().Name("PART_BorderElement"),
                (Border.BackgroundProperty, SurfaceSunkenBrush),
                (Border.BorderBrushProperty, LineBrush),
                (Border.BorderThicknessProperty, new Thickness(1)),
                (Border.CornerRadiusProperty, new CornerRadius(CornerMedium)));

            yield return Make(x => x.OfType<TextBox>().Class(":pointerover").Template().OfType<Border>().Name("PART_BorderElement"),
                (Border.BorderBrushProperty, LineStrongBrush));

            yield return Make(x => x.OfType<TextBox>().Class(":focus").Template().OfType<Border>().Name("PART_BorderElement"),
                (Border.BackgroundProperty, SurfaceSunkenBrush),
                (Border.BorderBrushProperty, AccentBrush));

            yield return Make(x => x.OfType<CheckBox>(),
                (CheckBox.ForegroundProperty, TextBrush),
                (CheckBox.FontSizeProperty, FontBody),
                (CheckBox.MinHeightProperty, 0d));
        }

        private static IEnumerable<Style> ListStyles()
        {
            yield return Make(x => x.OfType<ListBox>(),
                (ListBox.BackgroundProperty, Brushes.Transparent),
                (ListBox.BorderThicknessProperty, new Thickness(0)),
                (ListBox.PaddingProperty, new Thickness(2)));

            yield return Make(x => x.OfType<ListBoxItem>(),
                (ListBoxItem.PaddingProperty, new Thickness(8, 5)),
                (ListBoxItem.MinHeightProperty, 0d),
                (ListBoxItem.CornerRadiusProperty, new CornerRadius(CornerSmall)),
                (ListBoxItem.ForegroundProperty, TextBrush));

            yield return Make(x => x.OfType<ListBoxItem>().Class(":pointerover").Template().OfType<ContentPresenter>(),
                (ContentPresenter.BackgroundProperty, Solid(Color.FromRgb(0x2E, 0x33, 0x39))));

            yield return Make(x => x.OfType<ListBoxItem>().Class(":selected").Template().OfType<ContentPresenter>(),
                (ContentPresenter.BackgroundProperty, Solid(Color.FromArgb(0x40, 0x4C, 0x8D, 0xFF))));

            yield return Make(x => x.OfType<ListBoxItem>().Class(":selected").Class(":pointerover").Template().OfType<ContentPresenter>(),
                (ContentPresenter.BackgroundProperty, Solid(Color.FromArgb(0x55, 0x4C, 0x8D, 0xFF))));
        }

        private static IEnumerable<Style> TabStyles()
        {
            yield return Make(x => x.OfType<TabItem>(),
                (TabItem.FontSizeProperty, FontBody),
                (TabItem.FontWeightProperty, FontWeight.Normal),
                (TabItem.ForegroundProperty, TextDimBrush),
                (TabItem.PaddingProperty, new Thickness(14, 8)),
                (TabItem.MinHeightProperty, 0d),
                (TabItem.MarginProperty, new Thickness(0)));

            yield return Make(x => x.OfType<TabItem>().Class(":selected"),
                (TabItem.ForegroundProperty, TextBrush),
                (TabItem.FontWeightProperty, FontWeight.SemiBold));

            yield return Make(x => x.OfType<TabItem>().Class(":pointerover").Template().OfType<Border>().Name("PART_LayoutRoot"),
                (Border.BackgroundProperty, Solid(Color.FromRgb(0x2A, 0x2E, 0x34))));
        }

        private static IEnumerable<Style> MiscStyles()
        {
            yield return Make(x => x.OfType<Separator>(),
                (Separator.BackgroundProperty, LineBrush),
                (Separator.HeightProperty, 1d),
                (Separator.MarginProperty, new Thickness(0, 4)));

            yield return Make(x => x.OfType<GridSplitter>(),
                (GridSplitter.BackgroundProperty, Brushes.Transparent));

            yield return Make(x => x.OfType<GridSplitter>().Class(":pointerover"),
                (GridSplitter.BackgroundProperty, Solid(Color.FromArgb(0x60, 0x4C, 0x8D, 0xFF))));

            yield return Make(x => x.OfType<ProgressBar>(),
                (ProgressBar.ForegroundProperty, AccentBrush),
                (ProgressBar.BackgroundProperty, LineBrush));

            yield return Make(x => x.OfType<AutoCompleteBox>(),
                (AutoCompleteBox.FontSizeProperty, FontBody),
                (AutoCompleteBox.MinHeightProperty, 0d));
        }
        #endregion

        #region Helpers
        private static Style Make(System.Func<Selector?, Selector> selector, params (AvaloniaProperty Property, object? Value)[] setters)
        {
            Style style = new(selector);
            foreach ((AvaloniaProperty property, object? value) in setters)
                style.Setters.Add(new Setter(property, value));
            return style;
        }

        private static IBrush Solid(Color color)
        {
            SolidColorBrush brush = new(color);
            brush.ToImmutable();
            return brush;
        }
        #endregion
    }
}
