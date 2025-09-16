# Coensio Swing Trader V06 Strategy
[English](README.md) | [Русский](README_ru.md)

该策略复现了原始 Coensio Swing Trader 的突破逻辑。通过 Donchian 通道确定动态支撑和阻力。当价格突破上轨或下轨超过设定阈值时开仓。

## 细节

- **入场**：
  - **多头**：收盘价突破 Donchian 上轨并超出 `Entry Threshold` 点。
  - **空头**：收盘价跌破 Donchian 下轨并超过 `Entry Threshold` 点。
- **出场**：
  - 固定的 `Stop Loss` 和 `Take Profit`，以入场价为基准的点数。
  - 在获得 `Break Even` 点利润后可移动止损至保本价。
  - 可选的追踪止损，在保本后按 `Trailing Step` 点跟随价格。
- **止损**：止损、止盈、保本、追踪止损。
- **默认值**：
  - `Channel Period` = 20
  - `Entry Threshold` = 15 点
  - `Stop Loss` = 50 点
  - `Take Profit` = 80 点
  - `Break Even` = 25 点
  - `Trailing Step` = 5 点
  - `Enable Trailing` = false
  - `Candle Type` = 15 分钟K线
