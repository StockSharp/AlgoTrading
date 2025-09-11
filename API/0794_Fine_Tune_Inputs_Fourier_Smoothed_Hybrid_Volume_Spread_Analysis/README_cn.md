# Fine-Tune Inputs Fourier Smoothed Hybrid Volume Spread Analysis
[English](README.md) | [Русский](README_ru.md)

该策略结合平滑成交量与开盘价和收盘价的EMA来分析成交量价差。当价差和其移动平均都为正时做多，当两者都为负时做空。可选参数允许在没有信号时平仓。

## 细节

- **入场条件**:
  - **多头**: `vd > 0` 且 `vdma > 0`
  - **空头**: `vd < 0` 且 `vdma < 0`
- **出场条件**: 在信号中性时可选择平仓。
- **类型**: 趋势跟随
- **指标**: EMA
- **时间框架**: 1 分钟（默认）
