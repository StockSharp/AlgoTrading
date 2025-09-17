# BuySellStopButtons Strategy

## Overview
- Recreates the MetaTrader 4 "Buy Sell Stop Buttons" expert advisor inside StockSharp.
- Provides three manual parameters (`BuyRequest`, `SellRequest`, `CloseRequest`) that emulate the chart buttons.
- Implements the same money management helpers: fixed money take-profit, percent take-profit, equity trailing lock, break-even and pip trailing stops.
- Uses a one minute candle subscription purely as a heartbeat to evaluate the management rules on finished bars.

## Parameters
| Name | Description |
| --- | --- |
| `OrderLots` | Base lot size used when a manual entry is requested. Mirrors the `Lots` extern input (`0.01` by default). |
| `NumberOfTrades` | Number of tickets dispatched per request. The C# port nets the volume into a single market order. |
| `UseTakeProfitInMoney` / `TakeProfitInMoney` | Enable and configure the direct money target that closes all trades when reached. |
| `UseTakeProfitPercent` / `TakeProfitPercent` | Enable and configure the equity percentage target. The strategy uses `Portfolio.CurrentValue` to approximate account balance. |
| `EnableTrailing`, `TrailingProfitMoney`, `TrailingLossMoney` | Configure the equity trailing block: once profit exceeds `TrailingProfitMoney`, the peak is tracked and all trades close if profit retraces by `TrailingLossMoney`. |
| `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Move the stop to break-even plus offset after the position earns the configured pip distance. |
| `StopLossPips`, `TakeProfitPips`, `TrailingStopPips` | Ticket management settings converted to pip distances in StockSharp. |
| `CandleType` | Candle series that drives the heartbeat (default one-minute candles). |
| `BuyRequest`, `SellRequest`, `CloseRequest` | Manual commands that replace the original chart buttons. The flags reset automatically after the action succeeds. |

## Trading Logic
1. `OnStarted` subscribes to the configured candle series, sets the base `Volume` and enables the built-in position protection.
2. Each finished candle triggers the following workflow:
   - Manual commands are evaluated: buy and sell send a market order with `OrderLots * NumberOfTrades` volume, optionally offsetting an opposite position; close requests flatten the strategy.
   - Money targets are checked in order: fixed amount, percent of equity, then the trailing equity lock.
   - Break-even and pip trailing stops adjust internal stop levels based on the average entry price.
   - Static stop-loss/take-profit distances are enforced.
   - Optional Bollinger-band exit closes longs touching the upper band or shorts touching the lower band (20 period, width 2).
3. Open profit is calculated with `Security.PriceStep`/`Security.StepPrice` when available; otherwise a price-difference fallback is used.

## Differences from the MQL Version
- MetaTrader allowed hedged positions; StockSharp nets exposure, so buy/sell requests first neutralize opposite positions.
- Monthly MACD-based exits (`Close_BUY`/`Close_SELL`) are not present because they were never called in the original script.
- Volume auto-scaling via `MaximumRisk`/`DecreaseFactor` is replaced by the explicit `OrderLots` parameter. The MQL helper relied on account history that is not available in this port.
- Stop adjustments are driven by finished candles instead of raw ticks, matching repository guidelines.
- Indicator values are processed through `Bind`, avoiding direct collections or manual history buffers.

## Usage Notes
- Keep `BuyRequest`, `SellRequest` and `CloseRequest` under the "Manual controls" group disabled when running optimizations.
- The trailing equity lock and money take-profit logic require `Security.StepPrice` to translate profit into currency. When it is unavailable the fallback uses pure price differences.
- Break-even and trailing stops use the instrument pip size inferred from `MinPriceStep`/`PriceStep` and decimal digits.
- There is no Python translation, as requested.

## Testing
- No automated tests were modified; the strategy integrates with the existing solution structure and relies on manual parameter toggles for verification.
