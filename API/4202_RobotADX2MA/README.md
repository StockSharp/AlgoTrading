# Robot ADX + 2 MA Strategy

## Overview
The Robot ADX + 2 MA strategy is a StockSharp port of the MetaTrader expert `Robot_ADX+2MA`. The system combines a fast and a slow
exponential moving average with the +DI/-DI components of the Average Directional Index (ADX). Orders are only opened when the
previous candle shows a wide enough EMA separation and the current candle confirms momentum through the directional index. The
conversion keeps the original behaviour of opening at most one market position at a time and delegating exits to stop-loss and
take-profit protections.

## Trading logic
1. Subscribe to the primary candle series configured through `CandleType` and process finished candles only.
2. Feed two exponential moving averages (periods 5 and 12) with the candle close prices. Their values from the previous candle
   emulate the `shift = 1` lookback used in MetaTrader.
3. Feed an `AverageDirectionalIndex` indicator (period 6) with the same candles. Store both the current and the previous +DI/-DI
   readings to replicate the EA filters.
4. Compute the absolute EMA distance from the previous candle and compare it to `DifferenceThreshold` converted from points into
   price units (`Point` in MetaTrader equals `Security.PriceStep` in StockSharp).
5. **Bullish entry**: allowed only if no position is open and the following conditions are met:
   - Previous fast EMA is below the previous slow EMA.
   - Previous +DI is below 5, current +DI is above 10, and +DI is stronger than -DI.
   - EMA distance is above the configured threshold.
6. **Bearish entry**: symmetric to the long rules, requiring the previous fast EMA above the slow EMA, the -DI filters to be
   satisfied, and -DI to dominate +DI.
7. When a trade is opened, rely on the risk module started by `StartProtection` to exit via take profit or stop loss. No manual
   exit rules are added, matching the original expert.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-minute timeframe | Primary candle series processed by the strategy. |
| `TakeProfitPoints` | `int` | `4700` | Distance of the take-profit target expressed in price steps. Set to zero to disable. |
| `StopLossPoints` | `int` | `2400` | Distance of the stop-loss target in price steps. Set to zero to disable. |
| `TradeVolume` | `decimal` | `0.1` | Net volume used for every market order. |
| `DifferenceThreshold` | `int` | `10` | Minimum EMA distance (in price steps) required before a signal is accepted. |

## Risk management
- The StockSharp version calls `StartProtection` with `UnitTypes.Step`, so the configured stop-loss and take-profit distances are
  converted to the broker's price step automatically.
- Protective orders are generated as market exits (`useMarketOrders = true`), replicating the immediate close behaviour of the
  MQL helper function.

## Implementation details
- Indicator bindings use the high-level `SubscribeCandles(...).Bind(...).BindEx(...)` API so no manual data loops are required.
- EMA values from the previous candle are cached to reproduce the `iMA(..., shift = 1)` calls in the original EA.
- ADX data is consumed through `AverageDirectionalIndexValue`, giving direct access to the +DI and -DI components without calling
  forbidden `GetValue` helpers.
- A per-candle guard (`_lastProcessedTime`) ensures signals are evaluated only once even though both EMA and ADX bindings trigger
  callbacks for the same candle.

## Differences from the MetaTrader expert
- The redundant direct `OrderSend` call present in the sell branch of the MQL code is removed; both directions use a single
  `BuyMarket`/`SellMarket` helper.
- MetaTrader checks free margin before sending orders. The StockSharp port delegates risk controls to the hosting environment and
  assumes sufficient balance.
- Protective logic is implemented via StockSharp's risk manager instead of custom loops that repeatedly call `OrderSend`.

## Usage tips
- Adjust `TradeVolume` to respect the lot step of the selected security before starting live trading.
- If the market uses a different price scale, tweak `DifferenceThreshold` together with the stop/target distances so that the EMA
  separation is comparable to the MetaTrader configuration.
- The default timeframe is one minute, but the `CandleType` parameter allows switching to any other series supported by the data
  source.

## Indicators
- `ExponentialMovingAverage(5)` calculated on close prices.
- `ExponentialMovingAverage(12)` calculated on close prices.
- `AverageDirectionalIndex(6)` providing +DI/-DI and ADX strength filters.

