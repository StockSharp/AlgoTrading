# ComFracti Strategy

## Overview

ComFracti is a directional strategy translated from the MT4 "ComFracti" expert advisor. The logic combines multi-timeframe fractal confirmation with RSI and stochastic filters, while optional moving average, parabolic SAR, channel and perceptron filters control trend alignment. The C# implementation trades a single position at a time and evaluates signals on completed candles using StockSharp high-level APIs.

## Trading Logic

- **Primary signal**
  - Confirms a bullish setup when both the current timeframe and the higher timeframe produce a bullish fractal signal.
  - Confirms a bearish setup when both timeframes produce a bearish fractal signal.
  - RSI (default 3-period on the higher timeframe) must sit below `50 - RsiLevelBuy` for longs or above `50 + RsiLevelSell` for shorts when the RSI filter is enabled.
  - The stochastic oscillator (default %K period 5 with %D smoothing 3/3) must be below `50 - StochasticLevelBuy` for longs or above `50 + StochasticLevelSell` for shorts when the stochastic filter is enabled.
- **Optional filters**
  - **EMA slope**: the EMA on the filter timeframe must be rising for longs and falling for shorts.
  - **Parabolic SAR**: the SAR value must stay below the bar open for longs or above for shorts.
  - **Channel breakout**: compares the previous bar against an adaptive Donchian-style channel; previous lows must remain above the channel floor for longs, while previous highs must remain below the ceiling for shorts.
  - **Perceptron**: a weighted sum of recent high/low differences must be positive for longs and negative for shorts.
- **Position management**
  - Only one position is active at a time; the strategy closes existing exposure before opening a new trade in the opposite direction.
  - Fixed stop-loss and take-profit distances are expressed in instrument points.
  - An optional trailing stop moves in the direction of profit once the trailing buffer is reached (when `ProfitTrailing` is true).
  - When `CloseOnOppositeSignal` is enabled the strategy exits early if the opposite primary signal appears.

## Risk Management

- Base position size equals the `BaseVolume` parameter (default 0.1 lots). When `AccountMicro` is enabled the volume is divided by ten.
- If `UseMoneyManagement` is enabled the strategy risks `RiskPercent` of the account value per trade, using the configured stop-loss distance and the instrument step value to approximate position size. The computed volume is clamped by `MinimumVolume`.

## Parameters

| Name | Description |
| --- | --- |
| `TakeProfitPoints`, `StopLossPoints` | Take-profit and stop-loss distances in instrument points. |
| `UseTrailingStop`, `TrailingStopPoints`, `ProfitTrailing` | Trailing stop controls (distance and whether trailing requires open profit). |
| `BaseVolume`, `UseMoneyManagement`, `RiskPercent`, `AccountMicro`, `MinimumVolume` | Position sizing configuration. |
| `UseFractals`, `FractalShift*` | Enables fractal confirmation and defines the bar offsets to inspect on the current and higher timeframes. |
| `UseRsi`, `RsiLevelBuy`, `RsiLevelSell`, `RsiType` | RSI filter offsets and timeframe. |
| `UseStochastic`, `StochasticPeriod*`, `StochasticLevel*` | Stochastic oscillator periods and thresholds. |
| `UseMaFilter`, `MaPeriod` | EMA filter configuration on the filter timeframe. |
| `UsePsarFilter`, `PsarStep` | Parabolic SAR filter configuration. |
| `UseChannelFilter`, `ChannelLookback`, `ChannelK` | Channel breakout filter parameters. |
| `UsePerceptronFilter`, `PerceptronV1`–`PerceptronV4` | Perceptron filter weights (0–100, centred around 50). |
| `CandleType`, `HigherFractalType`, `FilterType` | Data timeframes used by the strategy. |

## Notes

- The strategy processes finished candles only, so behaviour may differ slightly from the original tick-driven expert advisor.
- The fractal tracker reproduces the MT4 five-bar fractal logic and allows the user to shift which historical bar is evaluated, matching the MT4 `sh1/ sh2` parameters.
- Money management relies on available portfolio valuation within StockSharp; when no valuation is available the strategy falls back to the fixed base volume.
