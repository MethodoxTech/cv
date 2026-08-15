using Avalonia;
using Avalonia.Headless;
using CheckVersion.GUI.UnitTests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace CheckVersion.GUI.UnitTests
{
    /// <summary>
    /// Boots the real <see cref="CheckVersion.GUI.App"/> on Avalonia's headless platform, so the tests
    /// exercise the same procedurally built visual tree the desktop app uses.
    /// </summary>
    public static class TestAppBuilder
    {
        /// <remarks>
        /// Rendering goes through Skia rather than the default headless drawing stub. The stub's text path
        /// stalls in shaping once the output pane actually has text in it, and Skia is also what the real
        /// desktop app uses, so the tests exercise the same measure/layout behavior users get.
        /// </remarks>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UseSkia()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}
