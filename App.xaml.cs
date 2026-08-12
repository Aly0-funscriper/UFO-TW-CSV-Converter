using System.IO;
using System.Windows;

namespace UfoTwCsvConverter;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Length > 0 && string.Equals(e.Args[0], "--convert", StringComparison.OrdinalIgnoreCase))
        {
            RunCommandLine(e.Args.Skip(1).ToArray());
            return;
        }

        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    private static void RunCommandLine(IReadOnlyList<string> args)
    {
        var files = new List<string>();
        string? outputDirectory = null;
        var stripUfoTwSuffix = true;
        var quiet = false;
        var english = false;

        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--output" when i + 1 < args.Count:
                    outputDirectory = args[++i];
                    break;
                case "--keep-suffix":
                    stripUfoTwSuffix = false;
                    break;
                case "--quiet":
                    quiet = true;
                    break;
                case "--language" when i + 1 < args.Count:
                    english = string.Equals(args[++i], "en", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(args[i], "english", StringComparison.OrdinalIgnoreCase);
                    break;
                default:
                    files.Add(args[i]);
                    break;
            }
        }

        if (files.Count == 0)
        {
            MessageBox.Show(
                english
                    ? "Usage: UfoTwCsvConverter.exe --convert file.csv [--output directory] [--keep-suffix] [--language en]"
                    : "命令行用法：UfoTwCsvConverter.exe --convert 文件.csv [--output 输出目录] [--keep-suffix] [--language en]",
                english ? "UFO-TW CSV Converter" : "UFO-TW CSV 转换器",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Current.Shutdown(2);
            return;
        }

        try
        {
            var results = files.Select(path => UfoCsvConverter.ConvertFile(path, outputDirectory, stripUfoTwSuffix)).ToList();
            var message = string.Join(Environment.NewLine, results.Select(result =>
                english
                    ? $"{Path.GetFileName(result.SourcePath)}: created {Path.GetFileName(result.LeftPath)} and {Path.GetFileName(result.RightPath)}"
                    : $"{Path.GetFileName(result.SourcePath)}：生成 {Path.GetFileName(result.LeftPath)} 和 {Path.GetFileName(result.RightPath)}"));
            if (!quiet)
                MessageBox.Show(message, english ? "Conversion complete" : "转换完成", MessageBoxButton.OK, MessageBoxImage.Information);
            Current.Shutdown(0);
        }
        catch (Exception exception)
        {
            if (!quiet)
                MessageBox.Show(exception.Message, english ? "Conversion failed" : "转换失败", MessageBoxButton.OK, MessageBoxImage.Error);
            Current.Shutdown(1);
        }
    }
}
