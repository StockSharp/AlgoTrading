# Parabolic SAR EA Strategy

## Overview
The **Parabolic SAR EA Strategy** is the StockSharp high-level conversion of the MetaTrader expert advisor `Parabolic SAR EA.mq5` located in `MQL/23039`. The original MQL script reacts to Parabolic SAR reversals on a configurable timeframe, opening market positions with fixed stop-loss and take-profit distances expressed in MetaTrader "pips" (fractional pip support included). The C# port subscribes to candles, binds the built-in `ParabolicSar` indicator, and reproduces the same bar-by-bar decision process while respecting StockSharp best practices.

## Trading Logic
1. **Data preparation**
   - The strategy subscribes to the user-selected candle type (30-minute candles by default) and binds a Parabolic SAR indicator configured with adjustable acceleration step and maximum values.
   - The SAR value is calculated for every candle and delivered to the strategy through the high-level `Bind` callback.
2. **Signal generation**
   - Buy signal: when the Parabolic SAR value of the finished candle is strictly below the candle low.
   - Sell signal: when the Parabolic SAR value of the finished candle is strictly above the candle high.
   - Signals are evaluated only on completed candles (`CandleStates.Finished`) to match the MQL new-bar processing.
3. **Position management**
   - Opposite exposure is flattened before a new entry by increasing the requested market order size with the absolute current position value, replicating the MetaTrader `ClosePosition` plus `OpenPosition` sequence.
   - Every entry recalculates protective stop-loss and take-profit levels using the same pip-to-price conversion rules as MetaTrader (digits 3/5 instruments receive a ×10 multiplier for the `PriceStep`).
4. **Protective exits**
   - On every finished candle the strategy checks whether the high/low breaches the stored stop-loss or take-profit level. If triggered, the position is closed with a market order and the corresponding targets are cleared.
   - Protective logic fires before new signals on the same bar, mirroring the original Expert Advisor behaviour where stop orders are broker-side.

## Indicator and Data Notes
- Uses the built-in `ParabolicSar` indicator from StockSharp with parameters `SarStep` and `SarMaximum`.
- Candle subscription is handled through `SubscribeCandles` without adding the indicator to `Strategy.Indicators`, as required by the project guidelines.
- Trading is allowed only when `IsFormedAndOnlineAndAllowTrading()` reports true, ensuring that live data is present and the connector permits order submission.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `TradeVolume` | `1` | Market order size in lots. Updating the value also refreshes `Strategy.Volume`. |
| `StopLossPips` | `50` | Stop-loss distance in MetaTrader pips. A pip equals `PriceStep × 10` for instruments with 3 or 5 decimals, otherwise just `PriceStep`. Set to `0` to disable. |
| `TakeProfitPips` | `50` | Take-profit distance in MetaTrader pips using the same conversion rules as the stop-loss. Set to `0` to disable. |
| `SarStep` | `0.02` | Acceleration step used by the Parabolic SAR indicator. |
| `SarMaximum` | `0.2` | Maximum acceleration factor for Parabolic SAR. |
| `CandleType` | `30m timeframe` | Candle type used for calculations. Supports any `DataType` derived from `TimeFrame`. |

## Risk Management and Behaviour
- Stop-loss and take-profit are recalculated after each fill and stored internally; no pending orders are registered with the exchange.
- If both protective levels are hit inside a single candle, the stop-loss check fires first, replicating the conservative handling of the source MQL logic.
- When the connector does not report a valid `PriceStep`, the conversion falls back to `0.0001` to avoid zero-distance protective levels.
- No averaging or pyramiding is performed; the strategy operates with a single net position, flipping direction when the Parabolic SAR crosses price.

## Conversion Notes
- MetaTrader `InpBarCurrent` equals 1, meaning the EA evaluates the previous finished candle. The StockSharp port achieves the same outcome by processing only `Finished` candles in the `Bind` callback.
- The original expert used `CheckVolumeValue` to validate lots and broker constraints. StockSharp delegates these checks to the connector, while the `TradeVolume` parameter still enforces a positive volume requirement.
- Python implementation is intentionally omitted, matching the task requirements.
