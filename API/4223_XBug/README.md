# X Bug Strategy

## Overview
The **X Bug Strategy** is a moving average crossover system converted from the MQL4 expert advisor with the same name. It compares two simple moving averages calculated on the median candle price. When the fast average crosses above or below the slow average, the strategy opens a position in the direction of the crossover. The implementation reproduces the original Expert Advisor features including optional signal reversal, automatic position closing on opposite signals, and pip-based protective orders.

## Trading Logic
1. Subscribe to the configured candle type (one-minute candles by default) and calculate two simple moving averages: a fast line and a slow line. The averages use the median price and respect the configured indicator shifts.
2. Detect a bullish crossover when the current fast value is above the slow value while the fast value two bars earlier was below the slow value. Detect a bearish crossover using the opposite condition.
3. Optionally invert the crossover signal when **ReverseSignals** is enabled to trade the opposite direction.
4. When **CloseOnSignal** is enabled, immediately close any opposing position before entering a new one on the fresh signal.
5. Enter long positions on bullish signals and short positions on bearish signals. The strategy avoids stacking positions in the same direction; it only trades when the current position is flat or aligned with the signal.

## Risk Management
- **StopLossPips** – sets an absolute protective stop in pips. The stop is expressed in whole pips; fractional pricing (5-digit or 3-digit quotes) is automatically handled by converting the pip value using the security price step.
- **TakeProfitPips** – configures the profit target distance in pips.
- **TrailingStopPips** – when **UseTrailingStop** is enabled, activates a trailing stop that starts at the configured pip distance once the position moves into profit. The trailing step matches the trailing distance, replicating the original MetaTrader logic.
- All protective orders are managed through `StartProtection` with market exits to maintain parity with the MQL4 expert.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `OrderVolume` | Base trade volume used for market entries. | `0.1` |
| `StopLossPips` | Stop-loss distance measured in pips; set to `0` to disable. | `70` |
| `TakeProfitPips` | Take-profit distance measured in pips; set to `0` to disable. | `5000` |
| `UseTrailingStop` | Enables or disables trailing stop management. | `true` |
| `TrailingStopPips` | Trailing distance in pips. | `90` |
| `FastPeriod` | Period of the fast moving average. | `1` |
| `FastShift` | Bars to shift the fast moving average before evaluating signals. | `0` |
| `SlowPeriod` | Period of the slow moving average. | `14` |
| `SlowShift` | Bars to shift the slow moving average before evaluating signals. | `10` |
| `CloseOnSignal` | Close an opposing position immediately when a new signal appears. | `true` |
| `ReverseSignals` | Invert signal direction to trade counter to the crossover. | `false` |
| `AppliedPrice` | Candle price source supplied to the moving averages. | `Median` |
| `CandleType` | Candle data type for signal generation. | `1 minute` time frame |

## Notes
- The pip conversion multiplies the price step by 10 for symbols quoted with 5 or 3 decimal places, matching the original Expert Advisor behaviour.
- No Python port is provided; only the C# strategy is included in this directory.
- Trailing stops, stops, and targets are optional. Set the corresponding pip values to zero to disable them.
