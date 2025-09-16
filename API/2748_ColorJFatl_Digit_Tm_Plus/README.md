# ColorJFatl Digit TM Plus Strategy

## Overview

The ColorJFatl Digit TM Plus strategy is a direct port of the MetaTrader 5
expert advisor *Exp_ColorJFatl_Digit_Tm_Plus*. It trades slope reversals of a
Fast Adaptive Trend Line (FATL) that is smoothed with a Jurik Moving Average
(JMA). The original indicator publishes three colors (up, flat, down). The
strategy reacts when the color on the latest finished bar changes and aligns
the position with the new slope.

The StockSharp implementation keeps the high-level behaviour of the MQL
version: orders are generated on closed candles, optional time-based exits are
available, and the lot sizing input is represented by the `TradeVolume`
parameter.

## Signal logic

1. **Indicator calculation**
   - Prices are fed through the 39-tap FATL digital filter supplied with the
     original indicator.
   - The filtered series is smoothed with a Jurik Moving Average. The length,
     applied price and rounding precision can be customised through parameters.
   - The color state is determined by the sign of the difference between the
     current and the previous smoothed values: `2` for bullish slope,
     `0` for bearish slope and `1` for neutral/unchanged.

2. **Entry conditions**
   - **Long entry** – enabled by `EnableBuyEntries`. Triggered when the current
     bar color becomes `2` while the previous bar color was less than `2`. Any
     existing short position is closed first when `EnableSellExits` is true.
   - **Short entry** – enabled by `EnableSellEntries`. Triggered when the
     current bar color becomes `0` while the previous color was greater than
     `0`. Any existing long position is closed first when `EnableBuyExits` is
     true.
   - Only one position can be open at a time. Orders are sent at the close of
     the confirming candle.

3. **Exit conditions**
   - **Slope reversal exits** – when the slope flips in the opposite direction
     the corresponding `EnableBuyExits` or `EnableSellExits` flag will close
     the open position.
   - **Time based exit** – if `UseTimeExit` is enabled, a position is closed
     after holding it for `HoldingMinutes` minutes.
   - **Protective levels** – `StopLossPoints` and `TakeProfitPoints` are
     expressed in price steps. They are evaluated on every finished candle by
     comparing the session high/low with the entry price.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Quantity used for market entries. |
| `StopLossPoints` | Protective stop distance in price steps. Set to `0` to disable. |
| `TakeProfitPoints` | Profit target distance in price steps. Set to `0` to disable. |
| `EnableBuyEntries` / `EnableSellEntries` | Enable or disable long/short entries. |
| `EnableBuyExits` / `EnableSellExits` | Enable or disable slope-based exits. |
| `UseTimeExit` | Enables the timed exit logic. |
| `HoldingMinutes` | Holding period in minutes when the timed exit is active. |
| `CandleType` | Time frame used for calculations (default 4 hours). |
| `JmaLength` | Jurik Moving Average smoothing length applied to the FATL output. |
| `AppliedPrice` | Price source for the digital filter (close, open, median, Demark, etc.). |
| `RoundingDigits` | Number of digits used when rounding the smoothed line. |
| `SignalBar` | Offset of the finished bar used to evaluate the indicator state. |

## Notes

- The strategy processes only fully completed candles and therefore works well
  with historical backtests.
- `AppliedPrice.Demark` reproduces the same calculation as the original MQL
  indicator.
- Because StockSharp handles order execution asynchronously, the internal
  tracking of the entry price is updated whenever a new position is opened and
  cleared whenever an exit order is submitted.
