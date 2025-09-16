# Rich Kohonen Map Strategy

## Overview
The Rich Kohonen Map Strategy is a conversion of the MetaTrader 4 expert advisor "Rich.mq4". The original system builds a self-organizing map (Kohonen network) over feature vectors derived from Tom DeMark pivot calculations and classifies the next bar as a buy, sell or hold opportunity. The StockSharp port preserves the learning approach while integrating with the high-level strategy API, operating exclusively on completed candles and market orders.

## Market data
- **Instrument** – configured through the linked `Security` in the host application.
- **Candle type** – parameter `CandleType` (default: 1-hour time frame). The strategy requires at least seven finished candles before producing signals so that both current and previous feature vectors can be assembled.

## Trading logic
1. Maintain a rolling window of the latest seven completed candles.
2. Build two seven-element vectors on every finished candle:
   - The **current vector** uses the most recent open together with Tom DeMark pivot projections calculated from the previous five candles.
   - The **previous vector** shifts the window by one bar and represents the bar that just closed. This vector is used for training.
3. Compare the current vector with three Kohonen maps (buy, sell, hold) and record the Euclidean distance to each best-matching unit.
4. Select the action with the smallest distance and set the target position:
   - Buy → long exposure equal to the calculated volume.
   - Sell → short exposure of the same magnitude.
   - Hold → no position.
   The strategy sends market orders for the difference between the current and target position so that the final exposure matches the decision.
5. Compute the open-to-open move (in pips) between the latest two candles and train the map:
   - Positive move within `[MinPips, MaxPips]` → add the previous vector to the buy map.
   - Negative move within `[-MaxPips, -MinPips]` → add the previous vector to the sell map.
   - Otherwise → store the vector in the hold map.
6. Position size is determined dynamically from the portfolio balance: `floor(balance / 50) / 10`. If this produces zero, the `Lots` fallback parameter is used instead.

## Parameters
- `MinPips` – lower bound (in pips) for considering a positive open-to-open move as a buy training example.
- `MaxPips` – upper bound (in pips) for buy/sell training samples.
- `TakeProfit`, `StopLoss` – preserved from the MQL expert for documentation purposes. The high-level implementation closes or reverses positions via market orders rather than by attaching stops.
- `Lots` – fallback volume applied when the balance-based formula yields zero.
- `Slippage` – reserved for manual order tuning (not used directly by the high-level API helpers).
- `MapPath` – binary file path used to persist the three Kohonen maps between runs.
- `EAName` – optional comment stored for reference.
- `CandleType` – candle subscription used for feature extraction.

## Persistent map storage
The strategy stores the trained map in a binary file defined by `MapPath` (default `rl.bin` inside the working directory). The file contains the buy, sell and hold matrices sequentially. On startup the matrices are loaded, and the strategy counts the non-empty rows to resume training from the previous state. Missing files are ignored, which causes the maps to start from zero-filled memory.

## Differences from the original MQL expert
- Orders are issued through StockSharp helpers (`BuyMarket` / `SellMarket`) and target the final desired exposure instead of forcing a full close plus reopen on each bar. This keeps the effective behaviour while reducing duplicate transactions in the managed environment.
- Stop-loss and take-profit levels remain as parameters for documentation but are not registered as separate orders. Position exits occur when the classifier selects the opposite side or the hold action.
- File handling uses .NET I/O helpers; the map format remains compatible (double precision values ordered identically).

## Usage notes
- Ensure the selected security exposes a valid `PriceStep` so pip differences are computed correctly. If the step is missing or zero, the strategy falls back to a unit step.
- The Kohonen maps can grow large (up to 10000 buy/sell entries and 25000 hold entries). Keep the default path on a storage device with sufficient capacity (~2.5 MB when full).
- Because the algorithm trains continuously, running the strategy on historical data before live deployment helps populate the map with representative samples.
