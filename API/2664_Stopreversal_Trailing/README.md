# Stopreversal Trailing Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Stopreversal Trailing Strategy reproduces the MT5 expert `Exp_Stopreversal.mq5`. It uses the Stopreversal custom indicator to build a dynamic trailing stop line around the selected candle price. When the price pierces this trailing line upward the strategy treats it as a bullish reversal, optionally closes short exposure, and opens a new long position. A downside pierce produces the symmetric bearish action. Signals can be delayed by a configurable number of closed bars to match the original expert advisor behaviour.

## Details

- **Entry Logic**: reacts to Stopreversal indicator arrows produced when price crosses the adaptive trailing stop.
- **Long/Short**: both directions are supported with independent toggles for enabling long or short entries.
- **Exit Logic**: opposite Stopreversal signals can close existing positions; protective stop-loss and take-profit levels are also available.
- **Stops**: static stop-loss and take-profit in price steps plus the indicator-driven reversals.
- **Data Source**: any timeframe; default uses 4-hour time frame candles, mirroring the original expert's multi-timeframe call.
- **Signal Delay**: `SignalBar` parameter delays order execution by the specified number of completed bars (default 1 bar).
- **Risk Management**: optional hard stops expressed in security price steps; position protection service is activated at start.
- **Indicator Parameters**: trailing offset `Npips` controls the distance between price and stop; `PriceMode` selects the candle price used by the trailing stop.
- **Default Values**:
  - `Volume` = 1
  - `StopLossSteps` = 1000
  - `TakeProfitSteps` = 2000
  - `BuyPositionOpen` = true
  - `SellPositionOpen` = true
  - `BuyPositionClose` = true
  - `SellPositionClose` = true
  - `Npips` = 0.004
  - `PriceMode` = Close
  - `SignalBar` = 1

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Candle subscription used for both Stopreversal calculations and trading. Default is a 4-hour timeframe. |
| `Volume` | Base order size sent when entering a new position. |
| `StopLossSteps` | Distance from entry to stop-loss in price steps; set to 0 to disable. |
| `TakeProfitSteps` | Distance from entry to take-profit in price steps; set to 0 to disable. |
| `BuyPositionOpen` | Enables opening long positions when a bullish signal occurs. |
| `SellPositionOpen` | Enables opening short positions when a bearish signal occurs. |
| `BuyPositionClose` | Closes any existing long positions when a bearish signal is received. |
| `SellPositionClose` | Closes any existing short positions when a bullish signal is received. |
| `Npips` | Fractional multiplier applied to the trailing stop to widen or tighten the reversal distance. |
| `PriceMode` | Applied price variant (close, open, high, low, median, typical, weighted, simple average, quarter average, trend-follow, or Demark). |
| `SignalBar` | Number of fully closed candles to wait before reacting to a signal, matching the MT5 parameter. |

## Filters

- **Category**: Trend-following reversal
- **Direction**: Bi-directional
- **Indicators**: Stopreversal (ATR-backed trailing stop)
- **Stops**: Static stop-loss and take-profit, optional
- **Timeframe**: Configurable (default H4)
- **Seasonality**: None
- **Neural Networks**: No
- **Divergence**: No
- **Complexity**: Medium due to custom trailing logic
- **Risk Level**: Adjustable through stop distance and trailing offset
