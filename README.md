# UFO-TW CSV 转 Funscript / UFO-TW CSV to Funscript

## 中文说明

这是一个 Windows 工具，用于把原版 UFO-TW 的五列 CSV 转换为 MFP 可识别的两个 funscript 文件。

### 使用方法

1. 运行 `UfoTwCsvConverter.exe`。
2. 点击右上角语言下拉框，可切换 `中文` 和 `English`。
3. 点击“选择 CSV”，或直接把一个或多个 CSV 文件拖入窗口。
4. 点击“开始转换”。输出文件会写入 CSV 所在目录。

输出文件名：

```text
视频名.Lnip.funscript
视频名.Rnip.funscript
```

如果输入文件名以 `_ufotw` 或 `.ufotw` 结尾，程序默认会自动移除该后缀；取消勾选即可保留。

### 输入格式

```text
时间,左极性,左力度,右极性,右力度
```

时间单位为 100 毫秒。极性 `0` 表示正转，`1` 表示反转；力度范围为 `0`～`100`。位置映射如下：

- `0`：反转 100%
- `49`：反转 2%
- `50`：停止/中心
- `51`：正转 2%
- `100`：正转 100%

### 命令行

```text
UfoTwCsvConverter.exe --convert 文件.csv [--output 输出目录] [--keep-suffix] [--language en] [--quiet]
```

## English Guide

This Windows tool converts the original five-column UFO-TW CSV format into the two funscript files recognized by MFP.

### How to use

1. Run `UfoTwCsvConverter.exe`.
2. Use the language selector in the upper-right corner to switch between `中文` and `English`.
3. Click `Select CSV`, or drag one or more CSV files into the window.
4. Click `Convert`. The output files are written next to each CSV file.

Output file names:

```text
video-name.Lnip.funscript
video-name.Rnip.funscript
```

When an input file name ends with `_ufotw` or `.ufotw`, the suffix is removed by default. Clear the checkbox to keep it.

### Input format

```text
time,left polarity,left power,right polarity,right power
```

Time is measured in 100 ms units. Polarity `0` means forward and `1` means reverse; power ranges from `0` to `100`. The position mapping is:

- `0`: 100% reverse
- `49`: 2% reverse
- `50`: stop/center
- `51`: 2% forward
- `100`: 100% forward

### Command line

```text
UfoTwCsvConverter.exe --convert file.csv [--output directory] [--keep-suffix] [--language en] [--quiet]
```
