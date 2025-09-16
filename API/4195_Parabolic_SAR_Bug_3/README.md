# Parabolic SAR Bug 3 Strategy

## Overview
The **Parabolic SAR Bug 3 Strategy** is a StockSharp high-level port of the MetaTrader 4 expert advisor `pSAR_bug_3.mq4` located in `MQL/9786`. The robot reacts to the very first Parabolic SAR dot that appears on the opposite side of the price. When the SAR flips below the candle close, the strategy opens a long position after closing any short exposure. When the SAR jumps above the close, it reverses to a short position. Each trade is guarded by fixed stop-loss and take-profit levels measured in Parabolic SAR points and scaled by the same multiplier as in the original MQL program.

## Trading Logic
1. **Market data and indicator** – the strategy subscribes to a configurable candle type (15-minute time-frame by default) and binds a Parabolic SAR indicator with user-specified acceleration step and maximum acceleration.
2. **State tracking** – after the first completed candle the code stores whether the Parabolic SAR value is above or below the close. The next candles compare the new state with the previous one to detect the flip of the indicator.
3. **Long entries** – if the Parabolic SAR switches from above the close to below it, the strategy sends a market order sized to close any active short position and to open the configured long volume. Protective stop-loss and take-profit prices are calculated immediately after entry.
4. **Short entries** – when the Parabolic SAR crosses from below the close to above it, the code mirrors the behaviour for short trades: it flattens long positions and opens a short order.
5. **Exits** – on every finished candle the high and low prices are compared with the stored protective levels. Breaching the stop-loss or the take-profit triggers a market order that closes the open position, matching the MetaTrader approach of broker-side protective orders.

## Risk Management
- The stop-loss and take-profit distances are converted by multiplying `StopLossPoints` or `TakeProfitPoints` with the `StopMultiplier` and the instrument `PriceStep` (or `0.0001` if the symbol does not provide a step).
- Market orders are only sent when `IsFormedAndOnlineAndAllowTrading()` confirms that the subscription is active and trading is permitted.
- Whenever the position direction changes, the unused protective levels for the old side are cleared to prevent stale exits.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Order volume in lots. Updating the value also changes the base `Strategy.Volume` property. |
| `StopLossPoints` | `90` | Stop-loss distance expressed in Parabolic SAR points, later scaled by `StopMultiplier` and the instrument price step. |
| `TakeProfitPoints` | `20` | Take-profit distance expressed in Parabolic SAR points, later scaled by `StopMultiplier` and the price step. |
| `StopMultiplier` | `10` | Multiplier that reproduces the MetaTrader `StopMult` input, enabling fractional-pip brokers compatibility. |
| `SarStep` | `0.02` | Initial acceleration factor for the Parabolic SAR indicator. |
| `SarMaximum` | `0.2` | Maximum acceleration factor for the Parabolic SAR indicator. |
| `CandleType` | `15m time-frame` | Candle type used for indicator calculations and signal detection. |

## Conversion Notes
- MetaTrader closed positions before opening the opposite trade using separate orders. The StockSharp version achieves the same result by sending a single market order sized to offset any opposite exposure and to establish the new position volume.
- Broker-side stop-loss and take-profit orders are emulated by monitoring candle extremes and submitting market exits once the thresholds are violated.
- The additional `StopMultiplier` parameter accepts any positive value but defaults to `10`, the only multiplier documented in the original code comments.
- No Python version is provided for this conversion, exactly as requested in the task description.
