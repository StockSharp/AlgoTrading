# Executor AO Strategy

## Overview
Executor AO is a saucer-style Awesome Oscillator strategy originally distributed as the "Executor AO" MetaTrader expert advisor.
The StockSharp port keeps the indicator-based reversal detection while simplifying money management to a fixed order size. The
strategy watches completed candles from the configured timeframe, evaluates the Awesome Oscillator slope change, and opens a
single net position whenever bullish or bearish conditions occur below or above the zero line. Optional protective stop, take-
profit, and trailing logic reproduce the original EA's risk management behaviour.

## Trading logic
1. Subscribe to the candle series defined by `CandleType` and feed every finished candle into the Awesome Oscillator with the
   configured `AoShortPeriod` and `AoLongPeriod` parameters.
2. Store the last three completed Awesome Oscillator values to reproduce the MetaTrader buffer access pattern used by the
   original expert.
3. When no position is open:
   - **Bullish setup**: the latest AO value is greater than the previous one, the previous value is lower than the value two bars
     ago (a trough), and the latest value remains below `-MinimumAoIndent`. In that case send a market buy order with
     `TradeVolume` lots.
   - **Bearish setup**: the latest AO value is smaller than the previous one, the previous value is higher than the value two bars
     ago (a peak), and the latest value stays above `MinimumAoIndent`. In that case submit a market sell order with the fixed
     volume.
4. When a position exists, the strategy emulates the EA's exits:
   - Calculate stop-loss and take-profit prices from the entry using `StopLossPips` and `TakeProfitPips` multiplied by the
     adjusted pip size (MetaTrader's 3/5-digit handling is replicated).
   - Apply the trailing-stop rule whenever price moves in favour of the position by more than `TrailingStopPips +
     TrailingStepPips` pips. The stop is only advanced if the new level is beyond the previous one, matching the EA's trailing
     step requirement.
   - Close long positions when price touches the take-profit or stop-loss or when the Awesome Oscillator value from the previous
     bar turns positive. Close short positions when price hits their targets or the previous AO value falls below zero.
5. All orders are market orders; StockSharp's net position model ensures only one direction is active at a time.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 5-minute candles | Primary timeframe used to compute and trade the strategy. |
| `TradeVolume` | `decimal` | `1` | Fixed order size used for every entry. |
| `AoShortPeriod` | `int` | `5` | Fast period for the Awesome Oscillator's short SMA. |
| `AoLongPeriod` | `int` | `34` | Slow period for the Awesome Oscillator's long SMA. |
| `MinimumAoIndent` | `decimal` | `0.001` | Minimum absolute distance from zero required for new signals. Prevents trades when AO hovers around zero. |
| `StopLossPips` | `decimal` | `50` | Protective stop-loss distance expressed in MetaTrader-style pips. Set to `0` to disable the stop. |
| `TakeProfitPips` | `decimal` | `50` | Take-profit distance expressed in pips. Set to `0` to disable the target. |
| `TrailingStopPips` | `decimal` | `5` | Trailing-stop activation distance. Only used when greater than zero. |
| `TrailingStepPips` | `decimal` | `5` | Minimum price improvement required before the trailing stop is updated. Must stay positive when trailing is enabled. |

## Differences versus the MetaTrader EA
- The MetaTrader version allowed risk-based position sizing. The StockSharp port implements the fixed-lot option (`TradeVolume`)
  and leaves percent-risk management out for clarity.
- Order management is simulated inside the strategy: when stop-loss or take-profit thresholds are reached on completed candles,
  the strategy sends market orders to flatten the position. This mirrors the EA's behaviour without creating separate child
  orders.
- Trailing adjustments occur on candle close events rather than on every tick. This keeps the implementation consistent with the
  high-level API while following the same threshold logic.
- All code paths rely on StockSharp's high-level `SubscribeCandles` + `Bind` pattern instead of manually copying indicator
  buffers.

## Usage tips
- Align `TradeVolume` with the instrument's lot step before starting the strategy. The constructor also assigns the same value to
  `Strategy.Volume`, so helper methods automatically use the chosen size.
- `MinimumAoIndent` can be increased on noisy markets to avoid frequent flips near zero. Setting it to `0` reproduces the most
  aggressive behaviour of the EA.
- When enabling the trailing stop, keep `TrailingStepPips` above zero; otherwise the constructor throws an exception, reproducing
  the original EA's parameter validation.
- Attach the strategy to a chart to visualise both candles and the Awesome Oscillator overlay. This helps validate trough/peak
  detection after conversion.

## Indicator
- **Awesome Oscillator**: difference between a fast and a slow simple moving average of the median price. The default 5/34
  configuration matches the MetaTrader indicator.
