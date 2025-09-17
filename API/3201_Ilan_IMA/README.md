# Ilan iMA Strategy

## Overview
The **Ilan iMA Strategy** is a StockSharp port of the MetaTrader 5 expert advisor `Ilan iMA.mq5`. The advisor combines a shifted
moving-average trend filter with a martingale-style averaging grid. The StockSharp version re-implements the same ideas with the
high-level API: when the weighted moving average confirms a trend, the strategy opens a market order and keeps adding trades every
time price moves against the position by a configurable step. The whole basket is closed when a profit target, trailing stop or
explicit stop-loss is reached, reproducing the money-management model of the original EA.

## Trading logic
1. Subscribe to the selected timeframe (`CandleType`) and feed a configurable moving average (`MaMethod`, `MaPeriod`,
   `PriceMode`). A positive `MaShift` shifts the indicator forward, so the strategy evaluates historic values to mimic the MT5
   behaviour.
2. Wait for the candle to close. Only finished bars generate signals and update trailing/stop logic.
3. Detect the trend by comparing four consecutive moving-average values shifted by `MaShift` bars:
   - strictly decreasing values signal a downtrend;
   - strictly increasing values signal an uptrend.
4. When no basket is open:
   - in a downtrend, if the close is above the moving average value, open a short with `StartVolume`;
   - in an uptrend, if the close is below the moving average value, open a long with `StartVolume`.
5. When a basket exists:
   - if price moves against the position by at least `GridStepPips`, open another order whose size grows by `LotExponent` but is
     capped by `LotMaximum` and the exchange volume limits;
   - the average entry price, lowest buy price and highest sell price are tracked internally to keep the behaviour close to the
     MT5 logic.
6. Close conditions:
   - once the floating profit of a basket with more than one trade reaches `ProfitMinimum` (in account currency), close all
     orders in that direction;
   - if the floating profit reaches `TakeProfitPips` or the loss hits `StopLossPips`, close the basket;
   - trailing protection becomes active after `TrailingStopPips + TrailingStepPips` points of favourable movement and moves in
     steps of `TrailingStepPips`.

## Risk management and sizing
- `StartVolume` replicates the MT5 `StartLots` parameter. Every additional order multiplies the previous size by `LotExponent`
  while respecting `LotMaximum` and the venue limits (`Security.MinVolume`, `Security.VolumeStep`, `Security.MaxVolume`).
- `ProfitMinimum` preserves the "lock release" behaviour from the MT5 version: once the grid recovered from a hedge and prints
  the requested profit, all trades in that direction are closed.
- Stop-loss and take-profit distances are measured in pips (`StopLossPips`, `TakeProfitPips`). The helper method converts pips
  into exchange price steps using `Security.PriceStep`.
- The trailing block emulates the MT5 implementation: trailing starts only after the price exceeds
  `TrailingStopPips + TrailingStepPips` and is updated in discrete steps to avoid premature stop adjustments.

## Parameters
| Name | Type | Default | MT5 counterpart | Description |
| --- | --- | --- | --- | --- |
| `MaPeriod` | `int` | `15` | `Inp_MA_ma_period` | Period of the trend filter moving average. |
| `MaShift` | `int` | `5` | `Inp_MA_ma_shift` | Forward shift of the moving-average line in bars. |
| `MaMethod` | `MovingAverageMethod` | `Weighted` | `Inp_MA_ma_method` | Smoothing algorithm (SMA, EMA, SMMA, LWMA). |
| `PriceMode` | `CandlePrice` | `Weighted` | `Inp_MA_applied_price` | Candle price fed into the indicator. |
| `StartVolume` | `decimal` | `1` | `InpStartLots` | Base order volume for the first trade in a basket. |
| `GridStepPips` | `decimal` | `30` | `InpStep` | Distance (in pips) between averaging entries. |
| `LotExponent` | `decimal` | `1.6` | `InpLotExponent` | Multiplier applied to the previous order size. |
| `LotMaximum` | `decimal` | `15` | `InpLotMaximum` | Hard cap for a single order volume. |
| `ProfitMinimum` | `decimal` | `15` | `InpProfitMinimum` | Minimum floating profit required to close a basket with several trades. |
| `StopLossPips` | `decimal` | `0` | `InpStopLoss` | Stop-loss distance expressed in pips (0 disables the stop). |
| `TakeProfitPips` | `decimal` | `100` | `InpTakeProfit` | Take-profit distance expressed in pips. |
| `TrailingStopPips` | `decimal` | `15` | `InpTrailingStop` | Profit threshold that activates the trailing stop. |
| `TrailingStepPips` | `decimal` | `5` | `InpTrailingStep` | Minimum additional profit before the trailing stop moves again. |
| `CandleType` | `DataType` | 15-minute timeframe | chart period | Timeframe used for signal calculation. |

## Differences from the original EA
- StockSharp works in a netting environment, therefore only one net position per direction exists. The strategy keeps an internal
  list of entry prices and volumes to emulate the MT5 basket accounting.
- Exchange-specific volume limits are always respected when rounding volumes, whereas the MT5 code relied on manual checks. This
  prevents orders that would be rejected by the broker connector.
- Stop-loss, take-profit and trailing logic are expressed through market exits instead of modifying existing MT5 positions. The
  functional behaviour remains the same, but order management is handled by StockSharp.

## Usage notes
- Ensure the security metadata (`PriceStep`, `StepPrice`, `MinVolume`, `VolumeStep`, `MaxVolume`) is filled in the connector so
  that the pip-to-price conversions and volume rounding operate correctly.
- The trailing block assumes the pip size equals the exchange price step. Adjust `GridStepPips`, `StopLossPips` and
  `TrailingStopPips` for instruments with unconventional tick sizes.
- Martingale grids are risky by nature. Test the strategy on historical data and use realistic commission/slippage settings
  before deploying to production.
