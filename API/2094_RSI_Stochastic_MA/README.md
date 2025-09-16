# RSI Stochastic MA Strategy

This strategy combines a simple moving average (SMA) trend filter with RSI and Stochastic oscillators.
The moving average defines the market bias. When price is above the SMA the strategy looks for long
entries; when below the SMA it seeks short entries. RSI and Stochastic levels identify oversold or
overbought conditions to time entries.

Positions are closed when the oscillators leave their extreme zones. This keeps trades aligned with
the prevailing trend while avoiding extended moves against the indicators.

## Parameters
- `RsiPeriod` – RSI calculation period.
- `RsiUpperLevel` – RSI overbought threshold.
- `RsiLowerLevel` – RSI oversold threshold.
- `MaPeriod` – period of the trend moving average.
- `StochKPeriod` – %K period for the Stochastic oscillator.
- `StochDPeriod` – %D smoothing period for the Stochastic oscillator.
- `StochUpperLevel` – Stochastic overbought level.
- `StochLowerLevel` – Stochastic oversold level.
- `Volume` – order volume.
- `CandleType` – candle data type used for calculations.

## Indicators
- Simple Moving Average
- Relative Strength Index
- Stochastic Oscillator

## Trading rules
- **Buy** when price is above the SMA, RSI is below `RsiLowerLevel`, and both Stochastic lines are below `StochLowerLevel`.
- **Sell** when price is below the SMA, RSI is above `RsiUpperLevel`, and both Stochastic lines are above `StochUpperLevel`.
- **Exit long** when RSI or Stochastic rises above their upper levels.
- **Exit short** when RSI or Stochastic falls below their lower levels.
