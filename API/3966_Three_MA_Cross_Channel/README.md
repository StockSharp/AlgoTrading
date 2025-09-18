# Three MA Cross Channel Strategy

## Overview
The **Three MA Cross Channel Strategy** converts the MetaTrader Expert Advisor `3MaCross_EA` into the StockSharp high-level API. It monitors three configurable moving averages and opens trades when the faster averages cross the slower one. A Donchian price channel is optionally used to manage exits, closely mimicking the original EA that referenced the "Price Channel" indicator.

## Trading Logic
- **Long entry**: Generated when both the fast and medium moving averages close above the slow moving average and either of the two faster averages crosses above the slow one on the current bar.
- **Short entry**: Triggered when both the fast and medium moving averages close below the slow moving average and either of the two faster averages crosses below the slow one.
- **Position exit**:
  - Opposite crossover signal.
  - Optional Donchian channel stop: long positions close if price falls below the lower band; short positions close if price rises above the upper band.
  - Optional fixed take-profit or stop-loss distances measured in absolute price units.

The strategy always waits for completed candles, matching the `TradeAtCloseBar` behaviour of the original script. Only one directional position is maintained at a time; when a signal appears against an existing position, the current trade is closed before a new one is opened.

## Parameters
| Name | Type | Default | Description |
|------|------|---------|-------------|
| `FastLength` | `int` | `2` | Lookback for the fast moving average. |
| `MediumLength` | `int` | `4` | Lookback for the medium moving average. |
| `SlowLength` | `int` | `30` | Lookback for the slow moving average. |
| `ChannelLength` | `int` | `15` | Donchian channel window used for channel-based exits. |
| `FastType` | `MovingAverageTypeEnum` | `EMA` | Moving-average algorithm applied to the fast average (SMA, EMA, SMMA, WMA). |
| `MediumType` | `MovingAverageTypeEnum` | `EMA` | Moving-average algorithm applied to the medium average. |
| `SlowType` | `MovingAverageTypeEnum` | `EMA` | Moving-average algorithm applied to the slow average. |
| `TakeProfit` | `decimal` | `0` | Profit target in absolute price units. Set to `0` to disable. |
| `StopLoss` | `decimal` | `0` | Loss limit in absolute price units. Set to `0` to disable. |
| `UseChannelStop` | `bool` | `true` | Enables Donchian channel exits. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Candle type used for calculations. |

## Notes
- All moving averages use closing prices and can be configured individually to match the original EA's `FasterMode`, `MediumMode`, and `SlowerMode` options.
- `TakeProfit` and `StopLoss` use absolute price distances (e.g., `0.0010` corresponds to 10 pips on a 5-digit Forex symbol). They are evaluated on candle closes, replicating the EA's bar-close management.
- When `UseChannelStop` is enabled, the strategy reproduces the automatic stop-loss behaviour that relied on the `Price Channel` custom indicator.
- The strategy draws the three moving averages, the Donchian channel, and trade markers on the chart for visual confirmation.
