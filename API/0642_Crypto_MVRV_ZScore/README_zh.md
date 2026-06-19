# Crypto MVRV ZScore 策略
[English](README.md) | [Русский](README_ru.md)

该策略利用 MVRV Z-Score 概念来检测市场价值与实现价值之间的极端偏离。
当价差 z-score 穿越设定阈值时开仓，并在反向穿越时平仓。

## 细节

- **入场条件**：
  - 当价差 z-score 上穿 `LongEntryThreshold` 时做多。
  - 当价差 z-score 下穿 `ShortEntryThreshold` 时做空。
- **多空方向**：可配置 (`TradeDirection`)。
- **出场条件**：
  - 穿越相反阈值。
- **止损**：无。
- **默认值**：
  - `ZScoreCalculationPeriod` = 252
  - `LongEntryThreshold` = 0.382
  - `ShortEntryThreshold` = -0.382
  - `CandleType` = TimeSpan.FromDays(1)
- **过滤器**：
  - 类别：均值回归
  - 方向：双向
  - 指标：SMA、StandardDeviation、Z-Score
  - 止损：无
  - 复杂度：中等
  - 时间框架：日线
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
