# Exp Hans Indicator Cloud System Tm Plus Strategy

## Overview
Exp Hans Indicator Cloud System Tm Plus is a session-based breakout strategy that reproduces the behaviour of the original MQL5 expert advisor. The algorithm monitors the colour states produced by the Hans indicator on a configurable timeframe. It opens a new position after a bullish (colours 0/1) or bearish (colours 3/4) breakout finishes and price returns inside the channel. The implementation keeps all trading decisions on closed candles, uses pip-based risk limits, and mirrors the time-based liquidation rule from the MQL version.

The strategy operates on a single instrument/candle feed pair obtained from `GetWorkingSecurities()`. All order sizes are derived from the strategy `Volume` property and the money-management fraction exposed by the parameters.

## Indicator logic
1. Candle timestamps are converted from the broker time (`LocalTimeZone`) to the destination time zone (`DestinationTimeZone`). By default the script works with GMT+4, which matches the reference implementation.
2. Two London-session ranges are collected every trading day:
   - **Range 1**: 04:00–08:00 destination time. The high/low of this period become the initial breakout channel.
   - **Range 2**: 08:00–12:00 destination time. Once completed it replaces the first range for the rest of the day.
3. Each range is extended by `PipsForEntry` pips on both sides. A pip equals the instrument `PriceStep`, multiplied by 10 when the security has 3 or 5 decimal places (MetaTrader-style fractional pips).
4. Candle colours are derived exactly as in the indicator:
   - Close above the upper band → colour `0` (bullish close) or `1` (bearish close).
   - Close below the lower band → colour `4` (bearish close) or `3` (bullish close).
   - Close inside the channel → neutral colour `2`.

## Trading rules
- **Entry**: When the previous closed candle had a bullish colour (0/1) and the most recent one is not bullish, the strategy opens a long position (if enabled). Symmetrically, a previous bearish colour (3/4) followed by a neutral/contrary colour triggers a short entry.
- **Exit**:
  - Directional exit when the previous colour turns against the current position (0/1 for shorts, 3/4 for longs).
  - Optional time-based exit once the holding period exceeds `HoldingMinutes`.
  - Optional stop-loss / take-profit levels expressed in points (`StopLossPoints`, `TakeProfitPoints`). Levels are skipped if the security does not expose a positive `PriceStep`.
- Exits are processed before new entries, so a position is flattened before a reversal order is sent.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `MoneyManagement` | Fraction of the strategy `Volume` used per trade. Values ≤ 0 fall back to the full volume. | `0.1` |
| `MoneyMode` | Placeholder for the original money-management modes. Currently only `Lot` is applied. | `Lot` |
| `StopLossPoints` / `TakeProfitPoints` | Protective stop and profit target expressed in points (pips). Set to `0` to disable. | `1000` / `2000` |
| `DeviationPoints` | Maximum acceptable execution deviation in points. Present for compatibility; not enforced by the StockSharp order layer. | `10` |
| `AllowBuyEntries` / `AllowSellEntries` | Enables long/short entries. | `true` |
| `AllowBuyExits` / `AllowSellExits` | Enables automated exits for long/short positions. | `true` |
| `UseTimeExit` | Toggles the time-based liquidation filter. | `true` |
| `HoldingMinutes` | Maximum holding time for any position in minutes. | `1500` |
| `PipsForEntry` | Pip offset added above/below the breakout ranges. | `100` |
| `SignalBar` | Closed-candle offset used for signals. Use values ≥ 1 to stay aligned with the MT5 logic. | `1` |
| `LocalTimeZone` | Broker/server time zone (hours from UTC). | `0` |
| `DestinationTimeZone` | Target time zone used for session boundaries. | `4` |
| `CandleType` | Time frame used for Hans calculations. | `30m` candles |

## Money management and execution
- Order size = `Volume * MoneyManagement`, normalised to the instrument `VolumeStep`. If the computed value is non-positive the logic defaults to one volume step.
- When a reversal signal appears the strategy sends a single market order equal to the new volume plus any opposite open quantity. This reproduces the behaviour of `BuyPositionOpen`/`SellPositionOpen` from the MQL helper.
- Stop-loss and take-profit levels are recalculated on every entry and cleared when a position is closed or reversed.

## Usage guidelines
1. Attach the strategy to a security that publishes valid `PriceStep`, `Decimals`, and `VolumeStep` metadata.
2. Set the desired `Volume` on the strategy before starting it. The money-management fraction will be applied on top.
3. Choose a candle type equal to the one used in MetaTrader (M30 by default). All calculations rely on completed candles.
4. Align the time zones if your market data source differs from the default GMT+4 destination time used by the Hans indicator.
5. Monitor the logs for messages about missing pip size; the risk levels will be skipped when no `PriceStep` is available.

## Implementation notes
- Colour detection is performed purely on finished candles via the high-level `SubscribeCandles` API, avoiding manual indicator buffers.
- The breakout levels are recomputed once per candle and cached in memory; no historical collections are created.
- `DeviationPoints` is retained for configuration completeness but cannot be enforced with plain market orders in StockSharp.
- The strategy resets its internal state on `OnReseted()` to support repeated backtests without stale session data.

## Limitations
- The current implementation only supports `SignalBar ≥ 1`, matching the original EA behaviour on new-bar events. Using `0` would require tick-level access which is not present in the high-level port.
- Money management modes other than `Lot` are not implemented. Extend `GetOrderVolume()` if your workflow depends on balance-based sizing.
- Without a valid `PriceStep` value, pip-based distances (stop, take-profit, Hans offsets) cannot be calculated and will be ignored.

