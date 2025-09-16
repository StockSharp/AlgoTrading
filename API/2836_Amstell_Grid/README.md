# Amstell Grid Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Amstell Grid Strategy is a C# port of the MetaTrader 5 expert advisor `exp_Amstell.mq5`. It creates a symmetric buy/sell grid and applies a virtual take profit to individual entries. The conversion follows the StockSharp high-level API guidelines and replaces tick handling with candle processing while keeping the original idea intact.

## How It Works

1. **Initialization**
   - The strategy subscribes to the configured candle type and starts position protection.
   - An adjusted pip size is calculated from the security's `PriceStep` and decimal precision. Five-digit and three-digit symbols automatically receive a 10x multiplier, mirroring the MT5 implementation.

2. **First Trade**
   - When both the last recorded buy and sell prices are empty (initial launch), a market buy order is sent immediately. This bootstraps the grid exactly like the original expert advisor.

3. **Grid Expansion**
   - A new **buy** is issued whenever the current close price is at least `StepPips` below the last recorded buy price.
   - A new **sell** is issued whenever the price is at least `StepPips` above the last recorded sell price.
   - The strategy internally tracks separate long and short stacks so that alternating orders can coexist even on a netting account. Opposite orders first reduce the other stack before adding new exposure, reproducing the hedging behavior of the MT5 version.

4. **Virtual Take Profit**
   - Every open long is monitored independently. When price advances by `TakeProfitPips`, a market sell is sent for that position's volume only.
   - Every open short is treated similarly in the opposite direction. The take profit is "virtual" because positions are closed programmatically without using broker-side TP orders.
   - After a direction has been fully closed while the opposite side still exists, the corresponding last-deal price is cleared so that the next order in that direction can fire immediately, just as in the original code.

5. **State Tracking**
   - The `OnOwnTradeReceived` handler rebuilds the long/short stacks from executed trades, allowing partial fills and reversals to be handled gracefully.
   - Last buy/sell prices remain cached when both sides are flat so that the grid waits for the required step before re-entering after a full reset.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Volume` | `0.1` | Order size used for every market order in both directions. |
| `TakeProfitPips` | `50` | Distance in pips that must be gained before an individual position is closed. |
| `StepPips` | `15` | Gap in pips between consecutive grid orders of the same direction. |
| `CandleType` | `1 Minute` | Candle data source used to approximate tick-based logic. |

All pip-based settings respect the security's price step and precision. For example, on EURUSD (5 digits) `StepPips = 15` corresponds to 0.0015.

## Practical Notes

- The strategy uses candle close prices to emulate the tick-level comparisons found in the MT5 code. For high-frequency operation, decrease the timeframe.
- No stop-loss exists by default. As with any grid approach, runaway trends can accumulate large exposure. Use conservative volumes and consider session-based supervision.
- Because take profits are handled virtually, closed trades are immediately reflected in the strategy's PnL without placing visible TP orders at the broker.
- The implementation leaves cached last prices untouched after both sides flatten. This preserves the original behavior where the grid waits for price displacement before restarting.

## Files

- `CS/AmstellGridStrategy.cs` – StockSharp strategy implementation with extensive inline comments.
- `README.md`, `README_ru.md`, `README_cn.md` – Full documentation in English, Russian, and Chinese.

This port is ready for further customization (e.g., money management, risk limits) directly within the StockSharp ecosystem.
