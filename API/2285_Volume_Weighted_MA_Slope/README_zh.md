# VWMA 斜率
[English](README.md) | [Русский](README_ru.md)

**VWMA 斜率** 策略通过观察成交量加权移动平均线（VWMA）的方向来交易。当 VWMA 连续两个柱向上时做多，连续两个柱向下时做空；当斜率反转时，平掉现有仓位。

该方法利用考虑成交量的平均价格来识别趋势，避免在低量波动中产生信号。

## 细节

- **入场条件**：VWMA 连续两根柱上升（做多）或下降（做空）。
- **多空方向**：双向。
- **出场条件**：VWMA 斜率反转。
- **止损**：支持（默认止损 1%，止盈 2%）。
- **默认值**：
  - `VwmaPeriod` = 12
  - `CandleType` = TimeSpan.FromHours(4)
- **筛选**：
  - 类别：Trend
  - 方向：Both
  - 指标：VWMA
  - 止损：Yes
  - 复杂度：Basic
  - 时间框架：Swing
  - 季节性：No
  - 神经网络：No
  - 背离：No
  - 风险等级：Medium
