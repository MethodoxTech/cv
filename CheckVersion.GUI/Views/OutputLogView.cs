using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using System.Collections.Generic;
using DrawingColor = System.Drawing.Color;

namespace CheckVersion.GUI.Views
{
    /// <summary>
    /// A console-like transcript pane. Renders the same colored text the CLI prints, so GUI users see
    /// exactly what `cv` would have said.
    /// </summary>
    public sealed class OutputLogView : UserControl
    {
        #region Constants
        /// <summary>
        /// Trim the transcript once it grows past this, so a long gather does not turn into an unbounded
        /// inline collection.
        /// </summary>
        private const int MaximumInlines = 4000;
        private const int TrimBatch = 1000;
        #endregion

        #region Fields
        private readonly SelectableTextBlock _text;
        private readonly ScrollViewer _scroller;
        #endregion

        #region Construction
        public OutputLogView()
        {
            _text = new SelectableTextBlock
            {
                FontFamily = new FontFamily("Consolas, Menlo, DejaVu Sans Mono, monospace"),
                FontSize = 12,
                Margin = new Thickness(8),
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.Gainsboro
            };

            _scroller = new ScrollViewer
            {
                Content = _text,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Content = new Border
            {
                // A dark ground keeps the CLI's palette (White/DarkGray/Yellow/...) readable.
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
                Child = _scroller
            };
        }
        #endregion

        #region Methods
        /// <summary>
        /// Append text without a line break. Must be called on the UI thread.
        /// </summary>
        public void Append(DrawingColor color, string text)
        {
            _text.Inlines?.Add(new Run(text) { Foreground = ToBrush(color) });
            TrimIfNeeded();
        }
        /// <summary>
        /// Append text followed by a line break. Must be called on the UI thread.
        /// </summary>
        public void AppendLine(DrawingColor color, string text)
        {
            if (text.Length > 0)
                _text.Inlines?.Add(new Run(text) { Foreground = ToBrush(color) });
            _text.Inlines?.Add(new LineBreak());
            TrimIfNeeded();
        }
        public void Clear()
            => _text.Inlines?.Clear();
        /// <summary>
        /// Scroll the newest output into view. No-op before the pane is laid out, since scrolling an
        /// unmeasured viewer would only re-invalidate the layout that is about to run anyway.
        /// </summary>
        public void ScrollToEnd()
        {
            if (_scroller.IsLoaded)
                _scroller.ScrollToEnd();
        }
        #endregion

        #region Helpers
        private void TrimIfNeeded()
        {
            InlineCollection? inlines = _text.Inlines;
            if (inlines == null || inlines.Count <= MaximumInlines)
                return;

            for (int i = 0; i < TrimBatch && inlines.Count > 0; i++)
                inlines.RemoveAt(0);
        }
        private static readonly Dictionary<int, IBrush> BrushCache = [];
        private static IBrush ToBrush(DrawingColor color)
        {
            int key = color.ToArgb();
            if (BrushCache.TryGetValue(key, out IBrush? cached))
                return cached;

            SolidColorBrush brush = new(Color.FromArgb(color.A, color.R, color.G, color.B));
            BrushCache[key] = brush;
            return brush;
        }
        #endregion
    }
}
