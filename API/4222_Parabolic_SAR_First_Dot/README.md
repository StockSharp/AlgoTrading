# Parabolic SAR First Dot Strategy

## Overview
The **Parabolic SAR First Dot Strategy** is the StockSharp high-level conversion of the MetaTrader expert advisor `pSAR_bug_4` from the folder `MQL/9954`. The system reacts to the very first dot of the Parabolic SAR that appears on the opposite side of price. When the SAR flips below the close, a long trade is opened; when the SAR jumps above the close, a short trade is executed. Every position is protected with fixed stop-loss and take-profit distances expressed in Parabolic SAR "points", just like in the original MQL version.

## Trading Logic
1. **Data and indicator preparation**. The strategy subscribes to a configurable candle type (15-minute candles by default) and binds a Parabolic SAR indicator with user-defined acceleration step and maximum acceleration.
2. **State tracking**. On the first completed candle the strategy remembers whether the SAR is above or below the close. Later candles compare the new SAR position with the previous state.
3. **Entry rules**.
   - **Long entry**: the SAR switches from above the close to below the close. Any existing short position is closed and a new long position with the configured volume is opened at market.
   - **Short entry**: the SAR switches from below the close to above the close. Any existing long position is closed before opening a new short position.
4. **Protective orders**. Immediately after entry the strategy stores stop-loss and take-profit levels calculated from the candle close by multiplying `StopLossPoints` or `TakeProfitPoints` by the security `PriceStep`. If `UseStopMultiplier` is enabled (default behaviour copied from MetaTrader), the distance is multiplied by 10 to account for brokers quoting with fractional pips.
5. **Exit rules**. On every finished candle the strategy checks the high and low against the stored stop-loss and take-profit levels. If the high or low breaches the level, the position is closed at market. When an opposite SAR signal arrives the position is also reversed by sending an order sized to flat the current exposure and open the new trade.

## Risk Management
- Stop-loss and take-profit distances are recalculated for every new position.
- The code performs a conservative fallback: when the security does not provide a price step, a value of `0.0001` is used to avoid zero distances.
- All trading decisions use the `IsFormedAndOnlineAndAllowTrading()` helper to ensure that the subscription is active and live.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Order volume used for new positions. The parameter also updates the base `Strategy.Volume` property. |
| `StopLossPoints` | `90` | Stop-loss distance expressed in Parabolic SAR points. The value is multiplied by the security `PriceStep` (and optionally by 10 when `UseStopMultiplier` is true). |
| `TakeProfitPoints` | `20` | Take-profit distance in Parabolic SAR points converted through the price step. |
| `UseStopMultiplier` | `true` | If enabled, multiplies the stop-loss and take-profit distances by 10 to mimic the MetaTrader expert's `StopMult` switch. |
| `SarAccelerationStep` | `0.02` | Initial acceleration factor supplied to the Parabolic SAR indicator. |
| `SarAccelerationMax` | `0.2` | Maximum acceleration factor for the Parabolic SAR indicator. |
| `CandleType` | `15m time-frame` | Candle type used for the indicator and signal calculations. |

## Notes on the Conversion
- MetaTrader stop-loss and take-profit orders were broker-side protective orders. StockSharp reproduces them by monitoring candle highs and lows and sending market exits when the thresholds are crossed.
- The MetaTrader expert multiplied stop distances by ten whenever `StopMult` was true to improve compatibility with brokers quoting with fractional pips. The `UseStopMultiplier` parameter implements the same behaviour.
- The conversion uses StockSharp's high-level API (`SubscribeCandles`, `Bind`, `BuyMarket`, `SellMarket`) as required by the project guidelines. No additional Python version is provided yet, matching the task request.
