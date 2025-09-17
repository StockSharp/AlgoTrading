# Sample Trailing Stop Strategy

## Overview

The **SampleTrailingStopStrategy** is a direct C# port of the MetaTrader expert advisor `SampleTrailingstop.mq4`. The strategy does not generate its own entries; instead, it continuously watches the current position and maintains protective stop-loss and take-profit orders. The logic mirrors the original EA by respecting broker-imposed stop and freeze levels while applying a trailing stop measured in price points.

Whenever a long position becomes profitable and the best bid travels far enough from the entry price, the strategy first moves the stop-loss just below the bid by the minimal allowed distance. Subsequent updates trail the stop behind the bid by the configured number of points plus broker buffers. Short positions are processed symmetrically, with the stop above the ask. Optional take-profit targets are recalculated on every trailing event.

## Data Flow

* Subscribes to Level1 updates to receive the best bid/ask quotes.
* Tracks the current position price through the base `Strategy` API.
* Re-registers protective stop and limit orders when new prices are calculated.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `TrailingStopPoints` | `200` | Distance between the market and the trailing stop measured in price points. This value is added to the broker buffers during trailing calculations. |
| `TakeProfitPoints` | `1000` | Optional take-profit distance in points. Set to `0` to disable take-profit management. |
| `StopLevelPoints` | `0` | Broker stop-level restriction expressed in points. It is added to the trailing distance to keep stop orders valid. |
| `FreezeLevelPoints` | `0` | Broker freeze-level restriction expressed in points. Trailing waits until the market moves beyond this buffer from the entry price. |

All distances are translated into price values with the instrument tick size to emulate the `_Point` behaviour from MetaTrader.

## Trailing Algorithm

1. **Position validation** – The strategy ignores trailing until a position exists and the best bid/ask is known.
2. **Profit check** – Trailing activates only when the position is profitable (`bid > entry` for longs, `ask < entry` for shorts) and the freeze buffer has been cleared.
3. **Initial stop placement** – If no trailing stop is active yet, the stop is moved to the minimal allowed distance from the market (bid minus buffers for longs, ask plus buffers for shorts) once the price runs at least the trailing distance away from the entry.
4. **Trailing updates** – While the position stays profitable, the stop is pushed deeper using the configured trailing distance plus broker buffers. Take-profit levels are recalculated on every update when enabled.
5. **Order maintenance** – Protective orders are automatically created, updated, or cancelled through high-level helper methods so the broker always sees the latest stop-loss and take-profit values.

## Usage Notes

* Start the strategy together with another component that opens positions, or use manual orders; this module only manages exits.
* Ensure that the instrument metadata contains proper price and volume steps. The strategy normalizes every generated price and amount to satisfy exchange constraints.
* When the position direction flips, any legacy protective orders are cancelled before trailing restarts for the new side.
