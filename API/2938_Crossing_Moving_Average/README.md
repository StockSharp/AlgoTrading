# Crossing Moving Average Strategy

## Overview
- Conversion of the MetaTrader 5 expert advisor **"Crossing Moving Average (barabashkakvn's edition)"** from the `MQL/21515` source.
- Implements the logic on top of the StockSharp high-level API with candle subscriptions and indicator binding.
- Designed for instruments where momentum and moving average crossovers capture trend reversals.
- This package contains only the C# version. A Python translation is intentionally omitted as requested.

## Core Idea
The strategy monitors two configurable moving averages (fast and slow) with optional forward shifts and combines their crossover with a momentum confirmation filter. A trade is opened only when:
1. The fast average crosses the slow average by at least the configured minimum distance (in pips) over the two most recent completed bars.
2. The momentum indicator rises above (for long) or falls below (for short) the user-defined threshold and is improving in the direction of the trade.
3. The signal price source can be chosen between open, high, low, close, median, typical, or weighted candle prices to mimic MetaTrader applied price modes.

## Risk & Trade Management
- **Order volume** is fixed per trade and is applied both when entering a fresh position and when reversing an existing position.
- **Stop-loss / Take-profit** distances are configured in pips and automatically translated into price offsets using `Security.PriceStep`. For instruments quoted with 3 or 5 decimal digits the strategy multiplies the step by 10 to reproduce MetaTrader pip sizing.
- **Trailing stop** activates after price moves by `TrailingStop + TrailingStep` (in pips) from the entry. Once triggered, the stop is moved to `current price - TrailingStop` for long positions (or `current price + TrailingStop` for shorts) whenever it can be advanced by at least `TrailingStep` pips.
- Protective levels are evaluated on every finished candle: if the candle's range touches the stop-loss or take-profit, the position is closed at market to mimic order execution in MetaTrader.

## Indicators
- **Fast Moving Average** – configurable period, shift, and smoothing method (SMA, EMA, SMMA, WMA).
- **Slow Moving Average** – same options as the fast MA.
- **Momentum** – period and price source identical to the moving averages. The strategy auto-detects whether the indicator outputs values around 0 or 100 and applies the filter accordingly.

## Signal Logic
1. Wait for all indicators to be fully formed. The algorithm keeps an internal history of the most recent values to evaluate shifted crossovers exactly as in the original expert advisor.
2. Calculate the price distance between the fast and slow averages on the two previous bars (with applied shifts). The fast line must cross the slow line and exceed the minimum distance filter.
3. Retrieve momentum values on the same bars. For long entries the current momentum must be greater than both the configured threshold and the previous momentum value; for short entries the opposite is required.
4. If a new signal appears while the position is opposite, the strategy closes the existing position and immediately opens one in the new direction with the configured lot size.

## Parameter Reference
| Parameter | Description | Default |
|-----------|-------------|---------|
| `OrderVolume` | Base volume used for each market order. | `1` |
| `StopLossPips` | Stop-loss distance in pips (0 disables the stop). | `50` |
| `TakeProfitPips` | Take-profit distance in pips (0 disables the target). | `50` |
| `TrailingStopPips` | Trailing stop distance in pips (0 disables trailing). | `5` |
| `TrailingStepPips` | Minimum pip improvement required to move the trailing stop. | `5` |
| `MinDistancePips` | Minimum separation between MAs to validate the crossover. | `0` |
| `MomentumFilter` | Minimum momentum difference required to allow entries. | `0.1` |
| `FastPeriod` / `FastShift` | Fast MA length and horizontal shift (bars). | `13` / `1` |
| `SlowPeriod` / `SlowShift` | Slow MA length and horizontal shift (bars). | `34` / `3` |
| `MaMethod` | Moving average smoothing type (Simple, Exponential, Smoothed, Weighted). | `Exponential` |
| `AppliedPrice` | Candle price used for indicator calculations. | `Close` |
| `MomentumPeriod` | Momentum lookback length in bars. | `14` |
| `CandleType` | Data type of candles supplied to the strategy. | `TimeFrame(1m)` |

## Practical Notes
- Always ensure `Security.PriceStep` is configured for your instrument; otherwise pip-based risk management will fallback to raw price units.
- The trailing logic requires a positive `TrailingStepPips` when `TrailingStopPips` is enabled—mirroring the original MetaTrader validation.
- Because stop and take levels are evaluated on candle ranges, using higher-resolution candles provides a closer approximation to tick-based execution.
- Logging messages on entries and trailing adjustments are included to ease debugging and parameter tuning.

## Files
```
API/2938_Crossing_Moving_Average/
├── CS/CrossingMovingAverageStrategy.cs
├── README.md
├── README_cn.md
└── README_ru.md
```
