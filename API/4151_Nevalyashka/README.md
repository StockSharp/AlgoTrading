# Nevalyashka Strategy

## Overview
The Nevalyashka strategy is a C# port of the original MetaTrader 4 expert advisor `Nevalyashka.mq4`. The EA repeatedly flips its trading direction: it opens a single market order, waits until the position is closed by a stop-loss, take-profit or manual action, and instantly re-enters in the opposite direction with the same volume. The StockSharp implementation reproduces this behaviour while exposing all critical settings as strategy parameters.

## Trading Logic
1. **Initialization**
   - When the strategy starts it calculates the pip size from the instrument's `PriceStep`. For 3- and 5-digit Forex symbols the step is multiplied by 10 to match the MetaTrader point definition.
   - `StartProtection` is configured with stop-loss and take-profit distances converted from pips into price points. Protective orders are attached to every subsequent position.
   - An initial market order is sent in the direction defined by `InitialDirection` (default: short). The requested volume is rounded to the nearest valid lot using the security's `VolumeStep`, `MinVolume`, and `MaxVolume` values.

2. **Position tracking**
   - `OnPositionChanged` captures every change in net exposure. When a new position opens the strategy stores the filled volume and remembers the trade side.
   - Once the position fully returns to flat the strategy immediately issues a new market order in the opposite direction, reusing the previously stored lot size.

3. **Failure handling**
   - If the broker rejects an order registration the pending direction flag is cleared, allowing the platform operator to retry manually or adjust the parameters without stale internal state.

The resulting workflow mirrors the "roly-poly" idea of the original script: the bot is always in the market, alternating between long and short positions with fixed exits.

## Parameters
| Name | Description | Default | Notes |
| --- | --- | --- | --- |
| `StopLossPips` | Distance of the protective stop in pips. | `50` | Converted to price points via the pip size calculation; set to `0` to disable the stop. |
| `TakeProfitPips` | Distance of the protective take-profit in pips. | `50` | Converted in the same way as the stop-loss; set to `0` to disable the take-profit. |
| `Volume` | Lot size used for the very first trade. | `1` | After the first fill the strategy reuses the actually executed volume for all future entries. |
| `InitialDirection` | Side of the initial market order. | `Sell` | Choose between `Buy` and `Sell` to match the desired starting bias. |

## Implementation Notes
- No candle or indicator subscriptions are required; the strategy reacts solely to position events and order confirmations.
- `IsFormedAndOnlineAndAllowTrading()` is consulted before every entry to ensure the connector is ready to trade.
- Volume rounding uses `MidpointRounding.AwayFromZero` so that fractional lots always snap to a tradable level instead of zero.
- The pip conversion logic relies on instrument metadata rather than hard-coded assumptions, which makes the port work across FX, CFD, or futures symbols with different price formats.

## Differences vs. the MQL Version
- The StockSharp variant exposes the starting direction as a parameter instead of forcing the initial short from the MT4 script.
- Stop-loss and take-profit orders are managed through `StartProtection`, which produces native protective orders compatible with any StockSharp connector.
- Order rejections clear the internal pending state to avoid repeated submission of invalid requests.

These adjustments keep the spirit of the original advisor while integrating seamlessly with the StockSharp high-level API.
