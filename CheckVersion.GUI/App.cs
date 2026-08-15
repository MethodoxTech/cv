using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using CheckVersion.GUI.Views;
using System;
using System.IO;

namespace CheckVersion.GUI
{
    /// <summary>
    /// Application shell. Everything (styles included) is set up in code — there is no XAML in this project.
    /// </summary>
    public sealed class App : Application
    {
        public override void Initialize()
        {
            // The XAML-free equivalent of App.axaml: Fluent supplies the control templates, Theme skins them.
            Styles.Add(new FluentTheme());
            Styles.Add(AppTheme.BuildStyles());
            RequestedThemeVariant = ThemeVariant.Dark;
            Name = "Check Version";
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = new MainWindow(GetInitialRepoPath(desktop.Args));

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// `cv-gui [repo folder]`, defaulting to the working directory so launching from a repo just works.
        /// </summary>
        private static string GetInitialRepoPath(string[]? args)
        {
            if (args is { Length: > 0 } && !string.IsNullOrWhiteSpace(args[0]))
            {
                try
                {
                    return Path.GetFullPath(args[0]);
                }
                catch (ArgumentException)
                {
                    // Fall through to the working directory for an unusable path.
                }
                catch (NotSupportedException)
                {
                }
            }

            return Directory.GetCurrentDirectory();
        }
    }
}
