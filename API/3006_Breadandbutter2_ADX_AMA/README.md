# Bread and Butter 2 (ADX + AMA)

## Overview
This strategy is a port of the MetaTrader 5 expert advisor *Breadandbutter2* created by Ron Thompson. The original logic waits for a fresh bar, compares the latest Average Directional Index (ADX) value with the previous one, and checks whether the Kaufman Adaptive Moving Average (KAMA, also known as AMA) is rising or falling. A long position is opened when the trend strength weakens while price momentum improves, whereas a short position is opened when trend strength increases while momentum deteriorates. The StockSharp version keeps the behaviour of closing any opposite exposure before opening a new order, and applies the same fixed stop-loss and take-profit distances that were specified in pips in the original script.

## Indicators
- **Average Directional Index (ADX)** – measures the strength of the current trend. The strategy looks at the main ADX line and compares the last two values to determine whether trend strength is increasing or decreasing.
- **Kaufman Adaptive Moving Average (KAMA/AMA)** – adapts to market volatility using separate fast and slow smoothing constants. The strategy compares the last two values to evaluate momentum direction.

## Strategy Logic
1. Work with the configured candle type (default: 1-hour bars) and wait until a candle is fully closed before processing.
2. Calculate KAMA with the selected length, fast period, and slow period.
3. Calculate ADX with the configured averaging period and extract the main line value.
4. Compare the current and previous indicator readings:
   - **Long setup** – the ADX value decreases (trend strength weakens) while KAMA rises (price momentum improves).
   - **Short setup** – the ADX value increases while KAMA falls.
5. When a signal appears, close any opposite-side exposure and open a new market order so that the final position matches the base strategy volume.
6. Continuously monitor the active position. If price touches the configured stop-loss or take-profit levels (expressed in pips and converted to price units according to the security tick size), exit the trade immediately.

## Trade Management
- **Stop-loss** – expressed in pips; converted into price units using the security `PriceStep`. For symbols quoted with 3 or 5 decimal places the pip size is 10 times the price step, matching the MetaTrader implementation.
- **Take-profit** – also expressed in pips and handled in the same way as the stop-loss distance.
- The strategy uses market orders for entries and exits and flips the position when an opposite signal occurs.

## Parameters
| Name | Default | Description |
| ---- | ------- | ----------- |
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | Candle type used for all calculations. |
| `AdxPeriod` | `14` | Averaging length of the ADX main line. |
| `AmaPeriod` | `9` | Base period of the Kaufman Adaptive Moving Average. |
| `AmaFastPeriod` | `2` | Fast EMA period used inside the AMA. |
| `AmaSlowPeriod` | `30` | Slow EMA period used inside the AMA. |
| `StopLossPips` | `50` | Distance to the protective stop-loss in pips. Set to `0` to disable. |
| `TakeProfitPips` | `50` | Distance to the profit target in pips. Set to `0` to disable. |

## Usage Notes
- Ensure the strategy is attached to a security that exposes a valid `PriceStep`. For forex symbols with fractional pips the pip size is calculated automatically.
- `Volume` controls the base order size. When a reversal signal appears the algorithm adds enough volume to close any opposite exposure and establish a position equal to `Volume` in the new direction.
- Because stop-loss and take-profit exits are evaluated on candle highs and lows, the behaviour approximates the original MetaTrader pending-order execution.

## References
- Original MetaTrader 5 strategy: `MQL/22003/Breadandbutter2.mq5`
- StockSharp indicators: `KaufmanAdaptiveMovingAverage`, `AverageDirectionalIndex`
