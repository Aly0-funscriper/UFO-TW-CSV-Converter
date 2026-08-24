using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UfoTwCsvConverter;

public partial class MainWindow : Window
{
    private enum StatusKind
    {
        Waiting,
        Added,
        Completed,
        Failed,
    }

    private readonly ObservableCollection<string> _files = [];
    private StatusKind _statusKind = StatusKind.Waiting;
    private int _statusFileCount;
    private int _statusRowCount;

    public MainWindow()
    {
        InitializeComponent();
        FileList.ItemsSource = _files;
        ApplyLanguage();
        UpdateButtons();
    }

    private bool IsEnglish => LanguageSelector.SelectedIndex == 1;

    private void LanguageSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StatusText != null)
            ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        var english = IsEnglish;
        Title = english ? "UFO-TW CSV to Funscript" : "UFO-TW CSV 转 Funscript";
        TitleText.Text = Title;
        InfoText.Text = english
            ? "Supports UFO-TW three-column CSV (time, polarity, power) and five-column CSV (time, left polarity, left power, right polarity, right power). Time units are 100 ms; polarity 0 is forward and 1 is reverse. Output mapping: 0–49 reverse, 50 stop, 51–100 forward."
            : "支持 UFO-TW 的三列 CSV（时间,极性,力度）和五列 CSV（时间,左极性,左力度,右极性,右力度）。时间单位为 100 毫秒；极性 0 为正转，1 为反转。输出映射：0～49 反转，50 停止，51～100 正转。";
        SelectButton.Content = english ? "Select CSV" : "选择 CSV";
        ClearButton.Content = english ? "Clear list" : "清空列表";
        DragHintText.Text = english ? "You can also drag CSV files into this window" : "也可以把 CSV 拖到窗口中";
        StripSuffixCheckBox.Content = english
            ? "Automatically remove _ufotw or .ufotw from the input file name"
            : "如果文件名以 _ufotw 或 .ufotw 结尾，自动去掉该后缀";
        AxisTargetLabel.Text = english ? "Three-column target:" : "三列目标：";
        AxisAutoItem.Content = english ? "Auto-detect" : "自动判断";
        AxisBothItem.Content = english ? "Both sides" : "左右两侧";
        AxisLeftItem.Content = english ? "Left only" : "仅左侧";
        AxisRightItem.Content = english ? "Right only" : "仅右侧";
        OutputHintText.Text = english
            ? "Five-column CSV creates both files. Three-column CSV follows the file name or the target option above to create left, right, or both files."
            : "五列 CSV 会生成左右两个文件；三列 CSV 会根据文件名或上面的选项生成左侧、右侧或左右两个文件。";
        ConvertButton.Content = english ? "Convert" : "开始转换";
        UpdateStatusText();
    }

    private void SelectButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
            Multiselect = true,
            Title = "选择 UFO-TW CSV 文件",
        };

        if (dialog.ShowDialog(this) == true)
            AddFiles(dialog.FileNames);
    }

    private void ClearButton_OnClick(object sender, RoutedEventArgs e)
    {
        _files.Clear();
        SetStatus(StatusKind.Waiting);
        UpdateButtons();
    }

    private void ConvertButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_files.Count == 0)
            return;

        try
        {
            var stripSuffix = StripSuffixCheckBox.IsChecked == true;
            var axisTarget = (UfoCsvAxisTarget)AxisTargetSelector.SelectedIndex;
            var results = _files
                .Select(path => UfoCsvConverter.ConvertFile(path, stripUfoTwSuffix: stripSuffix, axisTarget: axisTarget))
                .ToList();

            var totalRows = results.Sum(result => result.RowsRead);
            SetStatus(StatusKind.Completed, results.Count, totalRows);
            MessageBox.Show(
                string.Join(Environment.NewLine, results.Select(result =>
                    $"{Path.GetFileName(result.SourcePath)}\n{FormatOutputPath(result.LeftPath)}{FormatOutputPath(result.RightPath)}")),
                IsEnglish ? "Conversion complete" : "转换完成",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            SetStatus(StatusKind.Failed);
            MessageBox.Show(exception.Message, IsEnglish ? "Conversion failed" : "转换失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Window_OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = HasCsvFiles(e.Data) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_OnDrop(object sender, DragEventArgs e)
    {
        if (HasCsvFiles(e.Data))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddFiles(files);
        }

        e.Handled = true;
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths.Where(path =>
                     File.Exists(path)
                     && string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase)))
        {
            if (!_files.Contains(path, StringComparer.OrdinalIgnoreCase))
                _files.Add(path);
        }

        SetStatus(_files.Count == 0 ? StatusKind.Waiting : StatusKind.Added, _files.Count);
        UpdateButtons();
    }

    private void SetStatus(StatusKind kind, int fileCount = 0, int rowCount = 0)
    {
        _statusKind = kind;
        _statusFileCount = fileCount;
        _statusRowCount = rowCount;
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (StatusText == null)
            return;

        StatusText.Text = _statusKind switch
        {
            StatusKind.Added when IsEnglish => $"{_statusFileCount} CSV file(s) selected.",
            StatusKind.Added => $"已添加 {_statusFileCount} 个 CSV 文件。",
            StatusKind.Completed when IsEnglish => $"Completed {_statusFileCount} file(s), converted {_statusRowCount} row(s).",
            StatusKind.Completed => $"已完成 {_statusFileCount} 个文件，共转换 {_statusRowCount} 行。",
            StatusKind.Failed when IsEnglish => "Conversion failed.",
            StatusKind.Failed => "转换失败。",
            _ when IsEnglish => "Waiting for CSV files.",
            _ => "等待选择 CSV 文件。",
        };
    }

    private void UpdateButtons()
    {
        ClearButton.IsEnabled = _files.Count > 0;
        ConvertButton.IsEnabled = _files.Count > 0;
    }

    private static string FormatOutputPath(string? path)
        => path == null ? string.Empty : $"  {Path.GetFileName(path)}{Environment.NewLine}";

    private static bool HasCsvFiles(IDataObject data)
        => data.GetDataPresent(DataFormats.FileDrop)
        && ((string[])data.GetData(DataFormats.FileDrop)!).Any(path =>
            string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase));
}
