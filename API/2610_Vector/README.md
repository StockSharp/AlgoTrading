# Vector Strategy

## Overview
The Vector Strategy is a multi-currency trend-following system converted from the MetaTrader 5 "Vector" expert. It trades four major
forex pairs — EURUSD, GBPUSD, USDCHF, and USDJPY — simultaneously. The strategy calculates smoothed moving averages on the median
price of each pair and opens synchronized positions when the combined trend points in the same direction. A dynamic pip target based
on four-hour volatility and portfolio-level profit and loss thresholds control exits.

## Core Ideas
- Use smoothed moving averages (SMMA) built on median prices to measure direction on each currency pair.
- Summarize the fast and slow averages from all instruments to determine a common bullish or bearish bias.
- Enter a single market order per pair when the global bias and the local fast/slow crossover agree.
- Manage positions with a floating pip target derived from the average range of 50 completed 4-hour candles on EURUSD.
- Close all trades simultaneously if floating profit or loss reaches the configured percentage of the starting balance.

## Parameters
| Parameter | Description |
|-----------|-------------|
| **Fast MA** | Length of the smoothed moving average used for the fast trend on each pair. |
| **Slow MA** | Length of the smoothed moving average used for the slow trend on each pair. |
| **MA Shift** | Additional number of finished candles required before signals are evaluated, mirroring the shift setting in the original EA. |
| **Equity Take Profit %** | Floating profit percentage that triggers closing every open position. |
| **Equity Stop Loss %** | Floating loss percentage that triggers an emergency exit for all trades. |
| **Signal Timeframe** | Candle timeframe used for the smoothed moving averages (default 15 minutes). |
| **Range Timeframe** | Candle timeframe used for volatility averaging (default 4 hours). |
| **Range Period** | Number of higher-timeframe candles used to compute the average pip target. |
| **EURUSD / GBPUSD / USDCHF / USDJPY** | Securities that correspond to each traded instrument. |

All parameters support optimization ranges identical to the original expert advisor where applicable.

## Trading Logic
1. **Indicator update** — Each finished candle on a trading timeframe updates the fast and slow smoothed moving averages for the
   corresponding pair. Values are only considered after the configured warm-up (MA Shift) is complete.
2. **Bias calculation** — The strategy sums the latest fast averages from all pairs and subtracts the sum of slow averages. A positive
   result indicates bullish pressure, while a negative result indicates bearish pressure.
3. **Entry conditions** — When no position exists for a pair, the strategy enters a buy order if the global bias is bullish and the
   pair’s fast average is above the slow one. It opens a sell order in the opposite case.
4. **Pip target exit** — The EURUSD four-hour subscription computes the average candle range over the configured period. The current
   pip target is the larger of this average and 13 pips. Longs close once the price gains at least the target number of pips, and
   shorts close after an equivalent favorable move.
5. **Equity protection** — Whenever floating profit exceeds the take-profit percentage, or floating loss breaches the stop-loss
   percentage, the strategy immediately closes all managed positions.

## Usage Notes
- Attach the strategy to a portfolio that provides access to all four forex instruments and set each security parameter explicitly.
- The default signal timeframe is 15 minutes; ensure that matching candles are available for every currency pair.
- Only one open position per pair is maintained at any time. The volume parameter from the base strategy is used for every entry.
- Because exits rely on floating P/L, the strategy is intended for continuous operation rather than bar-by-bar backtesting only.
- The dynamic pip target uses EURUSD volatility in line with the original implementation. Adjust the range timeframe or period if you
  prefer to adapt the target to a different market environment.

