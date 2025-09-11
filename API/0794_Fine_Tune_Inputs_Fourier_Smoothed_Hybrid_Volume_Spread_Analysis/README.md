# Fine-Tune Inputs Fourier Smoothed Hybrid Volume Spread Analysis
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines smoothed volume with EMA of open and close prices to analyze volume spread. It enters long when both the volume spread and its moving average are positive, and short when both are negative. An optional parameter allows closing positions when no signal is present.

## Details

- **Entry Conditions**:
  - **Long**: `vd > 0` and `vdma > 0`
  - **Short**: `vd < 0` and `vdma < 0`
- **Exit Conditions**: Optionally close position when signals are neutral.
- **Type**: Trend-following
- **Indicators**: EMA
- **Timeframe**: 1 minute (default)
