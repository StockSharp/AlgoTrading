# SilverTrend V3 Strategy

## Overview
SilverTrend V3 is a trend-following strategy translated from the original MetaTrader 4 implementation. It evaluates the SilverTrend indicator together with the J_TPO statistical filter to identify new directional swings. The strategy trades a single instrument at a time and enforces a Friday evening flat rule to avoid holding risk over the weekend.

## Trading Logic
1. **Indicator Processing**
   - The strategy keeps a rolling buffer of recent candles and recalculates the SilverTrend direction on every completed bar.
   - SilverTrend uses a 9-bar window and a risk factor of 3 to determine the adaptive channel limits. If the closing price pierces above the upper limit the signal flips to bullish; crossing below the lower limit flips the signal to bearish.
   - The J_TPO calculation (length 14) measures price distribution skewness. Only positive J_TPO values confirm long entries, while negative readings are required for shorts.
2. **Entry Conditions**
   - A long trade is opened when the SilverTrend signal changes from bearish to bullish and J_TPO is above zero.
   - A short trade is opened when the SilverTrend signal changes from bullish to bearish and J_TPO is below zero.
   - New positions are blocked on Fridays once the market hour exceeds the configured cutoff.
3. **Exit Management**
   - Opposite SilverTrend signals close open trades immediately.
   - Optional initial stop loss and take profit levels are placed at fixed distances (expressed in points).
   - An optional trailing stop follows the price once it moves beyond the configured profit buffer.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `Volume` | Order size in lots. | `1` |
| `TrailingStopPoints` | Trailing stop distance in price points. `0` disables trailing. | `0` |
| `TakeProfitPoints` | Take profit distance in price points. `0` disables the take profit. | `0` |
| `InitialStopPoints` | Initial stop loss distance in price points. `0` disables the protective stop. | `0` |
| `FridayCutoffHour` | Hour (exchange time) after which new trades on Friday are disallowed. | `16` |
| `CandleType` | Candle type or timeframe used for analysis. | `1h` candles |

## Additional Notes
- Only one position is open at any time, matching the single-trade behavior of the original expert advisor.
- The implementation uses StockSharp high-level API, so the strategy subscribes to candles and performs logic only on finished bars.
- Trailing and fixed stops are managed internally and will close the position at market price once triggered.
