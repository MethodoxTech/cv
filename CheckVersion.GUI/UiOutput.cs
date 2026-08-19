using Avalonia.Threading;
using CheckVersion.GUI.Views;
using CheckVersion.Types;
using System;
using DrawingColor = System.Drawing.Color;

namespace CheckVersion.GUI
{
    /// <summary>
    /// Routes <see cref="CheckVersionTool"/> output into an <see cref="OutputLogView"/>, marshalling to the
    /// UI thread because tool operations run on the thread pool to keep the window responsive.
    /// </summary>
    public sealed class UiOutput : ICheckVersionOutput
    {
        #region Fields
        private readonly OutputLogView _log;
        #endregion

        #region Construction
        public UiOutput(OutputLogView log)
            => _log = log;
        #endregion

        #region Properties
        /// <summary>
        /// Answer handed back for any <see cref="Confirm"/> the tool asks. The GUI resolves questions with a
        /// real dialog before starting an operation, so this is only a fallback.
        /// </summary>
        public bool AutoConfirm { get; set; }
        #endregion

        #region ICheckVersionOutput
        public void Write(DrawingColor color, string text)
            => Post(() => _log.Append(color, text));
        public void WriteLine(DrawingColor color, string text)
            => Post(() =>
            {
                _log.AppendLine(color, text);
                _log.ScrollToEnd();
            });
        public void WriteLine(string text)
            => WriteLine(DrawingColor.Gainsboro, text);
        public void WriteLine()
            => WriteLine(DrawingColor.Gainsboro, string.Empty);
        public bool Confirm(string question, bool defaultAnswer)
        {
            WriteLine(DrawingColor.Yellow, $"{question} [{(AutoConfirm ? "Yes" : "No")}]");
            return AutoConfirm;
        }
        #endregion

        #region Helpers
        private static void Post(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
                action();
            else
                Dispatcher.UIThread.Post(action, DispatcherPriority.Background);
        }
        #endregion
    }
}
