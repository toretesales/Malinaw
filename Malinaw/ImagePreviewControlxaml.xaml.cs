using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Malinaw
{
    public sealed partial class ImagePreviewControl : UserControl
    {
        public ImagePreviewControl()
        {
            this.InitializeComponent();
        }

        // Make the PreviewImageControl property public
        public Image PreviewImageControl => PreviewImage;


        // Event handler for when the grid is tapped
        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var grid = sender as Grid;

            if (grid != null)
            {
                // Get the Image element inside the Grid
                var image = grid.FindName("PreviewImage") as Image;

                if (image != null)
                {
                    // Show the flyout attached to the Image
                    var flyoutBase = FlyoutBase.GetAttachedFlyout(image);
                    flyoutBase.ShowAt(image);
                }
            }
        }


        // Event handler for when the image is tapped
        private void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var image = sender as Image;
            if (image != null)
            {
                // Show the flyout when the image is tapped
                var flyoutBase = FlyoutBase.GetAttachedFlyout(image);
                flyoutBase.ShowAt(image);
            }
        }

        private void FlyoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Find the parent ImagePreviewControl
            var previewControl = this;

            // Get the parent ConvertPage (where StackImagesGrid exists)
            var parentPage = FindParent<pageConvert>(this);
            if (parentPage != null)
            {
                parentPage.RemoveImage(previewControl);
            }
        }

        // Helper method to find the parent Grid in the visual tree
        private T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            // Traverse the visual tree upwards to find the parent of the specific type
            while (parentObject != null && parentObject is not T)
            {
                parentObject = VisualTreeHelper.GetParent(parentObject);
            }

            return parentObject as T; // The caller should handle the null case
        }



    }

}
