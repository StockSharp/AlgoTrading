# Nevalyashka Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A direct StockSharp port of the MetaTrader expert "Nevalyashka". The strategy always alternates between long and short trades: it starts with a market sell order, waits for the position to close by stop loss or take profit, and then immediately opens a market order in the opposite direction. Protective orders are recreated for every entry using the same pip-based offsets as in the original code.

## Strategy Logic

1. **Initialization**
   - Detects the instrument price step and decimals to derive a pip size identical to the MQL version (3/5 digit pairs are multiplied by 10).
   - Multiplies the exchange `MinVolume` by the `LotMultiplier` parameter to obtain the order size and rounds it to the volume step if necessary.
2. **Quote Handling**
   - Subscribes to order book updates to capture the latest best bid/ask prices, mirroring the `RefreshRates()` call from the expert.
3. **Order Flow**
   - Places an initial sell market order once best bid/ask quotes are available.
   - After a position is closed, flips the side (buy after sell, sell after buy) and issues a new market order with the same volume.
   - For every filled entry the strategy places separate stop-loss and take-profit orders using the pip distance parameters.

## Risk Management

- **Stop Loss**: Optional. When `StopLossPips` is greater than zero, the strategy submits a protective stop order (`SellStop` for long positions, `BuyStop` for short positions) at `entry ± StopLossPips * pip`.
- **Take Profit**: Optional. When `TakeProfitPips` is greater than zero, the strategy submits a protective limit order (`SellLimit` for long positions, `BuyLimit` for short positions) at `entry ± TakeProfitPips * pip`.
- Both protective orders are cancelled whenever the position is flat to avoid dangling orders before the next flip.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `LotMultiplier` | Multiplier applied to the instrument minimum volume. The result is rounded to the exchange volume step. | `1` |
| `StopLossPips` | Stop-loss distance in pips. Set to `0` to disable the stop. | `50` |
| `TakeProfitPips` | Take-profit distance in pips. Set to `0` to disable the target. | `50` |

## Operational Notes

- The approach continuously alternates exposure and therefore suits mean-reverting markets where a completed move is likely to reverse.
- Works with any symbol that provides top-of-book quotes; pip calculations adapt automatically based on price precision.
- Slippage handling is delegated to the exchange—orders are sent at market without additional checks just like in the original expert.
- The strategy does not include trading-hour filters, news filters or trailing stops. Such logic can be added by extending `TryOpenNextPosition` or `RegisterProtectionOrders`.
