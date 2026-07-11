using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using SlimPenHotkeys.Core;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SlimPenHotkeys;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ApplyThemeIcons();

        // Size once the content is loaded, when the rasterization scale
        // (DPI) is reliable. GetDpiForWindow returns 96 too early.
        RootFrame.Loaded += OnRootLoaded;

        // Navigate the root frame to the main page on startup.
        RootFrame.Navigate(typeof(MainPage));
    }

    /// <summary>
    /// Applies the theme-appropriate app icon to the title bar (which follows the
    /// app theme) and to the window/taskbar (which follows the system/shell theme),
    /// so the icon stays visible on both light and dark backgrounds. Safe to call
    /// again whenever the Windows theme changes. Absolute paths are used so the
    /// icon loads regardless of the process working directory.
    /// </summary>
    public void ApplyThemeIcons()
    {
        try
        {
            AppTitleBar.IconSource = new ImageIconSource
            {
                ImageSource = new BitmapImage(ThemeIcons.AppIconUri()),
            };
        }
        catch { /* non-fatal: title bar keeps its previous icon */ }

        try
        {
            string iconPath = ThemeIcons.SystemIconPath();
            if (System.IO.File.Exists(iconPath))
            {
                AppWindow.SetIcon(iconPath);
            }
        }
        catch { /* non-fatal: fall back to the default window icon */ }
    }

    private void OnRootLoaded(object sender, RoutedEventArgs e)
    {
        RootFrame.Loaded -= OnRootLoaded;
        SizeToContent();
    }

    /// <summary>
    /// Sizes the window client area to the minimum width the content needs
    /// (content MaxWidth 460 + 20px margins each side + scrollbar allowance),
    /// scaled for the current display DPI. Height is left tall enough that
    /// content scrolls rather than clips.
    /// </summary>
    private void SizeToContent()
    {
        const double logicalWidth = 190 + 460 + 40 + 20;  // pane + content + margins + scrollbar
        const double logicalHeight = 470;

        double scale = RootFrame.XamlRoot?.RasterizationScale ?? 1.0;
        if (scale <= 0) scale = 1.0;

        AppWindow.ResizeClient(new SizeInt32(
            (int)Math.Round(logicalWidth * scale),
            (int)Math.Round(logicalHeight * scale)));
    }

    /// <summary>The hosted settings page, once navigated.</summary>
    public MainPage? Page => RootFrame.Content as MainPage;
}
