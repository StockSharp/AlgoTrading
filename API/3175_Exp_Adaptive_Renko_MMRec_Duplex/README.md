# Exp Adaptive Renko MMRec Duplex Strategy

This strategy ports the MetaTrader 5 expert advisor **Exp_AdaptiveRenko_MMRec_Duplex.mq5** to the StockSharp high level API. Two independent Adaptive Renko streams – one configured for long opportunities and one for shorts – watch how the custom brick channels flip between support and resistance. When the long channel reports fresh support while the short channel drops resistance (or vice versa), the strategy opens the corresponding market position. The C# version keeps the original "MM Recounter" money-management block that cuts the trade size after a configurable series of losses and restores it once the streak ends.

## Core workflow

1. **Data subscriptions** – each side subscribes to its own candle type (time frame) and binds a volatility indicator (ATR or Standard Deviation) through `SubscribeCandles().BindEx(...)`. The indicator drives the adaptive brick height.
2. **Adaptive Renko processing** – the helper `AdaptiveRenkoProcessor` rebuilds the MQL indicator logic, returning a snapshot with the latest trend, support and resistance levels. Signals are evaluated on finished candles only.
3. **Entry logic** – when the long Renko snapshot indicates an upswing (support prints on the signal bar), the strategy opens a long position. Short entries require a downswing from the short stream.
4. **Exit logic** – opposite Renko events close an active position. Additional checks enforce stop-loss and take-profit distances expressed in price steps.
5. **MMRec money management** – each direction keeps a queue of recent realised PnL values. If the number of losses inside the configured window reaches the loss trigger, the next order uses the reduced money-management value (`LongSmallMoneyManagement` / `ShortSmallMoneyManagement`). Otherwise the normal value (`LongMoneyManagement` / `ShortMoneyManagement`) is used. The `MarginModeOption` enum reproduces the MQL sizing modes (lot, balance share, loss-based share, etc.).
6. **Trade registration** – every exit calls `RegisterTradeResult` to feed the MMRec queues. Queue trimming mirrors the original functions `BuyTradeMMRecounterS` and `SellTradeMMRecounterS` without scanning terminal history.

## Parameter groups

| Group | Key parameters | Description |
| --- | --- | --- |
| Long side | `LongCandleType`, `LongVolatilityMode`, `LongVolatilityPeriod`, `LongSensitivity`, `LongPriceMode`, `LongMinimumBrickPoints`, `LongSignalBarOffset` | Control the Adaptive Renko stream that produces long entries. |
| Short side | `ShortCandleType`, `ShortVolatilityMode`, `ShortVolatilityPeriod`, `ShortSensitivity`, `ShortPriceMode`, `ShortMinimumBrickPoints`, `ShortSignalBarOffset` | Mirror the settings for the short module. |
| MMRec | `LongTotalTrigger`, `LongLossTrigger`, `LongSmallMoneyManagement`, `LongMoneyManagement`, `LongMarginMode`, `ShortTotalTrigger`, `ShortLossTrigger`, `ShortSmallMoneyManagement`, `ShortMoneyManagement`, `ShortMarginMode` | Replicate the money-management recovery block. The *TotalTrigger* parameters define the rolling window size, *LossTrigger* the loss count that activates the reduced volume. |
| Risk | `LongStopLossPoints`, `LongTakeProfitPoints`, `ShortStopLossPoints`, `ShortTakeProfitPoints`, `LongDeviationSteps`, `ShortDeviationSteps` | Express protective levels and informational slippage in price steps. |

## Behavioural notes

- The strategy works on the netting account model: before opening a long trade it closes any outstanding short and vice versa.
- Position sizes are calculated through `CalculateVolume`. The helper supports all original margin modes including loss-based sizing that depends on the configured stop-loss distance.
- All indicator processing happens on finished candles only, respecting the source EA.
- Logs include the money-management multiplier and the expected slippage (in steps) for traceability.

## Files

- `CS/ExpAdaptiveRenkoMmrecDuplexStrategy.cs` – strategy implementation with the Adaptive Renko processor and MMRec module.
- `README.md` – English documentation (this file).
- `README_ru.md` – Russian documentation.
- `README_cn.md` – Chinese documentation.
