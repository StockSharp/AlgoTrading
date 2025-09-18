# Monday Typical Breakout Strategy

## Overview

The **Monday Typical Breakout Strategy** is a C# port of the MetaTrader expert advisor `yi1ywioff50qr6` (repository ID 8187). The original robot monitors hourly candles and opens a long position every Monday when the new session opens above the previous bar's typical price `(high + low + close) / 3`. This implementation reproduces the entry logic within the StockSharp high-level strategy framework and adds detailed configuration parameters for position sizing and risk control.

## Trading Logic

1. The strategy subscribes to the configured candle series (hourly by default).
2. At the start of each finished candle it checks whether:
   - The candle belongs to Monday.
   - The candle's opening hour matches the configured *Open Hour* parameter (default 09:00).
   - No open position or active orders exist.
   - The candle's open price is greater than the typical price of the previous bar.
3. If all conditions are satisfied the strategy sends a market buy order with a volume computed by the money management block. Protective stop-loss and take-profit distances are applied through `StartProtection`.

The strategy never opens short positions and will only place one trade per qualifying Monday candle.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `FixedVolume` | Lot size for entries. Set to `0` to enable the equity scaling table. | `0.1` |
| `OpenHour` | Trading session hour (0-23) when Monday signals are evaluated. | `9` |
| `StopLossPoints` | Distance in price points for the protective stop. `0` disables the stop. | `50` |
| `TakeProfitPoints` | Distance in price points for the profit target. `0` disables the target. | `20` |
| `InitialEquity` | Equity threshold that activates equity-based lot scaling. | `600` |
| `EquityStep` | Equity increment required to increase the trade size. | `300` |
| `InitialStepVolume` | Lot size used when equity is at least `InitialEquity`. | `0.4` |
| `VolumeStep` | Additional lot size added for each `EquityStep` reached. | `0.2` |
| `CandleType` | Candle data type driving the strategy (hourly by default). | `1 hour time-frame` |

## Money Management

- When `FixedVolume` is greater than zero the strategy always uses the fixed lot size.
- When `FixedVolume` equals zero the strategy inspects the portfolio equity:
  - If equity is below `InitialEquity`, the instrument minimum lot is used.
  - Otherwise the volume starts at `InitialStepVolume` and increases by `VolumeStep` for each `EquityStep` of additional equity.
  - The final volume is aligned to the instrument's minimum and step constraints.

## Risk Management

`StartProtection` is activated during `OnStarted`. The stop-loss and take-profit distances are automatically translated from points to price offsets using the instrument `PriceStep`. Set either distance to zero to disable that component.

## Usage Notes

- The original EA is designed for hourly candles. Lower timeframes may produce multiple Monday candles with the same hour. The port retains the single-entry-per-candle behaviour and will still ignore additional signals while a position is open.
- Ensure the portfolio information (`Portfolio.CurrentValue`) is available if the dynamic sizing block is enabled.
- The strategy requires level-1 data to execute market orders and the corresponding candle subscription for the configured `CandleType`.

## Conversion Notes

- MQL magic-number filtering is replaced with StockSharp's position and order checks (`Position` and `ActiveOrders`).
- Time comparisons leverage `DateTimeOffset` from the candle open time with `.ToLocalTime()` to stay aligned with chart time.
- Protective orders are handled by the high-level `StartProtection` helper instead of manual order placement.

