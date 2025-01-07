using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Composition.SystemBackdrops;
using Windows.UI.ApplicationSettings;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Microsoft.UI;
using Microsoft.UI.Xaml.Documents;
using Windows.System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Graphics.Imaging;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Malinaw
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            // Set the backdrop for the application window
            SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.BaseAlt };

            // Extend content into the title bar for customization
            this.ExtendsContentIntoTitleBar = true;

            // Subscribe to the navigation view's SelectionChanged event
            NavView.SelectionChanged += OnNavigationViewSelectionChanged;

            // Ensure the navigation pane is closed (or open, based on preference) at startup
            NavView.IsPaneOpen = false; // Change to 'true' if you want it initially expanded.

            // Programmatically select and navigate to the "Convert" page at startup
            var convertItem = NavView.MenuItems.OfType<NavigationViewItem>()
                                .FirstOrDefault(item => item.Tag?.ToString() == "convert");
            if (convertItem != null)
            {
                NavView.SelectedItem = convertItem;
                ContentFrame.Navigate(typeof(pageConvert)); // Explicitly navigate to ConvertPage
            }

            // Set window size
            AppWindow.Resize(new SizeInt32(1208, 768));

            // Reference to the main window
            App.MainWindow = this;

            // Set the window icon
            var appWindow = this.AppWindow;
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Square44x44Logo.scale-200.png");
            appWindow.SetIcon(iconPath);

        }

 


        // Event handler for the navigation view's selection changes
        private void OnNavigationViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (ContentFrame != null)
            {
                if (args.IsSettingsSelected)
                {
                    // Navigate to the settings/about page
                    ContentFrame.Navigate(typeof(pageAbout));
                }
                else
                {
                    // Handle navigation based on the selected item's tag
                    if (args.SelectedItem is NavigationViewItem item)
                    {
                        switch (item.Tag)
                        {
                            case "convert":
                                ContentFrame.Navigate(typeof(pageConvert));
                                break;
                        }
                    }
                }
            }
        }



    }
}
