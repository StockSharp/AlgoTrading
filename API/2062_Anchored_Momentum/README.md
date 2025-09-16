# Anchored Momentum Strategy

## Overview
The Anchored Momentum Strategy uses the ratio of an exponential moving average (EMA) to a simple moving average (SMA) to measure momentum. When the short-term EMA begins to rise faster than the long-term SMA, it indicates increasing bullish momentum. Conversely, a falling ratio signals strengthening bearish momentum.

## How It Works
1. **Indicators**
   - EMA with configurable period.
   - SMA with configurable period.
2. **Momentum Calculation**
   - `Momentum = 100 * (EMA / SMA - 1)`
   - Positive momentum means EMA is above SMA; negative momentum means EMA is below SMA.
3. **Trading Logic**
   - If momentum has been decreasing and then turns upward, the strategy enters a long position.
   - If momentum has been increasing and then turns downward, the strategy enters a short position.
   - Position size automatically includes the existing position to reverse when needed.
4. **Risk Management**
   - Stop-loss and take-profit levels are set as percentages of entry price using the built-in protection mechanism.

## Parameters
| Name | Description |
|------|-------------|
| `SmaPeriod` | Period for the SMA indicator. |
| `EmaPeriod` | Period for the EMA indicator. |
| `StopLossPercent` | Percentage for stop-loss. |
| `TakeProfitPercent` | Percentage for take-profit. |
| `CandleType` | Timeframe of candles used for calculations. |

## Notes
- The strategy works with finished candles only.
- All trading actions are executed using market orders.
- Indicator values are obtained through the high-level `Bind` API without accessing historical buffers directly.
