# Exp Iin MA Signal MMRec Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A StockSharp port of the MetaTrader expert "Exp_Iin_MA_Signal_MMRec". The strategy listens to the crossover signals produced by a pair of configurable moving averages (the original Iin_MA_Signal indicator) and applies an adaptive position sizing scheme with loss-based reduction.

## Overview

- **Signal generation**: the fast and slow moving averages are evaluated on the selected candle type and applied price. A buy signal is created when the fast average crosses above the slow one, while a sell signal is produced on the opposite crossover. The `SignalBar` parameter postpones the execution by the specified number of fully closed bars, reproducing the indicator buffer lag used in the MQL version.
- **Position management**: `BuyPosOpen` and `SellPosOpen` enable or disable long and short entries. When an opposite signal appears and the corresponding `BuyPosClose` or `SellPosClose` flag is enabled, the strategy either closes the current exposure or reverses directly into the new direction.
- **Risk control**: `StopLossPoints` and `TakeProfitPoints` translate to price distances using `Security.PriceStep` and are checked against the candle extremes before processing fresh signals.
- **Money management**: the last trades are tracked separately for longs and shorts. When the number of losing trades inside the `BuyTotalTrigger`/`SellTotalTrigger` window reaches the respective loss threshold, the strategy switches from `NormalVolume` to `ReducedVolume`. The `MoneyMode` parameter defines how the volume value is interpreted (fixed lots, balance percentage, or stop-based risk percentage).

## Parameters

- `FastPeriod`, `SlowPeriod` – lengths of the fast and slow moving averages.
- `FastType`, `SlowType` – moving average flavours (`Simple`, `Exponential`, `Smoothed`, `Weighted`, `VolumeWeighted`).
- `FastPrice`, `SlowPrice` – applied price for each average (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`).
- `SignalBar` – number of closed bars between a detected signal and the order submission.
- `BuyPosOpen`, `SellPosOpen` – toggles for opening long/short positions.
- `BuyPosClose`, `SellPosClose` – toggles for closing or reversing an existing position on the opposite signal.
- `BuyTotalTrigger`, `SellTotalTrigger` – how many recent trades are inspected for the loss counter.
- `BuyLossTrigger`, `SellLossTrigger` – minimum number of losses inside the inspected window that activates the reduced volume.
- `NormalVolume`, `ReducedVolume` – primary and fallback volume (or risk factor, depending on `MoneyMode`).
- `StopLossPoints`, `TakeProfitPoints` – stop loss and take profit distances in instrument points.
- `MoneyMode` – interpretation of the volume values (`Lot`, `Balance`, `FreeMargin`, `BalanceRisk`, `FreeMarginRisk`). Balance-based modes use `Portfolio.CurrentValue`, while risk-based modes divide the risk amount by the calculated stop distance.
- `CandleType` – candle series used for indicator calculations.

## Signal Logic

1. Every finished candle feeds the moving averages with the chosen applied price.
2. The difference between current and previous moving average values defines a crossover event.
3. Signals are queued, and the oldest entry is executed once the queue size exceeds `SignalBar`.
4. When a buy signal is executed:
   - If a short position exists and `SellPosClose` is enabled, the strategy computes the realized PnL for that short trade. It then either reverses into a long (if `BuyPosOpen` is enabled) or simply closes the exposure.
   - If no position is open and `BuyPosOpen` is enabled, a fresh long is opened with the computed volume.
5. Sell signals mirror the buy workflow.

## Money Management Details

- Trade history is stored as a rolling FIFO queue limited by `BuyTotalTrigger` / `SellTotalTrigger`.
- A losing trade (negative PnL) increments the loss counter. When the counter reaches `BuyLossTrigger` or `SellLossTrigger`, the next position uses `ReducedVolume`.
- `MoneyMode = Lot` treats the volume values as raw quantities.
- `MoneyMode = Balance` and `FreeMargin` multiply the configured value by `Portfolio.CurrentValue` and divide by the current close price to obtain the quantity.
- `MoneyMode = BalanceRisk` and `FreeMarginRisk` multiply the configured value by `Portfolio.CurrentValue` and divide by the stop-loss distance. If the stop distance is zero, the fallback is identical to the balance percentage calculation.
- If portfolio information is unavailable, the computed volume defaults to zero to avoid accidental orders.

## Risk Handling

- Stop-loss and take-profit levels are recalculated on every candle using the entry price and point value. If a level is touched within the candle range, the position is closed before new signals are processed.
- Closing actions always record the trade result, ensuring that the money management queues remain synchronized with actual exits.

## Notes

- Ensure that `StopLossPoints` and `TakeProfitPoints` are compatible with the instrument tick size; the strategy multiplies them by `Security.PriceStep`.
- When `MoneyMode` relies on portfolio data, the strategy expects the `Portfolio` object to expose `CurrentValue`.
- The algorithm operates on a net position basis: simultaneous long and short holdings are not supported.
