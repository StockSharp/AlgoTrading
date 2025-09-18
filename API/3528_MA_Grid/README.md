# MA Grid Strategy

## Overview

This strategy is a C# port of the MetaTrader 5 expert advisor **MAGrid.mq5**. It maintains a hedged grid of buy and sell positions around an exponential moving average (EMA). The idea is to keep the grid balanced around the EMA anchor. When price crosses predefined distance steps above or below the EMA, the strategy closes one position from the opposing side of the grid and opens a new position in the direction of the breakout. This constantly re-centers the basket around the moving average.

## Original Source

- **MQL repository folder:** `MQL/38303`
- **Original file:** `MAGrid.mq5`
- **Platform:** MetaTrader 5 (hedging mode)

## Trading Logic

1. **EMA Anchor**
   - The EMA period is configurable (default 48).
   - The EMA is calculated on the selected candle series.
   - Grid levels are calculated as multiples of the `Distance` parameter above and below the EMA.

2. **Grid Initialization**
   - The effective grid size is forced to be even to mirror both sides around the EMA.
   - The current grid index is determined by comparing the last closing price to the EMA-based levels.
   - A symmetric basket of buy and sell market orders is opened so that half of the positions sit below the EMA and the other half above it.

3. **Grid Maintenance**
   - When the price closes above the next upper grid level, the strategy:
     - Increments the grid index.
     - Closes one long order if any exposure is left.
     - Opens a new short order to extend the upper half of the grid.
   - When the price closes below the next lower grid level, the strategy:
     - Decrements the grid index.
     - Closes one short order if any exposure is left.
     - Opens a new long order to rebuild the lower half of the grid.
   - If one side of the grid runs out of exposure, the corresponding trigger is disabled until new orders are opened.

4. **Order Handling**
   - Orders are tracked through a simple internal map to distinguish between opening and closing fills.
   - The strategy stores separate exposure counters for the long and short baskets. This mirrors the hedging behavior of the MQL version while using the net-position model of StockSharp.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `MaPeriod` | 48 | EMA period used for the anchor level. |
| `GridAmount` | 6 | Number of grid steps; automatically rounded up to an even value. |
| `Distance` | 0.005 | Relative spacing between grid levels (e.g., 0.005 = 0.5%). |
| `OrderVolume` | 0.1 | Volume submitted with each market order. |
| `CandleType` | Daily time frame | Candle series used to calculate the EMA and evaluate signals. |

## Risk Management

- The strategy does not implement stop-loss or take-profit rules; risk is controlled via the number of grid steps and the order volume.
- Because the grid keeps both long and short exposure, the portfolio value can remain relatively stable, but margin usage grows with grid size and distance.
- Consider using portfolio risk controls (max drawdown, capital usage) at the strategy or portfolio level.

## Conversion Notes

- The C# implementation reproduces the hedged logic by separately tracking long and short exposure.
- The account-dependent volume calculation from MQL has been replaced with a configurable `OrderVolume` parameter for clarity.
- Candle subscriptions rely on the StockSharp high-level API using `SubscribeCandles().Bind(...)` in accordance with the project guidelines.
