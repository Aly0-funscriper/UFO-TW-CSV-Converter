# UFO-TW CSV 转 Funscript / UFO-TW CSV to Funscript

## 中文说明

这是一个 Windows 工具，用于把 UFO-TW 的三列或五列 CSV 转换为 MFP 可识别的 `Lnip/Rnip` funscript 文件。

### 使用方法

1. 运行 `UfoTwCsvConverter.exe`。
2. 点击右上角语言下拉框，可切换 `中文` 和 `English`。
3. 点击“选择 CSV”，或直接把一个或多个 CSV 文件拖入窗口。
4. 点击“开始转换”。输出文件会写入 CSV 所在目录。

五列 CSV（左右独立）会生成：

```text
视频名.Lnip.funscript
视频名.Rnip.funscript
```

三列 CSV 格式为 `时间,极性,力度`，通常表示一个旋转通道。程序默认根据文件名判断目标：文件名包含“左”或 `left/Lnip` 时只生成左侧，包含“右”或 `right/Rnip` 时只生成右侧，其他情况同时生成左右两侧。也可以在界面中手动选择“左右两侧”“仅左侧”或“仅右侧”。

如果输入文件名以 `_ufotw` 或 `.ufotw` 结尾，程序默认会自动移除该后缀；取消勾选即可保留。

### 输入格式

五列格式：

```text
时间,左极性,左力度,右极性,右力度
```

三列格式：

```text
时间,极性,力度
```

时间单位为 100 毫秒。极性 `0` 表示正转，`1` 表示反转；力度范围为 `0`～`100`。位置映射如下：

- `0`：反转 100%
- `49`：反转 2%
- `50`：停止/中心
- `51`：正转 2%
- `100`：正转 100%

### 命令行

```text
UfoTwCsvConverter.exe --convert 文件.csv [--output 输出目录] [--keep-suffix] [--axis auto|both|left|right] [--language en] [--quiet]
```

## English Guide

This Windows tool converts three-column or five-column UFO-TW CSV files into the `Lnip`/`Rnip` funscript files recognized by MFP.

### How to use

1. Run `UfoTwCsvConverter.exe`.
2. Use the language selector in the upper-right corner to switch between `中文` and `English`.
3. Click `Select CSV`, or drag one or more CSV files into the window.
4. Click `Convert`. The output files are written next to each CSV file.

Five-column input creates both independent outputs:

```text
video-name.Lnip.funscript
video-name.Rnip.funscript
```

Three-column input has the format `time,polarity,power` and normally represents one rotation channel. In `Auto-detect` mode, a file name containing left/`Lnip` creates only the left output, a file name containing right/`Rnip` creates only the right output, and other names create both outputs. The GUI also provides explicit Both/Left only/Right only choices.

When an input file name ends with `_ufotw` or `.ufotw`, the suffix is removed by default. Clear the checkbox to keep it.

### Input format

Five-column format:

```text
time,left polarity,left power,right polarity,right power
```

Three-column format:

```text
time,polarity,power
```

Time is measured in 100 ms units. Polarity `0` means forward and `1` means reverse; power ranges from `0` to `100`. The position mapping is:

- `0`: 100% reverse
- `49`: 2% reverse
- `50`: stop/center
- `51`: 2% forward
- `100`: 100% forward

### Command line

```text
UfoTwCsvConverter.exe --convert file.csv [--output directory] [--keep-suffix] [--axis auto|both|left|right] [--language en] [--quiet]
```
