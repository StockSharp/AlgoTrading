# Bollinger Bands N Positions v2 Strategy

## Overview
This strategy replicates the "Bollinger Bands N positions v2" expert advisor by Vladimir Karputov. It operates on completed candles and looks for price breakouts relative to the Bollinger Bands envelope. The StockSharp port keeps the original pyramiding behaviour, risk controls, and trailing logic while adapting order management to the netting model of the platform.

## Trading Logic
- A Bollinger Bands indicator (period and deviation configurable) is calculated on the selected candle series.
- When the candle close finishes above the upper band, the strategy exits any active short exposure and opens an additional long position (up to the configured maximum number of stacked entries).
- When the candle close finishes below the lower band, the strategy exits any active long exposure and opens an additional short position (also limited by the maximum entries parameter).
- Position size is increased in fixed increments (the **Volume** parameter) when pyramiding into the same direction.
- The average entry price of the stacked position is tracked to manage stop loss, take profit, and trailing stop levels consistently.

## Risk Management
- Stop loss and take profit distances are entered in pips. They are converted into absolute price offsets by multiplying with the instrument price step. Instruments quoted with 3 or 5 decimal places automatically multiply the step by 10 to emulate MetaTrader's pip size adjustment.
- Trailing stop offset and trailing step are also configured in pips. The trailing mechanism updates the stop price only after the trade moves by `TrailingStop + TrailingStep` pips from the current average entry. Each update shifts the stop by the trailing offset while respecting the extra step buffer to avoid excessive modifications.
- Protective exit orders are simulated within the strategy: whenever a finished candle crosses the stop or target level, the entire position is closed using market orders.

## Parameters
| Parameter | Description |
|-----------|-------------|
| **Bollinger Period** | Lookback period for the Bollinger Bands moving average. |
| **Bollinger Deviation** | Standard deviation multiplier for the Bollinger envelope. |
| **Max Positions** | Maximum number of stacked entries allowed per direction. |
| **Volume** | Order volume for each individual entry. |
| **Stop Loss (pips)** | Stop loss distance in pips (0 disables the stop). |
| **Take Profit (pips)** | Take profit distance in pips (0 disables the target). |
| **Trailing Stop (pips)** | Trailing stop distance in pips (0 disables trailing). |
| **Trailing Step (pips)** | Additional profit in pips required before moving the trailing stop again. Must be positive when trailing is enabled. |
| **Candle Type** | Candle series processed by the strategy. |

## Implementation Notes
- The strategy uses high-level candle subscriptions with indicator binding, following the StockSharp guidelines.
- Only finished candles are processed to mirror the original "new bar" logic from MetaTrader.
- Because StockSharp operates in a netting mode, the conversion closes the opposite exposure before opening a new pyramid layer in the other direction.
- Trailing step must remain greater than zero whenever the trailing stop is active, matching the safety check of the original expert advisor.
- Python implementation is not included in this release.
