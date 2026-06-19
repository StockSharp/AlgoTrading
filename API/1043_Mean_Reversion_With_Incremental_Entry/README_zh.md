# 均值回归分批入场策略
[English](README.md) | [Русский](README_ru.md)

当价格偏离简单移动平均线一定百分比时入场，并在价格继续偏离时逐步加仓。

当价格回到移动平均线时平仓。

## 细节

- **入场条件：**
  - **多头：** `Low < SMA` 且 `Low` 与 `SMA` 的百分比差 ≥ `Initial Percent`。
  - **空头：** `High > SMA` 且 `High` 与 `SMA` 的百分比差 ≥ `Initial Percent`。
- **加仓：** 每当价格与前一次入场价差距达到 `Percent Step` 时加单。
- **离场条件：**
  - **多头：** `Close ≥ SMA`。
  - **空头：** `Close ≤ SMA`。
- **指标：** SMA。
- **默认值：**
  - `MA Length` = 30
  - `Initial Percent` = 5
  - `Percent Step` = 1
