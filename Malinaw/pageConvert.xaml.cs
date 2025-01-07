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
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.Graphics.Imaging;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Windows.UI.Popups;
using Microsoft.UI;
using ImageMagick;
using System.Text;
using Microsoft.UI.Xaml.Media.Animation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Malinaw
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class pageConvert : Page
    {

        // The collection that holds our images
        public ObservableCollection<ImagePreviewControl> ImageCollection { get; set; }
        public pageConvert()
        {
            this.InitializeComponent();


            // Attach an event handler to show the Flyout on click
            NoticeCorruptionLink.Click += NoticeCorruptionLink_Click;


            // Initialize the ObservableCollection
            ImageCollection = new ObservableCollection<ImagePreviewControl>();

            // Example of adding ImagePreviewControls
            for (int i = 0; i < 10; i++)
            {
                var imageControl = new ImagePreviewControl();
                ImageCollection.Add(imageControl);
            }

            // Bind the ObservableCollection to the UI (DataContext)
            DataContext = this;
        }

        private ObservableCollection<StorageFile> ImageFileCollection = new ObservableCollection<StorageFile>(); // Collection for the files.
        private ObservableCollection<ImagePreviewControl> ImagePreviewControls = new ObservableCollection<ImagePreviewControl>(); // Collection for the controls.
        private object? previousSelectedItem;
        private bool isSelectionRestoring = false;




        private async void cmbFilterWhatFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if the selection is being restored to avoid triggering the event again
            if (isSelectionRestoring)
            {
                return;
            }

            // Save the current selection if it's not already stored
            if (previousSelectedItem == null)
            {
                previousSelectedItem = cmbFilterWhatFormat.SelectedItem;
            }

            // Ensure ComboBox is properly initialized and an item is selected
            if (cmbFilterWhatFormat.SelectedItem != null && cmbFilterWhatFormat.SelectedIndex != -1)
            {
                // Check if the selected item in both ComboBoxes matches
                var selectedFormatFilterItem = cmbFilterWhatFormat.SelectedItem as ComboBoxItem;
                var selectedFormatConvertItem = cmbConvertTo.SelectedItem as ComboBoxItem;

                // Safely extract the content from ComboBox items
                string selectedFormatFilter = selectedFormatFilterItem?.Content?.ToString();
                string selectedFormatConvert = selectedFormatConvertItem?.Content?.ToString();

                // If either format is null or empty, provide a fallback empty string or appropriate action.
                selectedFormatFilter = selectedFormatFilter ?? string.Empty;
                selectedFormatConvert = selectedFormatConvert ?? string.Empty;

                // If the formats are the same, inform the user with a dialog
                if (!string.IsNullOrEmpty(selectedFormatFilter) && !string.IsNullOrEmpty(selectedFormatConvert) && selectedFormatFilter == selectedFormatConvert)
                {
                    // Create a ContentDialog to inform the user
                    ContentDialog formatWarningDialog = new ContentDialog
                    {
                        Title = "Format Warning",
                        Content = "The selected formats in both ComboBoxes are the same. Please choose different formats to avoid redundant conversion.",
                        PrimaryButtonText = "Ok",
                        XamlRoot = (sender as FrameworkElement)?.XamlRoot
                    };

                    // Show the dialog and wait for user acknowledgment
                    await formatWarningDialog.ShowAsync();
                    return; // Exit the method to prevent further processing
                }

                // If the format is changed to something other than JPEG, disable the tgDecodeWithJ2P
                if (selectedFormatFilter != "JPEG" && tgDecodeWithJ2P.IsEnabled)
                {
                    tgDecodeWithJ2P.IsEnabled = false; // Disable the tgDecodeWithJ2P
                    tgDecodeWithJ2P.IsOn = false;       // Set it to 'Off'
                }
                else if (selectedFormatFilter == "JPEG")
                {
                    tgDecodeWithJ2P.IsEnabled = true;  // Enable the tgDecodeWithJ2P for JPEG
                }

                // Ensure to call UpdateJpeg2PngToggleSwitch based on format selections
                UpdateJpeg2PngToggleSwitch();

                // Check if images have already been added
                if (ImageFileCollection.Count > 0)
                {
                    // Create a new ContentDialog to prompt the user about the format change
                    ContentDialog formatChangeDialog = new ContentDialog
                    {
                        Title = "Change Format",
                        Content = "Changing the format will reset the selected images. Do you want to proceed?",
                        PrimaryButtonText = "Yes",
                        CloseButtonText = "No",
                        XamlRoot = (sender as FrameworkElement)?.XamlRoot
                    };

                    // Show the ContentDialog and get the result
                    ContentDialogResult result = await formatChangeDialog.ShowAsync();

                    // Handle the result of the ContentDialog
                    if (result == ContentDialogResult.Primary)
                    {
                        // User chose to proceed with the format change
                        // Call the method to remove all images (awaited)
                        RemoveAllImages();
                        // Enable the Select Images button
                        btnSelectImages.IsEnabled = true;
                        btnRemoveAllImages.IsEnabled = false;
                        Logger.Log("Format changed. Images removed. Button enabled.");
                    }
                    else
                    {
                        // User chose to cancel the format change
                        // Temporarily disable the event handler
                        isSelectionRestoring = true;
                        // Restore the previous selection
                        cmbFilterWhatFormat.SelectedItem = previousSelectedItem;
                        // Re-enable the event handler
                        isSelectionRestoring = false;
                        Logger.Log("Format change cancelled. Previous selection restored.");
                    }
                }
                else
                {
                    // No images have been added, so enable the Select Images button
                    btnSelectImages.IsEnabled = true;
                    Logger.Log("ComboBox item selected. No images to remove. Button enabled.");
                }
            }
            else
            {
                // ComboBox is not properly initialized or no item is selected
                // Disable the Select Images button
                btnSelectImages.IsEnabled = false;
                Logger.Log("ComboBox selection invalid. Button disabled.");
            }

            // Reset previousSelectedItem if it matches the current selection
            if (!isSelectionRestoring)
            {
                previousSelectedItem = cmbFilterWhatFormat.SelectedItem;
            }
        }



        // Method to check the conditions and enable/disable tgDecodeWithJ2P based on selected format.
        public void UpdateJpeg2PngToggleSwitch()
        {
            // Get the selected value of cmbFilterWhatFormat and cmbConvertTo
            var selectedFilterFormat = cmbFilterWhatFormat.SelectedItem as ComboBoxItem;
            var selectedConvertFormat = cmbConvertTo.SelectedItem as ComboBoxItem;

            // Check if 'JPEG' is selected in cmbFilterWhatFormat and 'PNG' in cmbConvertTo
            bool isJpegSelected = selectedFilterFormat?.Content?.ToString().Equals("JPEG", StringComparison.OrdinalIgnoreCase) == true;
            bool isPngSelected = selectedConvertFormat?.Content?.ToString().Equals("PNG", StringComparison.OrdinalIgnoreCase) == true;

            if (isJpegSelected && isPngSelected)
            {
                // Enable and show the toggle switch when conditions are met
                tgDecodeWithJ2P.IsEnabled = true;
                tgDecodeWithJ2P.Visibility = Visibility.Visible;  // Show the toggle switch
            }
            else
            {
                // Disable and hide the toggle switch when condition doesn't match
                tgDecodeWithJ2P.IsEnabled = false;
                tgDecodeWithJ2P.IsOn = false;       // Make sure it stays "off"
                tgDecodeWithJ2P.Visibility = Visibility.Collapsed;  // Hide the toggle switch
            }
        }

        private async void cmbConvertTo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ensure ComboBox is properly initialized and an item is selected
            if (cmbConvertTo.SelectedItem != null && cmbConvertTo.SelectedIndex != -1)
            {
                // Get the selected item content from both ComboBoxes, providing a default value if null
                string selectedConvertTo = (cmbConvertTo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
                string selectedFilterWhatFormat = (cmbFilterWhatFormat.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

                // Check if the selected items in both ComboBoxes match
                if (selectedConvertTo == selectedFilterWhatFormat)
                {
                    // Create a new ContentDialog to show the error message
                    ContentDialog errorDialog = new ContentDialog
                    {
                        Title = "Invalid Selection",
                        Content = "The conversion format cannot be the same as the format being added. Please select a different format.",
                        CloseButtonText = "Ok",
                        // Set the XamlRoot to avoid the XamlRoot exception
                        XamlRoot = (sender as FrameworkElement)?.XamlRoot
                    };

                    // Show the ContentDialog
                    await errorDialog.ShowAsync();

                    // Clear the selection in cmbConvertTo
                    cmbConvertTo.SelectedIndex = -1;

                    // Log the error
                    Logger.Log("Invalid conversion format selection. Selection cleared.");
                }
                else
                {
                    // Log the valid selection
                    Logger.Log($"Conversion format selected: {selectedConvertTo}");

                    // Now that the ComboBoxes have valid selections, call the method to update the toggle switch
                    UpdateJpeg2PngToggleSwitch();
                }
            }
            else
            {
                // Log the invalid selection
                Logger.Log("Invalid conversion format selection. No item selected.");
            }
        }




        private async void btnSelectImages_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            // Get the selected format from the ComboBox
            var selectedFormat = cmbFilterWhatFormat.SelectedItem as ComboBoxItem;

            // If a specific format is selected, add it to the picker filter
            if (selectedFormat != null)
            {
                string selectedContent = selectedFormat.Content?.ToString()?.ToLower() ?? string.Empty;

                if (selectedContent == "jpeg") // Add alias for JPEG
                {
                    picker.FileTypeFilter.Add(".jpg");
                    picker.FileTypeFilter.Add(".jpeg");
                }
                else
                {
                    picker.FileTypeFilter.Add($".{selectedContent}");
                }
            }
            else
            {
                // If no specific format is selected, allow all common formats
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".tiff");
                picker.FileTypeFilter.Add(".bmp");
                picker.FileTypeFilter.Add(".ico");
                picker.FileTypeFilter.Add(".gif");
                picker.FileTypeFilter.Add(".webp");
                picker.FileTypeFilter.Add(".pdf");
            }

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();

            if (files != null && files.Count > 0)
            {
                List<StorageFile> fileList = files.ToList();

                // Track if new files were added to determine if scrolling is needed
                bool filesAdded = false;

                foreach (var file in fileList)
                {
                    if (!ImageFileCollection.Any(f => f.Path.Equals(file.Path, StringComparison.OrdinalIgnoreCase)))
                    {
                        ImageFileCollection.Add(file);
                        filesAdded = true;
                    }
                }

                await AddImagesToGrid(ImageFileCollection.ToList());

                // Highlight and scroll if new files are added
                if (filesAdded)
                {
                    await HighlightAndScrollToGridAsync(scrollControls, stackSelectFolder);
                }

                // Update TextBlocks with selection summary
                if (ImageFileCollection.Count == 1)
                {
                    tbSelectedImages.Text = ImageFileCollection[0].Name;

                    var folder = await ImageFileCollection[0].GetParentAsync();
                    string folderName = folder?.Name ?? "Unknown folder";
                    tbPathOrNumber.Text = folderName;

                    Logger.Log($"Selected folder: {folderName}");
                }
                else
                {
                    tbSelectedImages.Text = "Multiple files are selected";
                    tbPathOrNumber.Text = $"{ImageFileCollection.Count} files selected";

                    Logger.Log($"Multiple files selected: {ImageFileCollection.Count} files");
                }

                AdjustGridPositions();
            }

            UpdateConvertButtonState();
            ResetStatus();
        }


        private async Task HighlightAndScrollToGridAsync(ScrollViewer scrollViewer, Grid targetGrid)
        {
            if (scrollViewer == null || targetGrid == null)
                return;

            // Scroll to make the targetGrid fully visible
            scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);

            // Prepare for highlight animation
            var originalBrush = targetGrid.Background as SolidColorBrush;
            var highlightBrush = Application.Current.Resources["AccentFillColorTertiaryBrush"] as SolidColorBrush;

            if (highlightBrush == null || originalBrush == null)
                return;

            // Create animation
            var highlightAnimation = new ColorAnimation
            {
                From = originalBrush.Color,
                To = highlightBrush.Color,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)), // Highlight fade-in effect
                AutoReverse = true // Automatically return to the original color
            };

            // Create and apply Storyboard
            var storyboard = new Storyboard();

            Storyboard.SetTarget(highlightAnimation, targetGrid);
            Storyboard.SetTargetProperty(highlightAnimation, "(Grid.Background).(SolidColorBrush.Color)");
            storyboard.Children.Add(highlightAnimation);

            // Start the storyboard
            storyboard.Begin();

            // Wait for the duration of the highlight (includes reverse)
            await Task.Delay(2000); // 300ms forward + 300ms reverse
        }


        private async void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFolder folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                // Update TextBlocks with folder name and path
                tbSelFolderName.Text = folder.Name;
                tbConvertedPath.Text = folder.Path;

                // Log the selected output folder path
                Logger.Log($"Selected output folder: {folder.Path}");
            }
            else
            {
                tbSelFolderName.Text = "No folder selected";
                tbConvertedPath.Text = "Please choose a folder";

                // Log the action when no folder is selected
                Logger.Log("No folder was selected for output.");
            }

            // Call to update the Convert button state
            UpdateConvertButtonState();
            linkOpenCurrentFolder.IsEnabled = true;
        }


        private void UpdateConvertButtonState()
        {
            // Enable the Convert button only if both images and folder are selected
            btnConvert.IsEnabled = ImageFileCollection.Count > 0 && !string.IsNullOrEmpty(tbConvertedPath.Text) && tbConvertedPath.Text != "Please choose a folder";
        }

        private void UpdateRemoveButtonState()
        {
            // Enable the Remove All Images button only if there are images in the grid
    
        }

        private void ResetStatus()
        {
            // Reset the status text
            tbStatus.Text = "Malinaw is Ready";
            tbStatusMessage.Text = "Add an image then a folder and click Convert.";

            // Reset the background color of the Grid to default (using a neutral color like CautionBackgroundBrush)
            gridStatus.Background = Application.Current.Resources["SystemFillColorAttentionBackgroundBrush"] as Brush; // Or any other neutral color you prefer
        }


        private async Task AddImagesToGrid(List<StorageFile> files)
        {
            // First, we clear the existing controls from the grid and collections
            StackImagesGrid.Children.Clear();
            ImageFileCollection.Clear();
            ImagePreviewControls.Clear();

            NoticeCorruptionLink.Visibility = Visibility.Collapsed;

            LoadingContainer.Visibility = Visibility.Visible;

            int columnCount = 3;  // Set number of columns
            int row = 0, column = 0;

            foreach (var file in files)
            {
                var imageControl = new ImagePreviewControl
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(3)
                };

                // Load and process the image (code previously mentioned for handling images)

                using (var stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    var decoder = await BitmapDecoder.CreateAsync(stream);

                    double targetSize = 200;
                    double aspectRatio = (double)decoder.PixelWidth / decoder.PixelHeight;

                    uint scaledWidth, scaledHeight;
                    if (aspectRatio > 1) // Landscape
                    {
                        scaledWidth = (uint)targetSize;
                        scaledHeight = (uint)(targetSize / aspectRatio);
                    }
                    else // Portrait or Square
                    {
                        scaledWidth = (uint)(targetSize * aspectRatio);
                        scaledHeight = (uint)targetSize;
                    }

                    var bitmap = new WriteableBitmap((int)scaledWidth, (int)scaledHeight);
                    var pixelData = await decoder.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied,
                        new BitmapTransform()
                        {
                            ScaledWidth = scaledWidth,
                            ScaledHeight = scaledHeight
                        },
                        ExifOrientationMode.RespectExifOrientation,
                        ColorManagementMode.ColorManageToSRgb
                    );

                    byte[] pixels = pixelData.DetachPixelData();
                    using (Stream bitmapStream = bitmap.PixelBuffer.AsStream())
                    {
                        await bitmapStream.WriteAsync(pixels, 0, pixels.Length);
                    }

                    imageControl.PreviewImageControl.Source = bitmap;
                }

                StackImagesGrid.Children.Add(imageControl);
                ImageFileCollection.Add(file);  // Store the reference to the file
                ImagePreviewControls.Add(imageControl);  // Store the reference to the image control

                Grid.SetRow(imageControl, row);
                Grid.SetColumn(imageControl, column);

                column++;
                if (column >= columnCount)
                {
                    column = 0;
                    row++;
                }

                AdjustGridPositions();
                // Update the TextBlocks as each image is added
                UpdateTextBlocks();
            }

            AdjustGridPositions();

            LoadingContainer.Visibility = Visibility.Collapsed;
            NoticeCorruptionLink.Visibility = Visibility.Visible;
            btnConvert.IsEnabled = true;
            btnRemoveAllImages.IsEnabled = true;
        }


        private async Task CreateImageControl(ImagePreviewControl imageControl, StorageFile file)
        {
            using (var stream = await file.OpenAsync(FileAccessMode.Read))
            {
                var decoder = await BitmapDecoder.CreateAsync(stream);

                double targetSize = 200;
                double aspectRatio = (double)decoder.PixelWidth / decoder.PixelHeight;

                // Calculate the target width and height to scale the image to the desired size
                uint scaledWidth, scaledHeight;
                if (aspectRatio > 1) // Landscape
                {
                    scaledWidth = (uint)targetSize;
                    scaledHeight = (uint)(targetSize / aspectRatio);
                }
                else // Portrait or Square
                {
                    scaledWidth = (uint)(targetSize * aspectRatio);
                    scaledHeight = (uint)targetSize;
                }

                var transform = new BitmapTransform()
                {
                    ScaledWidth = scaledWidth,
                    ScaledHeight = scaledHeight
                };

                // Retrieve the pixel data
                var pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    transform,
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.ColorManageToSRgb
                );

                byte[] pixels = pixelData.DetachPixelData();

                // Now create a WriteableBitmap with the scaled size
                var bitmap = new WriteableBitmap((int)scaledWidth, (int)scaledHeight);

                // Fill the WriteableBitmap with the pixel data asynchronously
                await FillBitmapAsync(bitmap, pixels);

                // Set the processed bitmap to the image control's Source
                imageControl.PreviewImageControl.Source = bitmap;
            }
        }

        private async Task FillBitmapAsync(WriteableBitmap bitmap, byte[] pixels)
        {
            // Write the pixel data to the WriteableBitmap asynchronously
            using (var stream = bitmap.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(pixels, 0, pixels.Length);
            }
        }


        private void AdjustGridPositions()
        {
            int columnCount = 3; // Ensure the grid has a fixed number of columns
            int rowCount = (int)Math.Ceiling((double)ImagePreviewControls.Count / columnCount);

            // Clear existing definitions
            StackImagesGrid.RowDefinitions.Clear();
            StackImagesGrid.ColumnDefinitions.Clear();

            // Create the necessary column definitions
            for (int c = 0; c < columnCount; c++)
            {
                StackImagesGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            }

            // Create the necessary row definitions
            for (int r = 0; r < rowCount; r++)
            {
                StackImagesGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            }

            int row = 0;
            int column = 0;

            // Loop through the controls in the ImagePreviewControls list and adjust their positions
            foreach (var control in ImagePreviewControls)
            {
                Grid.SetRow(control, row);
                Grid.SetColumn(control, column);

                column++;
                if (column >= columnCount)
                {
                    column = 0;
                    row++;
                }
            }
        }

        public void RemoveImage(ImagePreviewControl controlToRemove)
        {
            // Find the index of the control to remove from the ImagePreviewControls collection
            int index = ImagePreviewControls.IndexOf(controlToRemove);

            if (index >= 0)
            {
                // Remove the image control from the grid and the corresponding file from the collection
                StackImagesGrid.Children.Remove(controlToRemove);

                // Also remove the file from ImageFileCollection
                ImageFileCollection.RemoveAt(index);

                // Remove the control from ImagePreviewControls list
                ImagePreviewControls.RemoveAt(index);

                Logger.Log("An image was removed through it's own ImagePreviewControl control.");

                // Reassign positions of the remaining elements to adjust the layout
                AdjustGridPositions();

                // Update TextBlocks
                UpdateTextBlocks();
                UpdateConvertButtonState();
                UpdateRemoveButtonState();
                

            }
        }

        private async void UpdateTextBlocks()
        {
            if (ImageFileCollection.Count == 0)
            {
                tbSelectedImages.Text = "No images selected";
                tbPathOrNumber.Text = "No images selected";
            }
            else if (ImageFileCollection.Count == 1)
            {
                tbSelectedImages.Text = ImageFileCollection[0].Name;

                // Get the parent folder of the selected file
                var folder = await ImageFileCollection[0].GetParentAsync();

                // Get the name of the folder (not the full path)
                string folderName = folder?.Name ?? "Unknown folder"; // Handle null if folder is not available
                tbPathOrNumber.Text = folderName; // Show only the folder name
            }
            else
            {
                tbSelectedImages.Text = "Multiple files are selected";
                tbPathOrNumber.Text = $"{ImageFileCollection.Count} files are selected";
            }
        }


        private void RemoveAllImages()
        {
            // Clear the grid and collections
            StackImagesGrid.Children.Clear();
            ImageFileCollection.Clear();
            ImagePreviewControls.Clear();

            // Optionally hide or disable any related UI elements if needed
            LoadingContainer.Visibility = Visibility.Collapsed;
            NoticeCorruptionLink.Visibility = Visibility.Collapsed;
            btnConvert.IsEnabled = false;

            Logger.Log("All images were removed through btnRemoveAllImages.");

            // Update TextBlocks
            UpdateTextBlocks();
            ResetStatus();
        }


        private async void btnRemoveAllImages_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await ClearImagesDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                RemoveAllImages();
                btnRemoveAllImages.IsEnabled = false;
            }
        }


        // Handle the click event of the HyperlinkButton
        private void NoticeCorruptionLink_Click(object sender, RoutedEventArgs e)
        {
            // Show the TeachingTip when the hyperlink button is clicked
            NoticeTeachingTip.IsOpen = true; // Open the teaching tip
        }

        // Code for btnConvert_Click
        private async void btnConvert_Click(object sender, RoutedEventArgs e)
        {
            // Disable the Convert button to avoid multiple clicks during conversion
            btnConvert.IsEnabled = false;

            var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

            try
            {
                string outputFolder = tbConvertedPath.Text;

                // Log the beginning of the conversion process
                Logger.Log("Starting image conversion...");

                // Prepare the UI
                dispatcherQueue.TryEnqueue(() =>
                {
                    tbStatus.Text = "Preparing Conversion";
                    tbStatusMessage.Text = "Loading selected files and preparing for conversion...";
                    gridStatus.Background = Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"] as Brush;
                });

                // Show progress bar
                indProgressBar.Visibility = Visibility.Visible;

                // Lists to track results
                var convertedFiles = new List<string>();
                var skippedFiles = new List<string>();
                var failedFiles = new List<(string FilePath, string ErrorMessage)>();

                // Get selected format from ComboBox
                string targetFormat = (cmbConvertTo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "PNG"; // Default to PNG if null

                if (string.IsNullOrEmpty(targetFormat))
                {
                    targetFormat = "PNG"; // Default format
                    Logger.Log("No format selected, defaulting to PNG.");
                }

                // Process each selected image
                foreach (var inputFile in ImageFileCollection)
                {
                    if (File.Exists(inputFile.Path))
                    {
                        Logger.Log($"Processing: {inputFile.Path}");

                        string sanitizedFileName = GetSanitizedFileName(Path.GetFileNameWithoutExtension(inputFile.Path)) + "." + targetFormat.ToLowerInvariant();
                        string sanitizedFilePath = Path.Combine(outputFolder, sanitizedFileName);

                        if (File.Exists(sanitizedFilePath))
                        {
                            // Skip conversion if file exists
                            skippedFiles.Add(inputFile.Path);
                            string skipMessage = $"File already exists: {sanitizedFilePath}";
                            Logger.Log(skipMessage);

                            dispatcherQueue.TryEnqueue(() =>
                            {
                                tbStatusMessage.Text = skipMessage;
                                gridStatus.Background = Application.Current.Resources["SystemFillColorCautionBackgroundBrush"] as Brush; // Caution background
                            });
                            continue; // Skip this file and move to the next
                        }

                        // Update UI for current file processing
                        dispatcherQueue.TryEnqueue(() =>
                        {
                            tbStatusMessage.Text = $"Converting {Path.GetFileName(inputFile.Path)}...";
                            gridStatus.Background = Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"] as Brush; // Neutral background
                        });

                        (bool isSuccess, string errorMessage) result;
                        if (tgDecodeWithJ2P.IsOn)
                        {
                            result = await RunJpeg2PngConversionAsync(inputFile.Path, outputFolder);
                        }
                        else
                        {
                            result = await RunMagickConversionAsync(inputFile.Path, outputFolder, targetFormat);
                        }

                        dispatcherQueue.TryEnqueue(() =>
                        {
                            if (result.isSuccess)
                            {
                                convertedFiles.Add(inputFile.Path);
                                string successMessage = $"Converted {Path.GetFileName(inputFile.Path)} successfully.";
                                tbStatusMessage.Text = successMessage;
                                gridStatus.Background = Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"] as Brush;
                                Logger.Log(successMessage);
                            }
                            else
                            {
                                failedFiles.Add((inputFile.Path, result.errorMessage));
                                string failureMessage = $"Error converting {Path.GetFileName(inputFile.Path)}: {result.errorMessage}";
                                tbStatusMessage.Text = failureMessage;
                                gridStatus.Background = Application.Current.Resources["SystemFillColorCriticalBackgroundBrush"] as Brush;
                                Logger.Log(failureMessage);
                            }
                        });
                    }
                    else
                    {
                        // File not found
                        failedFiles.Add((inputFile.Path, "File not found"));
                        string missingFileMessage = $"File not found: {inputFile.Path}";
                        Logger.Log(missingFileMessage);

                        dispatcherQueue.TryEnqueue(() =>
                        {
                            tbStatusMessage.Text = missingFileMessage;
                            gridStatus.Background = Application.Current.Resources["SystemFillColorCriticalBackgroundBrush"] as Brush; // Critical background
                        });
                    }
                }

                // Finalize processing and update UI
                dispatcherQueue.TryEnqueue(() =>
                {
                    tbStatus.Text = "Conversion Finished";

                    string finalMessage = $"Conversion complete. Converted: {convertedFiles.Count}, Skipped: {skippedFiles.Count}, Failed: {failedFiles.Count}.";
                    tbStatusMessage.Text = finalMessage;

                    if (failedFiles.Count > 0)
                    {
                        gridStatus.Background = Application.Current.Resources["SystemFillColorCautionBackgroundBrush"] as Brush; // Caution background for partial failures
                    }
                    else if (skippedFiles.Count > 0)
                    {
                        gridStatus.Background = Application.Current.Resources["SystemFillColorSuccessBackgroundBrush"] as Brush; // Success with skips
                    }
                    else
                    {
                        gridStatus.Background = Application.Current.Resources["SystemFillColorSuccessBackgroundBrush"] as Brush; // Full success
                    }

                    Logger.Log(finalMessage);

                    // Log file details
                    Logger.Log("Successfully converted files:");
                    foreach (var file in convertedFiles)
                    {
                        Logger.Log($"- {file}");
                    }

                    Logger.Log("Skipped files:");
                    foreach (var file in skippedFiles)
                    {
                        Logger.Log($"- {file}");
                    }

                    Logger.Log("Failed files:");
                    foreach (var (file, error) in failedFiles)
                    {
                        Logger.Log($"- {file} (Error: {error})");
                    }
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Unexpected error: {ex.Message}";
                Logger.Log(errorMessage);

                dispatcherQueue.TryEnqueue(() =>
                {
                    tbStatus.Text = "Unexpected Error";
                    tbStatusMessage.Text = errorMessage;
                    gridStatus.Background = Application.Current.Resources["SystemFillColorCriticalBackgroundBrush"] as Brush; // Critical background
                });
            }
            finally
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    indProgressBar.Visibility = Visibility.Collapsed;
                });
                btnConvert.IsEnabled = true;

                Logger.Log("Conversion process finished.");
            }
        }


        private async Task<(bool isSuccess, string errorMessage)> RunMagickConversionAsync(string inputFilePath, string outputFolder, string targetFormat)
        {
            bool isSuccess = false;
            string errorMessage = string.Empty;

            try
            {
                if (!File.Exists(inputFilePath))
                {
                    errorMessage = $"Input file does not exist: {inputFilePath}";
                    Logger.Log(errorMessage);
                    return (false, errorMessage);
                }

                if (!Directory.Exists(outputFolder))
                {
                    Logger.Log($"Output directory does not exist. Creating directory: {outputFolder}");
                    Directory.CreateDirectory(outputFolder);
                }

                string originalFileName = Path.GetFileName(inputFilePath);
                string sanitizedFileName = GetSanitizedFileName(originalFileName);
                string outputFilePath = Path.Combine(outputFolder, sanitizedFileName);

                // Set the desired output extension based on the selected format
                outputFilePath = Path.ChangeExtension(outputFilePath, GetImageExtensionFromFormat(targetFormat));

                Logger.Log($"Starting conversion for file: {inputFilePath} to {outputFilePath}");

                await Task.Run(() =>
                {
                    using var image = new MagickImage(inputFilePath);

                    // Select the format based on the ComboBox selection
                    image.Format = GetImageFormatFromComboBox(targetFormat);
                    image.Write(outputFilePath); // Write to output
                });

                isSuccess = true;
                Logger.Log($"Successfully converted to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                errorMessage = $"Error during conversion: {ex.Message}";
                Logger.Log(errorMessage);
            }

            return (isSuccess, errorMessage);
        }

        private string GetSanitizedFileName(string fileName)
        {
            string sanitizedFileName = fileName.ToLowerInvariant();
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + " ";
            sanitizedFileName = new string(sanitizedFileName
                .Where(ch => !char.IsSurrogate(ch) && !invalidChars.Contains(ch))
                .ToArray());
            sanitizedFileName = sanitizedFileName.Replace(" ", "_");

            return sanitizedFileName;
        }

        // Convert the string format (based on ComboBox selection) to a MagickFormat enum
        private MagickFormat GetImageFormatFromComboBox(string format)
        {
            return format switch
            {
                "JPEG" => MagickFormat.Jpeg,
                "PNG" => MagickFormat.Png,
                "TIFF" => MagickFormat.Tiff,
                "BMP" => MagickFormat.Bmp,
                "ICO" => MagickFormat.Ico,
                "GIF" => MagickFormat.Gif,
                "WEBP" => MagickFormat.WebP,
                "PDF" => MagickFormat.Pdf,
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };
        }

        // Return the file extension corresponding to the target format
        private string GetImageExtensionFromFormat(string format)
        {
            return format switch
            {
                "JPEG" => ".jpg",
                "PNG" => ".png",
                "TIFF" => ".tiff",
                "BMP" => ".bmp",
                "ICO" => ".ico",
                "GIF" => ".gif",
                "WEBP" => ".webp",
                "PDF" => ".pdf",
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };
        }

        private async Task<(bool isSuccess, string errorMessage)> RunJpeg2PngConversionAsync(string inputFilePath, string outputFolder)
        {
            bool isSuccess = false;
            string errorMessage = string.Empty;

            try
            {
                // Ensure the input file exists
                if (!File.Exists(inputFilePath))
                {
                    errorMessage = $"Input file does not exist: {inputFilePath}";
                    Logger.Log(errorMessage);
                    return (false, errorMessage);
                }

                // Ensure the output directory exists, or create it
                if (!Directory.Exists(outputFolder))
                {
                    Logger.Log($"Output directory does not exist. Creating directory: {outputFolder}");
                    Directory.CreateDirectory(outputFolder);
                }

                // Get the sanitized file name and set the output path
                string originalFileName = Path.GetFileName(inputFilePath);
                string sanitizedFileName = GetSanitizedFileName(originalFileName);
                string outputFilePath = Path.Combine(outputFolder, Path.ChangeExtension(sanitizedFileName, ".png"));

                Logger.Log($"Starting conversion for file: {inputFilePath} to {outputFilePath}");

                // Construct the command for jpeg2png.exe
                string jpeg2pngExePath = Path.Combine(Directory.GetCurrentDirectory(), "jpeg2png.exe");

                // Check if jpeg2png.exe exists
                if (!File.Exists(jpeg2pngExePath))
                {
                    errorMessage = $"jpeg2png.exe was not found in the current directory.";
                    Logger.Log(errorMessage);
                    return (false, errorMessage);
                }

                // Start a process to run the jpeg2png conversion
                var startInfo = new ProcessStartInfo
                {
                    FileName = jpeg2pngExePath,
                    Arguments = $"\"{inputFilePath}\" -o \"{outputFilePath}\"",  // Command to run jpeg2png
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Set up event handlers to capture output and errors
                startInfo.StandardOutputEncoding = Encoding.UTF8;
                startInfo.StandardErrorEncoding = Encoding.UTF8;

                // Start the process asynchronously
                var process = new Process { StartInfo = startInfo };

                // Capture any output or error messages from the process
                process.OutputDataReceived += (sender, e) => Logger.Log($"stdout: {e.Data}");
                process.ErrorDataReceived += (sender, e) => Logger.Log($"stderr: {e.Data}");

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for the process to exit
                await Task.Run(() => process.WaitForExit());

                if (process.ExitCode == 0)
                {
                    isSuccess = true;
                    Logger.Log($"Successfully converted {inputFilePath} to {outputFilePath}");
                }
                else
                {
                    errorMessage = $"Error occurred during jpeg2png conversion. Process exited with code {process.ExitCode}.";
                    Logger.Log(errorMessage);
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error during conversion: {ex.Message}";
                Logger.Log(errorMessage);
            }

            return (isSuccess, errorMessage);
        }


        private void linkOpenCurrentFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the path from tbConvertedPath and trim any whitespace
                string folderPath = tbConvertedPath.Text.Trim();

                // Log the folder path for debugging purposes
                Logger.Log($"linkOpenCurrentFolder: Attempting to open folder path: '{folderPath}'");

                // Check if the path is valid and the directory exists
                if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                {
                    // Open the folder in File Explorer
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = folderPath,
                        UseShellExecute = true, // Required for opening directories
                        Verb = "open" // Ensures the folder is opened
                    });
                }
                else
                {
                    // Log a message or inform the user (here it's a silent log for debugging)
                    Logger.Log("linkOpenCurrentFolder: Invalid or nonexistent folder path.");
                }
            }
            catch (Exception ex)
            {
                // Log any exception (you can replace Logger.Log with a logger)
                Logger.Log($"linkOpenCurrentFolder: An error occurred while opening the folder: {ex.Message}");
            }
        }

        private void btnOpenLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the path to the logs directory
                string logsDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");

                // Check if the logs directory exists
                if (Directory.Exists(logsDirectory))
                {
                    // Open the logs directory in File Explorer
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = logsDirectory,
                        UseShellExecute = true, // Required for opening directories
                        Verb = "open" // Ensures the folder is opened
                    });
                }
                else
                {
                    // Log a message or inform the user (here it's a silent log for debugging)
                    Logger.Log("btnOpenLogs_Click: Logs directory does not exist.");
                }
            }
            catch (Exception ex)
            {
                // Log any exception (you can replace Logger.Log with a logger)
                Logger.Log($"btnOpenLogs_Click: An error occurred while opening the logs directory: {ex.Message}");
            }
        }


    }
}
