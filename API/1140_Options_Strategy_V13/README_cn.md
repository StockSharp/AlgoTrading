# Options Strategy V1.3
[English](README.md) | [Русский](README_ru.md)

该策略利用 EMA 金叉/死叉配合 RSI 与基于 ATR 的止损和止盈，并加入成交量均线过滤。可选地要求突破开盘区间，并在纽约时间 15:55 自动平仓。策略在特定时段和用户自定义的禁止交易区间内不会开仓。

## 详情

- **入场条件**：
  - **多头**：短期 EMA 上穿长期 EMA，RSI ≥ `RsiLongThreshold`，成交量 ≥ 成交量均线，可选收盘价 > 开盘区间高点。
  - **空头**：短期 EMA 下穿长期 EMA，RSI ≤ `RsiShortThreshold`，成交量 ≥ 成交量均线，可选收盘价 < 开盘区间低点。
- **方向**：可做多或做空。
- **出场条件**：
  - 基于 ATR 的止损与止盈。
  - 反向 EMA 交叉。
  - 纽约时间 15:55 自动平仓。
- **止损**：有。
- **默认值**：
  - `EmaShortLength = 8`
  - `EmaLongLength = 28`
  - `RsiLength = 12`
  - `AtrLength = 14`
  - `SlMultiplier = 1.4`
  - `TpSlRatio = 4`
  - `VolumeMaLength = 20`
- **过滤**：
  - 类别：趋势跟随
  - 方向：可配置
  - 指标：EMA、RSI、ATR、SMA
  - 止损：是
  - 周期：日内
