# Smart Money Concept Strategy - Uncle Sam
[Русский](README_ru.md) | [中文](README_cn.md)

This breakout strategy monitors recent swing highs and lows. A long trade is opened when price closes above the latest pivot high, while a short trade is opened when price closes below the latest pivot low. An optional moving average filter can be enabled to trade only with the prevailing trend.

## Details

- **Entry Criteria**:
  - **Long**: Close crosses above the most recent pivot high (and above the MA if enabled).
  - **Short**: Close crosses below the most recent pivot low (and below the MA if enabled).
- **Long/Short**: Both.
- **Indicators**: Pivot detection, Moving Average (optional).
- **Timeframe**: Configurable.
- **Complexity**: Medium.
