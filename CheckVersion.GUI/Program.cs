using Avalonia;
using System;

namespace CheckVersion.GUI
{
    internal static class Program
    {
        /// <summary>
        /// Entry point. Kept free of any Avalonia object construction so the visual tree is only ever
        /// built after the framework is initialized.
        /// </summary>
        [STAThread]
        public static int Main(string[] args)
            => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        /// <summary>
        /// Also used by the Avalonia designer/tooling, which looks for this exact signature.
        /// </summary>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
