# Forex Profit Boost Strategy

## Overview
The **Forex Profit Boost** strategy is a reversal trading system that combines a fast Exponential Moving Average (EMA) and a slow Simple Moving Average (SMA). The strategy waits for the fast EMA to cross the slow SMA and then trades against the direction of the crossover, expecting a price retracement. Optional stop-loss and take-profit levels in absolute price points can be configured for risk management.

## Indicators
- **EMA (fast)**: default period 7.
- **SMA (slow)**: default period 21.

## Trading Rules
1. Subscribe to the selected candle timeframe.
2. Calculate EMA and SMA values on every finished candle.
3. When the fast EMA crosses **below** the slow SMA:
   - Close any short positions.
   - Open a new long position.
4. When the fast EMA crosses **above** the slow SMA:
   - Close any long positions.
   - Open a new short position.
5. Apply stop-loss and take-profit levels relative to the entry price if specified.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `FastPeriod` | Period for the fast EMA. | 7 |
| `SlowPeriod` | Period for the slow SMA. | 21 |
| `StopLoss` | Stop-loss distance in price points. | 1000 |
| `TakeProfit` | Take-profit distance in price points. | 2000 |
| `CandleType` | Timeframe used for calculations. | 1 hour |

## Notes
- The strategy uses the high-level StockSharp API and does not store historical collections.
- Trades are executed using market orders only after a candle is finished.
- All comments in the source code are written in English as required.
