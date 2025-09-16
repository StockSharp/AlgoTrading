# Evening Star Strategy

## Overview
This strategy is a direct port of the **EveningStar.mq5** Expert Advisor (MQL5 id 18507). It watches for the classical Evening Star candlestick formation and opens a position as soon as the next bar starts trading. The logic has been rewritten on top of the StockSharp high level API while keeping the original risk management and pattern filters.

## Trading Logic
1. The strategy subscribes to the timeframe selected by the `CandleType` parameter. All processing happens on finished candles only.
2. Every time a new candle closes the last snapshots are cached so that the three-candle window defined by `Shift` can be evaluated.
3. The Evening Star pattern is considered valid when:
   - Candle *N-2* (oldest) is bullish (`open < close`).
   - Candle *N-1* (middle) satisfies the `Candle2Bullish` preference (bullish by default).
   - Candle *N* (most recent) is bearish (`open > close`).
   - If `CheckCandleSizes` is enabled the middle candle must have the smallest body of the three.
   - If `ConsiderGap` is enabled there must be a gap between candle bodies in the same way as the original robot (gap size equals one pip calculated from the security price step).
4. Once the pattern is confirmed the strategy checks the direction selected by `Direction`:
   - `Short` (default) opens a sell order, matching the original Evening Star behaviour.
   - `Long` allows running the exact opposite exposure (kept for feature parity with the MQL version).
5. Before opening a position the algorithm optionally closes the opposite exposure if `CloseOppositePositions` is set to `true`.
6. Stop-loss and take-profit prices are computed from the pip distances (`StopLossPips`, `TakeProfitPips`) using the same 3/5 digit adjustment that existed in MetaTrader.
7. Position size is derived from the current portfolio value and `RiskPercent`. If the calculated volume is smaller than the minimum tradable size the signal is ignored.

## Position Management
- When a long position is active the strategy monitors every new candle. If the low price trades through the stop level or the high price reaches the take-profit level the entire position is closed at market.
- When a short position is active the same logic is applied with inverted comparisons.
- If the portfolio value or the stop distance equals zero the order size cannot be calculated, therefore the entry is skipped.

## Parameters
| Name | Default | Description |
| ---- | ------- | ----------- |
| `Direction` | `Short` | Chooses whether the pattern should open a long or short position. |
| `TakeProfitPips` | `150` | Distance to the profit target expressed in pips. Set to zero to disable. |
| `StopLossPips` | `50` | Distance to the protective stop in pips. A non-positive value disables the trade. |
| `RiskPercent` | `5` | Percentage of the portfolio equity risked per trade. Used to compute the order volume. |
| `Shift` | `1` | Number of bars skipped from the most recent candle before evaluating the pattern. |
| `ConsiderGap` | `true` | Requires a gap between candle bodies just like the original Expert Advisor. |
| `Candle2Bullish` | `true` | Forces the middle candle to be bullish. Disable to require a bearish middle candle. |
| `CheckCandleSizes` | `true` | Ensures the middle candle has the smallest absolute body. |
| `CloseOppositePositions` | `true` | Closes the opposite exposure before sending the new order. |
| `CandleType` | `1H` time frame | Candle series used for analysis. |

## Notes
- The pip size is derived from the security price step. For 3 and 5 digit forex symbols one pip equals ten price steps, reproducing the behaviour of the original EA.
- If `StopLossPips` is zero the position size cannot be computed and the signal is ignored to prevent unlimited risk.
- The strategy automatically trims the cached history, so memory usage remains constant even on long sessions.
