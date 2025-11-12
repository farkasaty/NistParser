using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using NistParser.Core;
using NistParser.Records;
using NistParser.Exceptions;
using NistParser.Constants;

namespace NistParser.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private NistTransaction? _currentTransaction;

    public MainWindow()
    {
        InitializeComponent();
        ShowEmptyState();
    }

    private void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Open NIST File",
            Filter = "NIST Files (*.nist;*.an2)|*.nist;*.an2|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            LoadNistFile(openFileDialog.FileName);
        }
    }

    private void LoadNistFile(string filePath)
    {
        try
        {
            // Show loading indicator
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            // Parse the NIST file
            _currentTransaction = NistTransactionParser.ParseFile(filePath);

            // Update UI
            FilePathText.Text = filePath;
            DisplayTransaction(_currentTransaction);

            // Hide empty state, show content
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            SummaryPanel.Visibility = Visibility.Visible;
        }
        catch (MissingType1Exception ex)
        {
            ShowError("Invalid NIST File", $"The file is missing a required Type-1 record:\n\n{ex.Message}");
        }
        catch (TruncatedFileException ex)
        {
            ShowError("Corrupted File", $"The file appears to be truncated or corrupted:\n\n{ex.Message}");
        }
        catch (NistParserException ex)
        {
            ShowError("Parse Error", $"Failed to parse NIST file:\n\n{ex.Message}");
        }
        catch (Exception ex)
        {
            ShowError("Error", $"An unexpected error occurred:\n\n{ex.Message}");
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void DisplayTransaction(NistTransaction transaction)
    {
        // Display transaction summary
        VersionText.Text = transaction.Version ?? "N/A";
        DateText.Text = transaction.Header.Date?.ToString("yyyy-MM-dd") ?? transaction.Header.DateString ?? "N/A";
        TcnText.Text = transaction.TransactionControlNumber ?? "N/A";
        TransactionTypeText.Text = transaction.Header.TypeOfTransaction ?? "N/A";
        OriText.Text = transaction.OriginatingAgencyIdentifier ?? "N/A";
        DaiText.Text = transaction.Header.DestinationAgencyIdentifier ?? "N/A";
        RecordCountText.Text = transaction.TotalRecordCount.ToString();
        ResolutionText.Text = transaction.Header.NativeScanningResolution ?? "N/A";

        // Populate records list
        var recordViewModels = new List<RecordViewModel>();

        // Add Type-1 header
        recordViewModels.Add(new RecordViewModel
        {
            Record = transaction.Header,
            DisplayName = "Type-1: Transaction Information",
            Details = $"IDC: {transaction.Header.IDC}, {transaction.Header.Fields.Count} fields"
        });

        // Add all other records
        foreach (var record in transaction.Records)
        {
            string recordTypeName = GetRecordTypeName(record);
            string details = $"IDC: {record.IDC}, {record.Fields.Count} fields";

            // Add specific details for known record types
            if (record is Type2Record type2)
            {
                if (!string.IsNullOrEmpty(type2.FullName))
                    details = $"{type2.FullName}, IDC: {type2.IDC}";
            }
            else if (record is Type14Record type14)
            {
                details = $"Finger {type14.FingerPosition}, {type14.Width}x{type14.Height}, IDC: {type14.IDC}";
            }

            recordViewModels.Add(new RecordViewModel
            {
                Record = record,
                DisplayName = $"Type-{(int)record.RecordType}: {recordTypeName}",
                Details = details
            });
        }

        RecordsListBox.ItemsSource = recordViewModels;

        // Select first record
        if (recordViewModels.Count > 0)
        {
            RecordsListBox.SelectedIndex = 0;
        }
    }

    private void RecordsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecordsListBox.SelectedItem is RecordViewModel viewModel)
        {
            DisplayRecordDetails(viewModel.Record);
        }
    }

    private void DisplayRecordDetails(NistRecord record)
    {
        // Show record header panel
        NoSelectionText.Visibility = Visibility.Collapsed;
        RecordHeaderPanel.Visibility = Visibility.Visible;

        // Display record metadata
        RecordTypeText.Text = $"Type-{(int)record.RecordType} ({GetRecordTypeName(record)})";
        RecordIdcText.Text = record.IDC;
        RecordLengthText.Text = $"{record.RecordLength} bytes";

        // Display fields
        var fieldViewModels = new List<FieldViewModel>();

        foreach (var kvp in record.Fields.OrderBy(f => f.Key))
        {
            var field = kvp.Value;
            string displayValue;

            if (field.IsBinary)
            {
                int dataSize = field.BinaryData?.Length ?? 0;
                displayValue = $"[Binary Data: {dataSize:N0} bytes]";

                // For image fields, show additional info if available
                if (field.FieldNumber.EndsWith(".999"))
                {
                    if (record is Type14Record type14 && type14.Compression != null)
                    {
                        displayValue += $"\nCompression: {type14.Compression.Value.GetDisplayName()}";
                        displayValue += $"\nDimensions: {type14.Width}x{type14.Height}";
                    }
                }
            }
            else
            {
                // Display subfields and items
                if (field.Subfields.Count == 1 && field.Subfields[0].Items.Count == 1)
                {
                    // Simple field with one value
                    displayValue = field.FirstValue ?? "";
                }
                else
                {
                    // Complex field with multiple subfields/items
                    var subfieldStrings = new List<string>();
                    for (int i = 0; i < field.Subfields.Count; i++)
                    {
                        var subfield = field.Subfields[i];
                        if (field.Subfields.Count > 1)
                        {
                            subfieldStrings.Add($"Subfield {i + 1}: {string.Join(" | ", subfield.Items)}");
                        }
                        else
                        {
                            subfieldStrings.Add(string.Join(" | ", subfield.Items));
                        }
                    }
                    displayValue = string.Join("\n", subfieldStrings);
                }
            }

            fieldViewModels.Add(new FieldViewModel
            {
                FieldNumber = field.FieldNumber,
                DisplayValue = displayValue
            });
        }

        FieldsItemsControl.ItemsSource = fieldViewModels;
    }

    private string GetRecordTypeName(NistRecord record)
    {
        return record.RecordType.ToString().Replace("_", " ").Replace("Type", "").Trim();
    }

    private void ShowEmptyState()
    {
        EmptyStatePanel.Visibility = Visibility.Visible;
        SummaryPanel.Visibility = Visibility.Collapsed;
        NoSelectionText.Visibility = Visibility.Visible;
        RecordHeaderPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowError(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

/// <summary>
/// View model for displaying a record in the list
/// </summary>
public class RecordViewModel
{
    public NistRecord Record { get; set; } = null!;
    public string DisplayName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// View model for displaying a field
/// </summary>
public class FieldViewModel
{
    public string FieldNumber { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
}
