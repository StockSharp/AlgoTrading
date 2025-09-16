# ScalpWiz Bollinger Strategy

## Overview

The **ScalpWiz Bollinger Strategy** is a counter-trend system that uses Bollinger Bands to detect stretched prices. When the close price moves far above the upper band or below the lower band, the strategy opens a position in the opposite direction expecting a reversion.

Four distance levels are checked. Each level corresponds to a different signal strength and multiplies the trade volume. Position size is also scaled by a risk percentage of the current portfolio value.

## Parameters

- `BandsPeriod` – number of candles used to calculate Bollinger Bands.
- `BandsDeviation` – standard deviation multiplier for the bands.
- `Level1Pips` … `Level4Pips` – distance from the band in pips that triggers a level 1–4 signal.
- `StrengthLevel1Multiplier` … `StrengthLevel4Multiplier` – volume multipliers for each level.
- `RiskPercent` – percentage of portfolio value risked per signal.
- `CandleType` – candle timeframe used for calculations.

## Trading Logic

1. Subscribe to candles of the selected timeframe and compute Bollinger Bands.
2. On every finished candle:
   - If the close is above the upper band by a configured level distance, open a short position.
   - If the close is below the lower band by a configured level distance, open a long position.
3. Volume is calculated from the risk percentage and signal strength multiplier.

The strategy was inspired by the original MQL `mcb.scalpwiz.9001.mq4` script.

