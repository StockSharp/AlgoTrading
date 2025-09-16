# Well Martin Strategy

## Overview

The **Well Martin** strategy is a mean reversion system that combines Bollinger Bands and the Average Directional Index (ADX). It enters long positions when price breaks below the lower Bollinger Band during low trend strength, and enters short positions when price breaks above the upper Bollinger Band under the same conditions. Positions are closed when price reaches the opposite band or hits the configured take profit or stop loss levels.

## Parameters

- **CandleType** – candle series used for calculations.
- **BollingerPeriod** – period for Bollinger Bands.
- **BollingerWidth** – standard deviation multiplier for Bollinger Bands.
- **AdxPeriod** – period for the ADX indicator.
- **AdxLevel** – ADX threshold; trades are taken only when the ADX value is below this level.
- **Volume** – trade volume for each entry.
- **TakeProfit** – profit target in price units.
- **StopLoss** – loss limit in price units.

## Logic

1. Subscribe to candle data and calculate Bollinger Bands and ADX.
2. When there is no open position:
   - **Buy** if the close price is below the lower band and ADX is below the threshold.
   - **Sell** if the close price is above the upper band and ADX is below the threshold.
3. Track the last executed trade side and allow entries only in the same direction or when no trades have been made.
4. When in a long position:
   - Exit if price touches the upper band, reaches the take profit, or hits the stop loss.
5. When in a short position:
   - Exit if price touches the lower band, reaches the take profit, or hits the stop loss.

## Notes

This implementation uses a fixed trade volume. The original MQL version increased volume after a losing trade; this behaviour can be added later if required.
