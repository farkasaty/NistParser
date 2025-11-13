using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NistParser.Core;
using NistParser.Editing;
using NistParser.WPF.ViewModels;

namespace NistParser.WPF.Windows;

/// <summary>
/// Window for editing NIST record fields
/// </summary>
public partial class RecordEditWindow : Window
{
    private readonly RecordEditorViewModel _viewModel;

    public RecordEditWindow(NistRecord record, NistTransactionEditor editor)
    {
        InitializeComponent();

        _viewModel = new RecordEditorViewModel(record, editor);
        DataContext = _viewModel;

        // Add converters to resources
        Resources.Add("TextBoxVisibilityConverter", new FieldTypeToTextBoxVisibilityConverter());
        Resources.Add("ComboBoxVisibilityConverter", new FieldTypeToComboBoxVisibilityConverter());
        Resources.Add("NullToVisibilityConverter", new NullToVisibilityConverter());
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var results = _viewModel.SaveChanges();
        if (results.IsValid)
        {
            MessageBox.Show("Changes saved successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show($"Validation errors:\n\n{results}", "Validation Failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Are you sure you want to reset all changes?",
            "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _viewModel.ResetChanges();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.IsDirty)
        {
            var result = MessageBox.Show("You have unsaved changes. Are you sure you want to cancel?",
                "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
                return;
        }

        DialogResult = false;
        Close();
    }

    private void AddField_Click(object sender, RoutedEventArgs e)
    {
        var result = _viewModel.AddNewField();
        if (!result.IsValid)
        {
            MessageBox.Show(result.ErrorMessage ?? "Failed to add field", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteField_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is EditableFieldViewModel fieldViewModel)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete field '{fieldViewModel.DisplayName}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var validationResult = _viewModel.RemoveField(fieldViewModel);
                if (!validationResult.IsValid)
                {
                    MessageBox.Show(validationResult.ErrorMessage ?? "Failed to delete field", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

/// <summary>
/// Converter to show TextBox for Text, Numeric, and Date field types
/// </summary>
public class FieldTypeToTextBoxVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length > 0 && values[0] is FieldDataType fieldType)
        {
            return fieldType == FieldDataType.Text ||
                   fieldType == FieldDataType.Numeric ||
                   fieldType == FieldDataType.Date
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter to show ComboBox for Enumeration field types
/// </summary>
public class FieldTypeToComboBoxVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length > 0 && values[0] is FieldDataType fieldType)
        {
            return fieldType == FieldDataType.Enumeration ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter to show elements only when value is not null or empty
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null && !string.IsNullOrWhiteSpace(value.ToString())
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
