using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UfoTwCsvConverter;

public sealed record UfoCsvConversionResult(
    string SourcePath,
    string LeftPath,
    string RightPath,
    int RowsRead,
    int LeftActions,
    int RightActions);

public static class UfoCsvConverter
{
    private sealed record ParsedRow(int At, int LeftPosition, int RightPosition, int Order);

    private sealed class FunscriptDocument
    {
        [JsonPropertyName("version")]
        public string Version { get; init; } = "1.0";

        [JsonPropertyName("inverted")]
        public bool Inverted { get; init; }

        [JsonPropertyName("range")]
        public int Range { get; init; } = 100;

        [JsonPropertyName("actions")]
        public IReadOnlyList<FunscriptAction> Actions { get; init; } = [];
    }

    private sealed record FunscriptAction(
        [property: JsonPropertyName("pos")] int Position,
        [property: JsonPropertyName("at")] int At);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static UfoCsvConversionResult ConvertFile(
        string csvPath,
        string? outputDirectory = null,
        bool stripUfoTwSuffix = true)
    {
        if (string.IsNullOrWhiteSpace(csvPath))
            throw new ArgumentException("没有指定 CSV 文件。", nameof(csvPath));

        var sourcePath = Path.GetFullPath(csvPath);
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("找不到指定的 CSV 文件。", sourcePath);

        var rows = ReadRows(sourcePath);
        if (rows.Count == 0)
            throw new InvalidDataException("CSV 中没有可转换的数据行。格式应为：时间,左极性,左力度,右极性,右力度");

        var leftActions = BuildActions(rows.Select(row => (row.At, row.LeftPosition, row.Order)));
        var rightActions = BuildActions(rows.Select(row => (row.At, row.RightPosition, row.Order)));

        var directory = string.IsNullOrWhiteSpace(outputDirectory)
            ? Path.GetDirectoryName(sourcePath)!
            : Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(directory);

        var stem = Path.GetFileNameWithoutExtension(sourcePath);
        if (stripUfoTwSuffix)
            stem = RemoveUfoTwSuffix(stem);
        if (string.IsNullOrWhiteSpace(stem))
            stem = "converted";

        var leftPath = Path.Combine(directory, $"{stem}.Lnip.funscript");
        var rightPath = Path.Combine(directory, $"{stem}.Rnip.funscript");
        WriteFunscript(leftPath, leftActions);
        WriteFunscript(rightPath, rightActions);

        return new UfoCsvConversionResult(
            sourcePath,
            leftPath,
            rightPath,
            rows.Count,
            leftActions.Count,
            rightActions.Count);
    }

    private static List<ParsedRow> ReadRows(string sourcePath)
    {
        var rows = new List<ParsedRow>();
        var lines = File.ReadAllLines(sourcePath);

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var lineNumber = lineIndex + 1;
            var line = lines[lineIndex].Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var fields = ParseCsvLine(line);
            if (fields.Count == 0)
                continue;

            if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestampUnits))
            {
                if (rows.Count == 0 && IsHeader(fields[0]))
                    continue;

                throw new InvalidDataException($"第 {lineNumber} 行的时间不是整数：{fields[0]}");
            }

            if (fields.Count != 5)
                throw new InvalidDataException($"第 {lineNumber} 行有 {fields.Count} 列，应为 5 列：时间,左极性,左力度,右极性,右力度");

            var leftPolarity = ParseBoundedInt(fields[1], 0, 1, lineNumber, "左极性");
            var leftPower = ParseBoundedInt(fields[2], 0, 100, lineNumber, "左力度");
            var rightPolarity = ParseBoundedInt(fields[3], 0, 1, lineNumber, "右极性");
            var rightPower = ParseBoundedInt(fields[4], 0, 100, lineNumber, "右力度");

            int at;
            try
            {
                at = checked(timestampUnits * 100);
            }
            catch (OverflowException)
            {
                throw new InvalidDataException($"第 {lineNumber} 行的时间太大，无法转换为毫秒。");
            }

            rows.Add(new ParsedRow(
                at,
                ToFunscriptPosition(leftPolarity, leftPower),
                ToFunscriptPosition(rightPolarity, rightPower),
                lineIndex));
        }

        return rows;
    }

    private static int ParseBoundedInt(string value, int min, int max, int lineNumber, string fieldName)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            || result < min
            || result > max)
        {
            throw new InvalidDataException($"第 {lineNumber} 行的{fieldName}无效：{value}，应为 {min}～{max}。");
        }

        return result;
    }

    private static int ToFunscriptPosition(int polarity, int power)
    {
        // UFO-TW CSV: polarity 0 = forward, polarity 1 = reverse.
        // Funscript: 50 = stop, 0 = reverse 100%, 100 = forward 100%.
        var signedPower = polarity == 1 ? -power : power;
        return 50 + (int)Math.Round(signedPower / 2.0, MidpointRounding.AwayFromZero);
    }

    private static List<FunscriptAction> BuildActions(IEnumerable<(int At, int Position, int Order)> source)
    {
        // A few CSV files contain multiple rows at one timestamp. Keep the last
        // row at that timestamp, then remove consecutive duplicate positions.
        var normalized = source
            .GroupBy(item => item.At)
            .Select(group => group.OrderBy(item => item.Order).Last())
            .OrderBy(item => item.At)
            .ThenBy(item => item.Order);

        var actions = new List<FunscriptAction>();
        int? previousPosition = null;
        foreach (var item in normalized)
        {
            if (previousPosition == item.Position)
                continue;

            actions.Add(new FunscriptAction(item.Position, item.At));
            previousPosition = item.Position;
        }

        return actions;
    }

    private static void WriteFunscript(string path, IReadOnlyList<FunscriptAction> actions)
    {
        var document = new FunscriptDocument { Actions = actions };
        var json = JsonSerializer.Serialize(document, JsonOptions) + Environment.NewLine;
        File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string RemoveUfoTwSuffix(string stem)
    {
        foreach (var suffix in new[] { "_ufotw", ".ufotw" })
        {
            if (stem.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return stem[..^suffix.Length];
        }

        return stem;
    }

    private static bool IsHeader(string field)
        => field.Contains("time", StringComparison.OrdinalIgnoreCase)
        || field.Contains("时间", StringComparison.OrdinalIgnoreCase);

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (character == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (character == ',' && !inQuotes)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        if (inQuotes)
            throw new InvalidDataException("CSV 存在未闭合的引号。");

        fields.Add(current.ToString().Trim());
        return fields;
    }
}
