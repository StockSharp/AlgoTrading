# Heiken Ashi Smoothed MTF Strategy

## Overview
The Heiken Ashi Smoothed MTF strategy is a port of the "HASNEWJ" MetaTrader expert advisor. It rebuilds the custom smoothed Heiken Ashi indicator on six timeframes (M1, M5, M15, M30, H1, H4) and waits for trend alignment across the higher frames. A trade is opened when the lower M5 stream shows a fresh pullback while the longer-term smoothed candles remain strongly bullish or bearish. Manual stop-loss and take-profit logic replicates the behaviour of the original EA, including the ability to widen the stop slightly after a losing trade.

## Indicators and Data
- **Smoothed Heiken Ashi candles** on M1, M5, M15, M30, H1 and H4.
  - The first smoothing pass applies a configurable moving average method/length to the raw OHLC values.
  - The second pass smooths the interim Heiken Ashi open/close with another configurable moving average.
- **Directional counters** that track how many one-minute updates each timeframe has stayed bullish or bearish.
- **Raw close price** from the M1 series for risk management checks.

## Entry Logic
1. Update the smoothed Heiken Ashi direction for each timeframe whenever a candle finishes.
2. On every finished M1 candle, increment or reset the bullish/bearish counters depending on the latest direction of each timeframe.
3. **Buy conditions:**
   - M5 smoothed Heiken Ashi is bullish and the bullish counter is below `MaxM5TrendLength` (default 10 updates).
   - M15 smoothed Heiken Ashi is bullish and its bullish counter is above `MinM15TrendLength` (default 200 updates).
   - M30, H1 and H4 smoothed Heiken Ashi candles are also bullish.
   - No long position is currently open (short exposure is allowed and will be flipped).
4. **Sell conditions:**
   - M5 smoothed Heiken Ashi is bearish and the bearish counter is below `MaxM5TrendLength`.
   - M15 smoothed Heiken Ashi is bearish and its bearish counter is above `MinM15TrendLength`.
   - M30, H1 and H4 smoothed candles are bearish.
   - No short position is currently open (long exposure is closed or reversed).
5. The market order volume equals `TradeVolume` plus the absolute value of the opposite exposure to ensure flips close the prior trade.

## Risk Management
- A manual stop-loss and take-profit are evaluated on every finished M1 candle using `Security.PriceStep`.
- The take-profit closes the position once price moves `TakeProfitPoints` steps in favour of the trade.
- The stop-loss closes the position once price moves `StopLossPoints` steps against the trade.
- After a losing trade the next entry widens the stop-loss by `ExtraStopLossPoints` steps, mimicking the EA's "fail" flag.
- Trade volume is fixed by `TradeVolume`; no pyramiding or scaling logic is applied beyond reversing existing exposure.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `TradeVolume` | Base order volume used for entries | `0.1` |
| `TakeProfitPoints` | Take-profit distance in price steps | `20` |
| `StopLossPoints` | Stop-loss distance in price steps | `500` |
| `ExtraStopLossPoints` | Additional stop steps applied after a losing trade | `5` |
| `FirstMaPeriod` | Length of the first smoothing moving average | `6` |
| `FirstMaMethod` | Method of the first smoothing MA (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`) | `Smoothed` |
| `SecondMaPeriod` | Length of the second smoothing moving average | `2` |
| `SecondMaMethod` | Method of the second smoothing MA | `LinearWeighted` |
| `MaxM5TrendLength` | Maximum number of M5 updates allowed before cancelling a pullback entry | `10` |
| `MinM15TrendLength` | Minimum number of M15 updates required to confirm the higher trend | `200` |
| `M1CandleType` | Data type for the base one-minute candle stream | `TimeFrame(00:01:00)` |
| `M5CandleType` | Data type for the five-minute confirmation stream | `TimeFrame(00:05:00)` |
| `M15CandleType` | Data type for the fifteen-minute confirmation stream | `TimeFrame(00:15:00)` |
| `M30CandleType` | Data type for the thirty-minute confirmation stream | `TimeFrame(00:30:00)` |
| `H1CandleType` | Data type for the hourly confirmation stream | `TimeFrame(01:00:00)` |
| `H4CandleType` | Data type for the four-hour confirmation stream | `TimeFrame(04:00:00)` |

## Usage Notes
- The directional counters are updated once per finished M1 candle, which approximates the tick-based counters from MetaTrader while keeping the implementation candle-driven.
- Ensure `Security.PriceStep` is configured; otherwise the strategy falls back to a 0.0001 step when computing stop and target levels.
- Both smoothing passes rely on moving averages; experimenting with different combinations of methods and periods can adapt the system to instruments with different volatility profiles.
