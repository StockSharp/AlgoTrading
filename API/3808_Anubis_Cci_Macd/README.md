# Anubis CCI MACD Strategy

## Summary
- Converts the MetaTrader 4 expert advisor "Anubis" to the StockSharp high level API.
- Uses a 4-hour Commodity Channel Index (CCI) filter together with a 15-minute MACD crossover.
- Applies adaptive position sizing, stop-loss, breakeven protection, ATR driven exits, and a standard deviation based take-profit.

## Strategy logic
1. **Data**
   - Primary timeframe: 15-minute candles (`SignalCandleType`), used for MACD and ATR calculations.
   - Higher timeframe: 4-hour candles (`TrendCandleType`), used for CCI filtering and standard deviation measurement.
2. **Indicators**
   - `CommodityChannelIndex` with configurable period on the 4H series.
   - `StandardDeviation` (length 30) on 4H closes to estimate the take-profit distance.
   - `MovingAverageConvergenceDivergenceSignal` (fast/slow/signal configurable) on 15M candles.
   - `AverageTrueRange` (length 12) on 15M candles for volatility based exits.
3. **Entries**
   - **Short**: when 4H CCI is above `CciThreshold`, the previous two MACD values show a bearish crossover (MACD crossing below its signal), MACD was positive, there are no open longs, and the price has moved at least `PriceFilterPoints` since the last short entry.
   - **Long**: symmetric condition with CCI below `-CciThreshold`, MACD crossing upwards while negative, no open shorts, and the minimum distance filter satisfied.
4. **Risk management**
   - Base volume is defined by `VolumeValue` and is scaled by account equity (2× above 14k, 3.2× above 22k) and by `LossFactor` after a losing trade.
   - Maximum simultaneous trades per direction are limited by `MaxLongTrades` and `MaxShortTrades`.
   - Hard stop-loss placed virtually at `StopLossPoints * PriceStep` from the average entry price.
   - Breakeven activates once price advances by `BreakevenPoints` and immediately closes the position if price returns to the entry.
5. **Exits**
   - Standard deviation take-profit closes the position once price moves `StdDevMultiplier * StdDev` in favor.
   - Aggressive exits trigger when the prior candle range exceeds `CloseAtrMultiplier * ATR`.
   - MACD deceleration exits require both sufficient profit (`ProfitThresholdPoints`) and a reversal in MACD slope (previous MACD less than or greater than two bars ago, depending on direction).
   - Protective stop closes the trade if price pierces the stop-loss distance or falls back to entry after breakeven activation.

## Parameters
| Name | Description |
| ---- | ----------- |
| `VolumeValue` | Base order volume. |
| `CciThreshold` | Absolute threshold for the 4H CCI filter. |
| `CciPeriod` | Period of the 4H CCI indicator. |
| `StopLossPoints` | Stop-loss distance in points. |
| `BreakevenPoints` | Profit in points required to arm breakeven. |
| `MacdFastPeriod` | Fast EMA period for MACD. |
| `MacdSlowPeriod` | Slow EMA period for MACD. |
| `MacdSignalPeriod` | Signal EMA period for MACD. |
| `LossFactor` | Volume multiplier applied after a losing trade. |
| `MaxShortTrades` | Maximum number of concurrent short entries. |
| `MaxLongTrades` | Maximum number of concurrent long entries. |
| `CloseAtrMultiplier` | ATR multiplier for early exits. |
| `ProfitThresholdPoints` | Additional profit buffer (points) before MACD exits. |
| `StdDevMultiplier` | Standard deviation multiplier for the take-profit. |
| `PriceFilterPoints` | Minimum price movement between consecutive entries. |
| `SignalCandleType` | Primary timeframe for MACD and ATR. |
| `TrendCandleType` | Higher timeframe for CCI and standard deviation. |

## Notes
- The strategy relies on valid `Security.PriceStep` metadata to translate point-based parameters to price distances.
- Protective logic is implemented via explicit checks instead of pending stop/limit orders, mirroring the original EA behavior with virtual stops.
- Python version is intentionally omitted per task instructions.
