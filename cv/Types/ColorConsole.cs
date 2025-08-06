using System;
using System.Drawing;

namespace cv.Types
{
    /// <summary>
    /// Print colorful console texts; A drop-in replacement for Colorful.Console, with some interface change for clarity
    /// </summary>
    public static class ColorConsole
    {
        #region Methods
        /// <summary>
        /// Write text in the given System.Drawing.Color, then reset to the previous console color.
        /// </summary>
        public static void Write(Color color, string text)
        {
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = ToConsoleColor(color);
            Console.Write(text);
            Console.ForegroundColor = previous;
        }
        public static void Write(Color color, object value)
            => Write(color, value.ToString());
        /// <summary>
        /// WriteLine(text) in the given System.Drawing.Color, then reset to the previous console color.
        /// </summary>
        public static void WriteLine(Color color, string text)
        {
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = ToConsoleColor(color);
            Console.WriteLine(text);
            Console.ForegroundColor = previous;
        }
        public static void WriteLine(Color color, object value)
            => WriteLine(color, value.ToString());
        /// <summary>
        /// Fall-back to a normal WriteLine.
        /// </summary>
        public static void WriteLine(string text)
            => Console.WriteLine(text);
        /// <summary>
        /// Fall-back to a normal WriteLine.
        /// </summary>
        public static void WriteLine()
            => Console.WriteLine();
        /// <summary>
        /// Fall-back to a normal Write.
        /// </summary>
        public static void Write(string text)
            => Console.Write(text);
        /// <summary>
        /// Expose ReadLine unchanged.
        /// </summary>
        public static string ReadLine()
            => Console.ReadLine();
        #endregion

        #region Helpers
        /// <summary>
        /// Map a System.Drawing.Color to the closest ConsoleColor.  
        /// First try an exact name-match, then special-case a few others, else default to White.
        /// </summary>
        private static ConsoleColor ToConsoleColor(Color color)
        {
            // Try parsing by name:
            if (Enum.TryParse<ConsoleColor>(color.Name, out ConsoleColor cc))
                return cc;

            // A few common overrides that don't map by name:
            if (color.ToArgb() == Color.Goldenrod.ToArgb()) return ConsoleColor.Yellow;
            if (color.ToArgb() == Color.YellowGreen.ToArgb()) return ConsoleColor.Green;
            if (color.ToArgb() == Color.SkyBlue.ToArgb()) return ConsoleColor.Cyan;
            if (color.ToArgb() == Color.DarkGray.ToArgb()) return ConsoleColor.DarkGray;
            if (color.ToArgb() == Color.Yellow.ToArgb()) return ConsoleColor.Yellow;
            if (color.ToArgb() == Color.DarkRed.ToArgb()) return ConsoleColor.DarkRed;
            // More to be added...

            // Default
            return ConsoleColor.White;
        }
        #endregion
    }
}
