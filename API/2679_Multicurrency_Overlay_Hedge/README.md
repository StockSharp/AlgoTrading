# Multicurrency Overlay Hedge Strategy

Conversion of the MetaTrader 4 expert advisor **"Multicurrency hedge example EA (overlay hedge)"** to the StockSharp high-level API.

## Overview
- Works on a universe of forex symbols supplied by the user and monitors all unique pairs.
- Computes rolling Pearson correlation and ATR ratios to determine which symbols move together and how to size both legs.
- Builds synthetic price overlays to detect when the main instrument deviates from its correlated partner beyond a configurable threshold.
- Opens hedged blocks (buy/sell, buy/buy, sell/buy, sell/sell) depending on the correlation sign and overlay direction.
- Closes the entire block once a mutual take-profit target is reached either in points or portfolio currency.

## Workflow
1. Subscribe to finished candles for every security in the universe and store the latest high/low/close values.
2. Subscribe to Level1 quotes of each security to enforce spread filters before submitting hedges.
3. Once per day (default 01:00 server time) rebuild the list of tradable pairs:
   - Only keep pairs where the absolute correlation is above the configured threshold.
   - Calculate the ATR ratio to scale the volume of the primary leg.
4. For every finished candle check the overlay distance:
   - Positive correlation ⇒ buy main / sell sub when the deviation is below `-OverlayThreshold` points, sell main / buy sub when it is above `+OverlayThreshold` points.
   - Negative correlation ⇒ buy both legs below the negative threshold, sell both legs above the positive threshold.
5. Track open hedge blocks and close them when the aggregated profit reaches either of the take-profit conditions.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `Universe` | Collection of `Security` objects to scan. Needs at least two entries. | empty |
| `CandleType` | Candle data type used for calculations. | 1 minute time frame |
| `RangeLength` | Number of bars used to compute price envelopes. | 400 |
| `CorrelationLookback` | Bars used for Pearson correlation. | 500 |
| `AtrLookback` | Bars used for ATR ratio sizing. | 200 |
| `CorrelationThreshold` | Minimum absolute correlation to keep a pair (0–1). | 0.90 |
| `OverlayThreshold` | Overlay distance in points measured using the main instrument step. | 100 |
| `TakeProfitByPoints` / `TakeProfitPoints` | Enables and configures point-based mutual take profit. | true / 10 |
| `TakeProfitByCurrency` / `TakeProfitCurrency` | Enables and configures currency-based mutual take profit. | false / 10 |
| `MaxOpenPairs` | Maximum simultaneously open hedge blocks. | 10 |
| `BaseVolume` | Volume of the secondary leg (main leg volume = `BaseVolume * ATR ratio`). | 1 |
| `RecalculationHour` | Hour of the day when correlations are recalculated. | 1 |
| `MaxSpread` | Maximum allowed bid-ask spread per leg (in points). | 10 |

## Data requirements
- Historical and live candles for every security in `Universe` with the specified `CandleType`.
- Level1 quote updates for each security to validate spreads.
- Portfolio information for order registration.

## Usage notes
- The strategy does not auto-populate the universe; pass the desired forex symbols before starting.
- To mimic the MetaTrader sizing logic, keep `BaseVolume` equal to the lot size of the secondary leg. The main leg volume is automatically scaled by the ATR ratio.
- If spread data is unavailable the strategy will skip new entries until the first order book snapshot arrives.
- Closing logic estimates mutual profit by combining the signed move of each leg using the instrument price step and step price.

## Differences from the original EA
- Uses StockSharp subscriptions (`SubscribeCandles`, `SubscribeLevel1`) instead of timer-based polling.
- Take-profit logic is implemented with averaged price step information rather than raw trade profit/commission.
- Requires an explicit universe parameter, allowing the strategy to run on any subset of instruments supported by StockSharp.
- Order execution is performed through StockSharp market orders with per-hedge comments for traceability.

