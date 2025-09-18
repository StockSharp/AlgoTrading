# Five MA Multi-Timeframe Strategy

## Overview
The **Five MA Multi-Timeframe Strategy** replicates the original MT4 "5matf" expert advisor using StockSharp's high-level API. The strategy analyzes five simple moving averages across three timeframes (primary, higher, and slowest) and combines the slope of each average with the Accelerator Oscillator to produce graded entry signals. When enough bullish or bearish evidence is present on all timeframes, the strategy opens or closes positions accordingly.

## Indicators and Data
- **Simple Moving Averages (SMA)**: Periods 5, 8, 13, 21, and 34 on all three timeframes.
- **Accelerator Oscillator (AC)**: Applied on the primary and tertiary timeframes to assess momentum acceleration.
- **Timeframes**: Default set to 15 minutes (signal), 60 minutes (confirmation), and 240 minutes (trend filter). All timeframes can be adjusted via parameters.

## Signal Logic
1. Each SMA compares its current value to the previous candle to determine an upward or downward slope.
2. Accelerator Oscillator checks for bullish or bearish sequences using the latest four values.
3. Slope counts and oscillator contributions are aggregated into percentage scores for every timeframe.
4. When all three timeframes have bullish scores above 50%, a **BUY** signal is generated. Scores above 75% strengthen the signal.
5. The same thresholds applied in the opposite direction generate **SELL** signals.
6. Positions are closed when an opposite signal exceeds the configured close level. New trades only open when no position is active, mirroring the original expert advisor behavior.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | 15-minute candles | Primary timeframe used for trading signals. |
| `HigherTimeframe1` | 60-minute candles | First higher timeframe for confirmation. |
| `HigherTimeframe2` | 240-minute candles | Second higher timeframe for slow trend filter. |
| `FirstPeriod` – `FifthPeriod` | 5, 8, 13, 21, 34 | SMA lengths applied to each timeframe. |
| `OpenLevel` | 0 | Minimum signal grade required to open a new position. |
| `CloseLevel` | 1 | Opposite signal grade required to close an existing position. |

All parameters can be optimized or fine-tuned within StockSharp's strategy UI.

## Usage Notes
- The strategy uses market orders and does not issue simultaneous reversals; it always waits for a flat position before opening in the opposite direction.
- Enable history data feeds for all selected timeframes to ensure synchronized calculations.
- Consider tuning the SMA lengths or oscillator usage when applying the strategy to different markets or volatility regimes.

## Conversion Notes
This implementation retains the core behavior of the MT4 "5matf" expert advisor while leveraging StockSharp's subscription and indicator binding system. The accelerator logic requires four completed candles before signals become active, just like the original script.
