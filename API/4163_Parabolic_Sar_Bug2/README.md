# Parabolic SAR Bug 2 Strategy

## Overview
The **Parabolic SAR Bug 2 Strategy** is the StockSharp high-level conversion of the MetaTrader expert advisor `pSAR_bug2` from the folder `MQL/9503`. The original EA reacts to the very first Parabolic SAR dot that appears on the opposite side of the price. When the dot flips below the close, the system closes any short trades and immediately opens a long position; when the dot jumps above the close, the logic mirrors the behaviour on the short side. Protective stop-loss and take-profit levels are calculated in raw price points, exactly like in MetaTrader where the values are multiplied by the instrument `Point` size.

The StockSharp port keeps the same intent while leveraging the framework's high-level API. It subscribes to finished candles, binds a Parabolic SAR indicator with configurable acceleration parameters, monitors dot reversals, and sends market orders sized to both flatten the previous exposure and establish the new trade.

## Trading Logic
1. **Indicator preparation**. The strategy subscribes to a user-defined candle type (15-minute time-frame by default) and binds a Parabolic SAR with acceleration step `SarStep` and maximum acceleration `SarMaximum`.
2. **State tracking**. On the first completed candle the algorithm records whether the SAR value is above or below the close. Every new candle compares the fresh SAR position with the previously stored state.
3. **Entry rules**.
   - **Long entry**: triggered when the SAR moves from above the close to below the close. The order volume is calculated as `TradeVolume + |Position|`, so an existing short position is closed and reversed in a single market order. After entry, stop-loss and take-profit levels are stored relative to the candle close.
   - **Short entry**: triggered when the SAR moves from below the close to above the close. Any existing long position is flattened and a new short trade is entered at market with the same combined size formula.
4. **Protective exits**. On every completed candle the stored stop-loss and take-profit levels are compared with the high/low. If the price pierces a protective level, the strategy sends a market order to close the open position and resets the cached stop and take values.

## Risk Management
- Stop-loss and take-profit distances are calculated in raw price points by multiplying the configured `StopLossPoints` or `TakeProfitPoints` by the security price step. A conservative fallback of `0.0001` is used when the instrument does not publish a price step.
- The strategy checks `IsFormedAndOnlineAndAllowTrading()` before submitting orders, ensuring that market data is online and trading is allowed.
- Reversal entries always include the absolute current position size, guaranteeing that the new order flattens the previous exposure before establishing the opposite trade.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Base order volume in lots. The same value is assigned to the internal `Strategy.Volume` property. |
| `StopLossPoints` | `90` | Stop-loss distance in price points. The distance is multiplied by the instrument price step to obtain the actual price offset. |
| `TakeProfitPoints` | `20` | Take-profit distance in price points converted through the instrument price step. |
| `SarStep` | `0.001` | Initial acceleration factor for the Parabolic SAR indicator. |
| `SarMaximum` | `0.2` | Maximum acceleration factor for the Parabolic SAR indicator. |
| `CandleType` | `15m time-frame` | Candle type used for calculations and signal evaluation. |

## Notes on the Conversion
- MetaTrader's broker-side stop-loss and take-profit orders are emulated by monitoring candle extremes and submitting market exits when the thresholds are breached.
- The MetaTrader EA required manual management of `OrdersTotal()` and explicit `OrderClose()` calls. The StockSharp version achieves the same behaviour by sending a single market order sized as `TradeVolume + |Position|`, which simultaneously closes any opposite position and opens the new one.
- No Python implementation is provided, matching the task request. The folder currently contains only the C# version of the strategy.
