# Berlin Candles 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用基于平滑 Heikin Ashi 值的 Berlin 自定义蜡烛。当看涨的 Berlin 蜡烛收盘价高于 Donchian 基线时做多；当看跌的 Berlin 蜡烛收盘价低于基线时做空。

## 细节

- **入场条件**：
  - **多头**：Berlin 收盘价 > Berlin 开盘价 且 Berlin 收盘价 > baseline。
  - **空头**：Berlin 收盘价 < Berlin 开盘价 且 Berlin 收盘价 < baseline。
- **方向**：双向
- **止损**：默认无
- **默认参数**：
  - `Smoothing` = 1
  - `BaselinePeriod` = 26
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
