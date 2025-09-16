# Exp KWAN NRP Strategy

## Overview
The Exp KWAN NRP strategy reproduces the original MetaTrader expert advisor by combining a stochastic oscillator, relative strength index, and momentum indicator into a single ratio. The ratio is smoothed with a configurable moving average, and the slope of the smoothed line determines when to open or close positions. The approach works on any symbol or timeframe and is designed for directional trading when momentum shifts.

## Trading Logic
1. Build the KWAN ratio by multiplying the stochastic %D line by the RSI value and dividing by the momentum reading.
2. Smooth the ratio with the selected moving average method (simple, exponential, smoothed, or weighted).
3. Evaluate the slope of the smoothed line at the configurable signal bar offset.
4. Enter long positions when the line turns upward and exit short positions. Enter short positions when the line turns downward and exit long positions.
5. Optional stop-loss and take-profit protection can automatically close positions after a predefined price move measured in price steps.

## Signals
- **Long entry**: The smoothed KWAN value at the signal bar rises compared to the previous bar and long entries are enabled.
- **Long exit**: The smoothed KWAN value turns down while a long position is open and long exits are enabled.
- **Short entry**: The smoothed KWAN value at the signal bar falls compared to the previous bar and short entries are enabled.
- **Short exit**: The smoothed KWAN value turns up while a short position is open and short exits are enabled.

## Risk Management
- Set the strategy `Volume` property to control baseline order size. Position flipping automatically closes an opposite position before opening a new one.
- Enable `UseProtection` to apply stop-loss and take-profit levels measured in instrument price steps. Both protections can be used together or separately.
- The strategy subscribes to candles defined by `CandleType` and trades at the close of finished candles.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Timeframe used for indicator calculations and signal evaluation. | 1 hour candles |
| `KPeriod` | Period of the stochastic %K line. | 5 |
| `DPeriod` | Period of the stochastic %D line. | 3 |
| `SlowingPeriod` | Additional smoothing applied to the stochastic %K line. | 3 |
| `RsiPeriod` | Period of the relative strength index. | 14 |
| `MomentumPeriod` | Period of the momentum indicator. | 14 |
| `SmoothingMethod` | Moving average type applied to the KWAN ratio (Simple, Exponential, Smoothed, Weighted). | Simple |
| `SmoothingLength` | Length of the smoothing moving average. | 3 |
| `SignalBar` | Number of bars back used to evaluate the slope (0 = current closed bar). | 1 |
| `EnableBuyEntries` | Allow opening long positions on bullish signals. | true |
| `EnableSellEntries` | Allow opening short positions on bearish signals. | true |
| `EnableBuyExits` | Allow closing long positions when a bearish signal appears. | true |
| `EnableSellExits` | Allow closing short positions when a bullish signal appears. | true |
| `UseProtection` | Enable stop-loss and take-profit protections. | true |
| `StopLossSteps` | Stop-loss distance expressed in price steps. | 1000 |
| `TakeProfitSteps` | Take-profit distance expressed in price steps. | 2000 |

## Usage Notes
- The KWAN ratio can become unstable when the momentum indicator equals zero. The strategy automatically skips signals for those bars to avoid division by zero.
- The `SignalBar` parameter allows aligning signals with historical bars if delayed confirmation is needed.
- Combine with brokerage-level risk controls or additional filters if required for production trading.
