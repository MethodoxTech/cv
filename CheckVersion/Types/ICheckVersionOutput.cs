using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace CheckVersion.Types
{
    /// <summary>
    /// Destination for the progress/diagnostic text produced by <see cref="CheckVersionTool"/>,
    /// plus the one interactive question the tool needs to ask.
    /// </summary>
    /// <remarks>
    /// The tool used to write to <see cref="ColorConsole"/> directly, which meant a GUI could only
    /// reuse it by hijacking <see cref="Console.SetOut(System.IO.TextWriter)"/>. Routing everything
    /// through this interface lets a non-console host render the same messages.
    /// </remarks>
    public interface ICheckVersionOutput
    {
        void Write(Color color, string text);
        void WriteLine(Color color, string text);
        void WriteLine(string text);
        void WriteLine();
        /// <summary>
        /// Ask the user a yes/no question. Hosts that cannot prompt should return <paramref name="defaultAnswer"/>.
        /// </summary>
        bool Confirm(string question, bool defaultAnswer);
    }

    /// <summary>
    /// Default <see cref="ICheckVersionOutput"/>: colored text on the terminal, answers read from stdin.
    /// </summary>
    public sealed class ConsoleOutput : ICheckVersionOutput
    {
        public static readonly ConsoleOutput Instance = new();

        public void Write(Color color, string text)
            => ColorConsole.Write(color, text);
        public void WriteLine(Color color, string text)
            => ColorConsole.WriteLine(color, text);
        public void WriteLine(string text)
            => ColorConsole.WriteLine(text);
        public void WriteLine()
            => ColorConsole.WriteLine();
        public bool Confirm(string question, bool defaultAnswer)
        {
            ColorConsole.WriteLine(Color.Red, $"{question} [Y/N]");

            string? input = ColorConsole.ReadLine()?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(input))
                return defaultAnswer;
            if (input == "n" || input == "no" || input == "f")
                return false;
            if (input == "y" || input == "yes" || input == "t")
                return true;
            return defaultAnswer;
        }
    }

    /// <summary>
    /// Collects output in memory instead of printing it. Useful for tests and for hosts that want the
    /// full transcript of an operation after it finished.
    /// </summary>
    public sealed class CollectingOutput : ICheckVersionOutput
    {
        private readonly StringBuilder _transcript = new();
        private readonly StringBuilder _pendingLine = new();
        private readonly List<string> _lines = [];
        private readonly bool _confirmAnswer;

        /// <param name="confirmAnswer">Answer returned for every <see cref="Confirm"/> call.</param>
        public CollectingOutput(bool confirmAnswer = false)
            => _confirmAnswer = confirmAnswer;

        /// <summary>
        /// Everything written so far, including any partial (non-terminated) trailing line.
        /// </summary>
        public string Text => _transcript.ToString();
        /// <summary>
        /// Completed lines only.
        /// </summary>
        public IReadOnlyList<string> Lines => _lines;

        public void Write(Color color, string text)
        {
            _transcript.Append(text);
            _pendingLine.Append(text);
        }
        public void WriteLine(Color color, string text)
            => WriteLine(text);
        public void WriteLine(string text)
        {
            _transcript.Append(text);
            _pendingLine.Append(text);
            FlushLine();
        }
        public void WriteLine()
            => FlushLine();
        public bool Confirm(string question, bool defaultAnswer)
            => _confirmAnswer;

        private void FlushLine()
        {
            _transcript.AppendLine();
            _lines.Add(_pendingLine.ToString());
            _pendingLine.Clear();
        }
    }
}
