using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using NistParser.Constants;
using NistParser.Core;
using NistParser.Models;
using NistParser.Exceptions;

namespace NistParser.WPF.Windows;

/// <summary>
/// Biometrikus képek megjelenítése NIST tranzakcióból.
/// Támogatja a JPEG, PNG, és JPEG 2000 formátumok megjelenítését.
/// WSQ formátumnál figyelmeztetést jelenít meg.
/// </summary>
public partial class ImageViewerWindow : Window
{
    private readonly NistTransaction _transaction;
    private List<BiometricImage> _allImages;
    private List<ImageListItem> _displayedItems;
    private BiometricImage? _selectedImage;

    public ImageViewerWindow(NistTransaction transaction)
    {
        InitializeComponent();
        _transaction = transaction;
        _allImages = new List<BiometricImage>();
        _displayedItems = new List<ImageListItem>();

        LoadImages();
    }

    private void LoadImages()
    {
        try
        {
            _allImages = _transaction.GetAllImages().ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading images: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            _allImages = new List<BiometricImage>();
        }

        if (_allImages.Count == 0)
        {
            NoImagesPanel.Visibility = Visibility.Visible;
            ImageCountText.Text = "(0 images)";
            return;
        }

        ImageCountText.Text = $"({_allImages.Count} image{(_allImages.Count != 1 ? "s" : "")})";
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        BiometricImageType? filterType = null;

        if (FilterComboBox.SelectedIndex > 0)
        {
            filterType = FilterComboBox.SelectedIndex switch
            {
                1 => BiometricImageType.Fingerprint,
                2 => BiometricImageType.Face,
                3 => BiometricImageType.LatentFingerprint,
                4 => BiometricImageType.PalmPrint,
                5 => BiometricImageType.Iris,
                6 => BiometricImageType.Signature,
                7 => BiometricImageType.SMT,
                8 => BiometricImageType.Other,
                _ => null
            };
        }

        var filtered = filterType.HasValue
            ? _allImages.Where(img => img.ImageType == filterType.Value).ToList()
            : _allImages;

        _displayedItems = filtered.Select((img, index) => new ImageListItem(img, index + 1)).ToList();
        ImagesListBox.ItemsSource = _displayedItems;

        if (_displayedItems.Count > 0)
        {
            ImagesListBox.SelectedIndex = 0;
            NoImagesPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            NoImagesPanel.Visibility = Visibility.Visible;
            ClearImageDisplay();
        }
    }

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_allImages != null)
            ApplyFilter();
    }

    private void ImagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ImagesListBox.SelectedItem is ImageListItem item)
        {
            DisplayImage(item.Image);
        }
    }

    private void DisplayImage(BiometricImage image)
    {
        _selectedImage = image;

        // Show panels
        NoImageSelectedPanel.Visibility = Visibility.Collapsed;
        ImageDisplayPanel.Visibility = Visibility.Visible;
        MetadataPanel.Visibility = Visibility.Visible;

        // Reset overlays
        WsqWarningPanel.Visibility = Visibility.Collapsed;
        NonDisplayablePanel.Visibility = Visibility.Collapsed;
        ImageDisplay.Source = null;

        // Try to display the image
        if (image.IsNativelyWebCompatible)
        {
            // JPEG or PNG — display directly
            TryDisplayImageData(image.RawImageData);
        }
        else if (image.Compression == CompressionAlgorithm.WSQ)
        {
            // WSQ — show warning
            WsqWarningPanel.Visibility = Visibility.Visible;
            SaveImageButton.IsEnabled = false;
        }
        else if (image.IsWebConvertible)
        {
            // JPEG2000 or other convertible format
            try
            {
                var convertedData = image.GetWebCompatibleImage();
                if (!TryDisplayImageData(convertedData))
                {
                    NonDisplayablePanel.Visibility = Visibility.Visible;
                    NonDisplayableText.Text = $"A(z) {image.CompressionDescription} formátum konvertálása még nem implementált.";
                    SaveImageButton.IsEnabled = false;
                }
            }
            catch (ImageConversionNotSupportedException ex)
            {
                NonDisplayablePanel.Visibility = Visibility.Visible;
                NonDisplayableText.Text = ex.Message;
                SaveImageButton.IsEnabled = false;
            }
        }
        else
        {
            NonDisplayablePanel.Visibility = Visibility.Visible;
            NonDisplayableText.Text = $"Ismeretlen formátum: {image.CompressionDescription}";
            SaveImageButton.IsEnabled = false;
        }

        // Update metadata
        UpdateMetadata(image);
    }

    private bool TryDisplayImageData(byte[] imageData)
    {
        try
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(imageData);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            ImageDisplay.Source = bitmapImage;
            SaveImageButton.IsEnabled = true;
            return true;
        }
        catch
        {
            // Image data couldn't be decoded by WPF
            SaveImageButton.IsEnabled = false;
            return false;
        }
    }

    private void UpdateMetadata(BiometricImage image)
    {
        MetaImageType.Text = GetImageTypeDisplayName(image.ImageType);
        MetaImageSize.Text = image.GetImageSizeDescription();
        MetaCompression.Text = image.CompressionDescription ?? "N/A";
        MetaDataSize.Text = $"{image.GetDataSizeKB():F1} KB";
        MetaRecordType.Text = $"Type-{(int)image.RecordType}";
        MetaIDC.Text = image.IDC;
        MetaResolution.Text = image.ResolutionDescription ?? "N/A";

        // Position: finger, palm, or face type
        if (!string.IsNullOrEmpty(image.FingerPositionDescription))
            MetaPosition.Text = image.FingerPositionDescription;
        else if (!string.IsNullOrEmpty(image.FaceImageTypeDescription))
            MetaPosition.Text = image.FaceImageTypeDescription;
        else if (image.AdditionalMetadata.TryGetValue("PalmPosition", out var palmPos))
            MetaPosition.Text = palmPos;
        else
            MetaPosition.Text = "N/A";
    }

    private void ClearImageDisplay()
    {
        _selectedImage = null;
        NoImageSelectedPanel.Visibility = Visibility.Visible;
        ImageDisplayPanel.Visibility = Visibility.Collapsed;
        MetadataPanel.Visibility = Visibility.Collapsed;
        ImageDisplay.Source = null;
    }

    private void SaveImageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedImage == null) return;

        var mimeType = _selectedImage.WebCompatibleMimeType;
        var extension = mimeType == "image/jpeg" ? "jpg" : "png";
        var defaultName = $"biometric_{_selectedImage.ImageType}_{_selectedImage.IDC}.{extension}";

        var saveDialog = new SaveFileDialog
        {
            Title = "Save Image",
            Filter = $"Image Files (*.{extension})|*.{extension}|All Files (*.*)|*.*",
            FileName = defaultName
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                byte[] dataToSave;
                if (_selectedImage.IsNativelyWebCompatible)
                    dataToSave = _selectedImage.RawImageData;
                else
                    dataToSave = _selectedImage.GetWebCompatibleImage();

                File.WriteAllBytes(saveDialog.FileName, dataToSave);
                MessageBox.Show("Image saved successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving image: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void SaveRawButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedImage == null) return;

        var compressionExt = _selectedImage.Compression switch
        {
            CompressionAlgorithm.WSQ => "wsq",
            CompressionAlgorithm.JPEG2000_Lossy => "jp2",
            CompressionAlgorithm.JPEG2000_Lossless => "jp2",
            CompressionAlgorithm.JPEG_Lossy => "jpg",
            CompressionAlgorithm.JPEG_Lossless => "jpg",
            CompressionAlgorithm.PNG => "png",
            _ => "bin"
        };

        var defaultName = $"biometric_raw_{_selectedImage.ImageType}_{_selectedImage.IDC}.{compressionExt}";

        var saveDialog = new SaveFileDialog
        {
            Title = "Save Raw Image Data",
            Filter = $"Raw Data (*.{compressionExt})|*.{compressionExt}|All Files (*.*)|*.*",
            FileName = defaultName
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                File.WriteAllBytes(saveDialog.FileName, _selectedImage.RawImageData);
                MessageBox.Show($"Raw image data saved ({_selectedImage.GetDataSizeKB():F1} KB)", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving raw data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private static string GetImageTypeDisplayName(BiometricImageType type)
    {
        return type switch
        {
            BiometricImageType.Fingerprint => "🖐️ Ujjlenyomat / Fingerprint",
            BiometricImageType.LatentFingerprint => "🔍 Látens ujjlenyomat / Latent",
            BiometricImageType.Face => "👤 Arckép / Face",
            BiometricImageType.SMT => "🏷️ Heg/Jegy/Tetoválás / SMT",
            BiometricImageType.PalmPrint => "🤚 Tenyérnyomat / Palm Print",
            BiometricImageType.Iris => "👁️ Írisz / Iris",
            BiometricImageType.Footprint => "🦶 Lábnyomat / Footprint",
            BiometricImageType.Signature => "📝 Aláírás / Signature",
            BiometricImageType.Other => "📦 Egyéb / Other",
            _ => type.ToString()
        };
    }
}

/// <summary>
/// View model for a biometric image in the list
/// </summary>
public class ImageListItem
{
    public BiometricImage Image { get; }
    public int Index { get; }

    public ImageListItem(BiometricImage image, int index)
    {
        Image = image;
        Index = index;
    }

    public string Icon => Image.ImageType switch
    {
        BiometricImageType.Fingerprint => "🖐️",
        BiometricImageType.LatentFingerprint => "🔍",
        BiometricImageType.Face => "👤",
        BiometricImageType.SMT => "🏷️",
        BiometricImageType.PalmPrint => "🤚",
        BiometricImageType.Iris => "👁️",
        BiometricImageType.Footprint => "🦶",
        BiometricImageType.Signature => "📝",
        _ => "📦"
    };

    public string Title
    {
        get
        {
            var typeName = Image.ImageType switch
            {
                BiometricImageType.Fingerprint => "Fingerprint",
                BiometricImageType.LatentFingerprint => "Latent",
                BiometricImageType.Face => "Face",
                BiometricImageType.SMT => "SMT",
                BiometricImageType.PalmPrint => "Palm Print",
                BiometricImageType.Iris => "Iris",
                BiometricImageType.Footprint => "Footprint",
                BiometricImageType.Signature => "Signature",
                _ => "Other"
            };

            var detail = Image.FingerPositionDescription
                        ?? Image.FaceImageTypeDescription
                        ?? "";

            return string.IsNullOrEmpty(detail)
                ? $"#{Index} — {typeName}"
                : $"#{Index} — {detail}";
        }
    }

    public string Subtitle =>
        $"{Image.GetImageSizeDescription()} • {Image.GetDataSizeKB():F1} KB • {Image.CompressionDescription ?? "?"}";

    public string StatusText => Image.IsWebConvertible
        ? (Image.IsNativelyWebCompatible ? "✓ Web" : "~ Convertible")
        : "✗ WSQ";

    public SolidColorBrush StatusBackground => Image.IsWebConvertible
        ? (Image.IsNativelyWebCompatible
            ? new SolidColorBrush(Color.FromRgb(0xD4, 0xED, 0xDA))  // green
            : new SolidColorBrush(Color.FromRgb(0xD1, 0xEC, 0xF1))) // blue
        : new SolidColorBrush(Color.FromRgb(0xF8, 0xD7, 0xDA));     // red

    public SolidColorBrush StatusForeground => Image.IsWebConvertible
        ? (Image.IsNativelyWebCompatible
            ? new SolidColorBrush(Color.FromRgb(0x15, 0x57, 0x24))
            : new SolidColorBrush(Color.FromRgb(0x0C, 0x5D, 0x6F)))
        : new SolidColorBrush(Color.FromRgb(0x72, 0x1C, 0x24));
}
