# MAMy Expert Strategy

## Overview
- Port of the MetaTrader 5 "MAMy Expert" advisor by Victor Chebotariov to the StockSharp high-level strategy API.
- Reproduces the original custom indicator that compares three moving averages of different price sources (open, close, weighted price).
- Works strictly on completed candles and manages at most one net position at a time, mirroring the behaviour of the MQL expert.

## Indicator foundation
- The strategy builds three moving averages using the same length and smoothing algorithm:
  - `MA(close)` – calculated on candle close prices.
  - `MA(open)` – calculated on candle open prices.
  - `MA(weighted)` – calculated on the weighted price `(High + Low + 2 × Close) / 4`.
- The `MaType` parameter selects the averaging algorithm (Simple, Exponential, Smoothed, or Weighted LWMA) for all three series, matching the `MODE_*` options from MetaTrader.
- A "close buffer" is computed as the difference `MA(close) − MA(weighted)`.
- A potential "open buffer" is produced only when the moving averages align in a trending configuration:
  - **Downtrend setup**: both `MA(close)` and `MA(weighted)` fall, the close MA stays below the weighted MA, both remain below the open MA, and the close buffer decreases.
  - **Uptrend setup**: both `MA(close)` and `MA(weighted)` rise, the close MA stays above the weighted MA, both remain above the open MA, and the close buffer increases.
  - When either setup is true, the open buffer becomes `(MA(weighted) − MA(open)) + (MA(close) − MA(weighted))`; otherwise it is reset to zero.
- If a fresh positive open buffer accompanies a negative cross of the close buffer, the close buffer is forced to zero, just like in the original indicator code.

## Signal logic
- **Entry conditions**
  - **Buy** when the open buffer crosses upward through zero (`previous ≤ 0`, `current > 0`).
  - **Sell** when the open buffer crosses downward through zero (`previous ≥ 0`, `current < 0`).
  - Entries are considered only when there is no existing position.
- **Exit conditions**
  - **Close long** when the close buffer crosses below zero (`previous ≥ 0`, `current < 0`).
  - **Close short** when the close buffer crosses above zero (`previous ≤ 0`, `current > 0`).
  - Exits are evaluated before new entries, so the strategy never holds simultaneous long and short exposure.
- Orders are issued at market using the configured `TradeVolume`. Protective automation via `StartProtection()` mirrors the safety call in the StockSharp samples.

## Charting and data flow
- Subscribes to the timeframe defined by `CandleType` and processes only finished candles.
- Draws price candles alongside all three moving averages and annotates filled orders, providing the same visual cues that the original indicator delivered in MetaTrader.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Primary timeframe that supplies candles for the indicator and signals. |
| `MaPeriod` | `int` | `3` | Length applied to all three moving averages. |
| `MaType` | `MaCalculationType` | `Weighted` | Averaging algorithm (Simple, Exponential, Smoothed, Weighted). |
| `TradeVolume` | `decimal` | `1` | Volume used for each market order entry. |

## Implementation notes
- Uses the StockSharp high-level `SubscribeCandles().Bind(...)` workflow and built-in moving-average indicators; no custom buffers are stored beyond the latest values required for signal detection.
- Signals are evaluated only after all indicators are fully formed and the strategy is ready for live trading (`IsFormedAndOnlineAndAllowTrading()`).
- The strategy intentionally ignores concurrent entries while a position is open, faithfully matching the logic of the source expert advisor.
