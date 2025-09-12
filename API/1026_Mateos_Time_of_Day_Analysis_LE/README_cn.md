# Mateo's Time of Day Analysis LE
[English](README.md) | [Русский](README_ru.md)

在设定的日内时间窗口内开多仓，并在当天稍后平仓。

该策略用于探索不同时间段的市场表现。

## 细节

- **入场条件**：时间达到 `StartTime` 且在 `From` 到 `Thru` 范围内。
- **多空方向**：仅做多。
- **出场条件**：时间达到 `EndTime`（20:00 前）。
- **止损**：无。
- **默认值**：
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `StartTime` = 09:30
  - `EndTime` = 16:00
  - `From` = 2017-04-21
  - `Thru` = 2099-12-01
- **筛选器**：
  - Category: Time-based
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
