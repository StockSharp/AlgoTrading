# NUp1Down Strategy

## Overview
The **NUp1Down Strategy** is a direct conversion of the MetaTrader 5 expert "N bars up, then one bar down" (file `NUp1Down.mq5`).
It scans completed candles delivered by StockSharp and enters a short trade when a bearish candle appears after a configurable
sequence of bullish candles that keep making higher closes. The strategy is designed for discretionary traders who want to
automate a classical swing-reversal pattern inside StockSharp Designer, Shell, or Runner.

## Trading Logic
1. Work only on finished candles provided by the `CandleType` parameter.
2. Keep the latest `BarsCount + 1` candles in memory. The newest candle must close below its open (bearish setup candle).
3. The previous `BarsCount` candles all have to close above their opens. Each of these bullish candles (except the oldest one)
   must also close above the close of the candle that came right before it, enforcing a "staircase" move higher.
4. When the pattern validates and there is no active short position, the strategy submits a market sell order.
5. Position sizing uses the `RiskPercent` parameter. The algorithm estimates how many contracts can be opened so that the
   capital at risk (distance to the stop-loss converted into monetary value) does not exceed the chosen percentage of the
   portfolio. The base `Volume` property remains the minimum lot size and the risk model can only increase the trade size.

## Position Management
- Upon entry a protective stop-loss and a take-profit level are computed from the entry price. Both distances are expressed in
  pips and translated into prices using the instrument's `PriceStep`. For symbols with three or five decimal digits the pip size
  is automatically adjusted to match MetaTrader's pip definition.
- A trailing stop is recalculated on every finished candle. The trailing distance equals `TrailingStopPips` and the stop is
  shifted only if the price has moved at least `TrailingStepPips` in the trade's favor. The trailing logic emulates the original
  expert: for short trades it follows the ask price lower, while long trades are not produced by this strategy.
- Exit conditions are evaluated before looking for new entries on every candle. The strategy closes the position when either the
  stop-loss or the take-profit is hit, or when the trailing logic tightens the stop above the current ask price.

## Parameters
| Name | Description |
| ---- | ----------- |
| `BarsCount` | Number of bullish candles required before the bearish setup candle (default: 3). |
| `TakeProfitPips` | Take-profit distance in pips applied to the entry price (default: 50). |
| `StopLossPips` | Stop-loss distance in pips applied to the entry price (default: 50). |
| `TrailingStopPips` | Distance between market price and the trailing stop (default: 10). |
| `TrailingStepPips` | Minimum favorable movement before the trailing stop is advanced (default: 5). |
| `RiskPercent` | Percentage of portfolio capital to risk on each trade (default: 5). |
| `CandleType` | Candle data type/time frame used for pattern detection (default: 1 hour). |

## Usage Notes
- Configure the `Volume` property to the minimum order size allowed by your broker. The risk-based sizing may raise the trade
  size but never reduces it below `Volume`.
- The strategy keeps only one aggregated short position at any time. If a long position exists, it will be closed before opening
  the short.
- The algorithm works on candle data. Intrabar stop-loss or take-profit hits are detected using the candle high/low, so the
  actual fill timing may differ from tick-level execution.
- No Python version is provided in this release. Only the C# implementation inside `API/2574/CS/NUp1DownStrategy.cs` is
  available.
