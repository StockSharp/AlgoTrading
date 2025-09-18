# Breakout Strategy

## Overview

The Breakout Strategy is a Donchian-channel breakout system converted from the original MetaTrader 5 expert advisor `BreakoutStrategy.mq5`. On each completed bar the strategy monitors the highest high and lowest low over a configurable lookback window and enters trades once price breaks through those boundaries. Open positions are protected by a trailing channel derived from a second Donchian calculation, mirroring the trailing logic used in the source expert.

## Trading logic

1. **Entry channel** – Highest and lowest prices over `EntryPeriod` bars are delayed by `EntryShift` bars to avoid using the current bar in the breakout calculation.
2. **Breakout detection** – A long breakout is triggered when the bar high touches the shifted upper band plus one price step. A short breakout is triggered when the bar low touches the shifted lower band minus one price step.
3. **Exit channel** – Highest and lowest prices over `ExitPeriod` bars are delayed by `ExitShift` bars. The optional middle line can tighten the trailing stop by selecting the maximum (for longs) or minimum (for shorts) between the outer and middle bands, replicating the "use middle line" option from the EA.
4. **Position management** – The strategy closes an existing long position when the bar low pierces the trailing level, and closes a short position when the bar high touches the short trailing level. Opposite signals flatten any existing exposure before entering in the new direction.
5. **Risk sizing** – Position size is derived from `RiskPerTrade`. The strategy obtains the portfolio equity, converts the stop distance into money using the instrument `PriceStep` and `StepPrice`, and requests the largest allowed volume that keeps the loss near the configured percentage. Volumes are aligned with the instrument `VolumeStep`, `VolumeMin`, and `VolumeMax`.

## Parameters

| Name | Description |
| --- | --- |
| `CandleType` | Data type describing the candle series used by the strategy. The default is 1-hour candles. |
| `EntryPeriod` | Lookback window for the breakout channel. |
| `EntryShift` | Number of completed bars used as an offset when evaluating the channel. `1` reproduces the original EA behaviour. |
| `ExitPeriod` | Lookback window for the trailing exit channel. |
| `ExitShift` | Offset in bars applied to the trailing channel. |
| `UseMiddleLine` | When enabled, the Donchian middle line participates in the trailing stop calculation, matching the MQL5 option. |
| `RiskPerTrade` | Fraction of portfolio equity risked per trade (e.g. `0.01` for 1%). |

## Notes

- All comments inside the C# implementation are written in English as required by the repository guidelines.
- The strategy uses StockSharp high-level API features: candle subscriptions, Donchian channels (`Highest`/`Lowest` indicators) and shift indicators to avoid manual buffers.
- No automated tests are provided for this conversion; please validate behaviour in your own environment before deploying to production.
