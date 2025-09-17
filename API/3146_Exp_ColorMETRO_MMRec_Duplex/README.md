# Exp ColorMETRO MMRec Duplex Strategy

## Overview
This strategy ports the MetaTrader 5 expert advisor `Exp_ColorMETRO_MMRec_Duplex` to StockSharp. The original robot runs two independent ColorMETRO indicator modules (one long, one short) and applies an MMRec (money management recalculation) overlay that shrinks the position size after repeated losses. The C# version mirrors that behaviour while using StockSharp's high-level API for candle subscriptions and order routing.

## Trading Logic
- Two distinct ColorMETRO indicators operate on configurable candle types. The long module only manages long exposure while the short module controls short exposure.
- Each indicator produces a fast and a slow stepped RSI envelope. The strategy mimics the MQL5 `CopyBuffer` calls by storing historical values and inspecting the bar defined by `SignalBar`.
- A long entry is generated when the fast band crosses **below** the slow band on the inspected bar while the previous bar still had the fast band above the slow band. Any open short position is flattened before opening the new long.
- Long exits occur when the slow band on the previous inspected bar sits above the fast band, signalling a bearish regime in the original EA.
- Short entries and exits mirror the long logic (crossing above for entries, fast line above the slow line on the previous bar for exits).
- Only finished candles are processed and trading is blocked until the indicator reports both bands as ready, reproducing the MetaTrader warm-up period.

## Money Management (MMRec)
- `Strategy.Volume` defines the reference lot size. The long and short modules multiply it by their respective `LongMm`/`ShortMm` coefficients when sizing new orders.
- After every completed trade the strategy records whether the result was a loss (based on candle close prices, just like the EA that inspects historical deals).
- If the most recent `TotalTrigger` trades for a module contain at least `LossTrigger` losers, the module switches to the reduced multiplier (`SmallMm`). Once the loss count drops below the threshold the default multiplier is restored automatically.
- Position reversals first finalise the existing trade's result (updating the MMRec counters) before sizing and opening the opposite direction.

## Indicator Notes
- `ColorMetroMmrecIndicator` is a faithful port of the `ColorMETRO` custom indicator. It feeds the same fast/slow envelopes driven by an RSI core with step tracking and trend memory.
- The indicator exposes the internal RSI and a readiness flag so that the strategy can ignore incomplete values exactly as the MQL implementation does.

## Parameters
| Group | Name | Description |
| --- | --- | --- |
| Long | `LongCandleType` | Candle type used for the long ColorMETRO module. |
| Long | `LongTotalTrigger` | Number of completed long trades inspected when evaluating MMRec. |
| Long | `LongLossTrigger` | Loss count that activates the reduced long multiplier. |
| Long | `LongSmallMm` | Reduced multiplier applied to long trades after a loss streak. |
| Long | `LongMm` | Default multiplier for long trades. |
| Long | `LongEnableOpen` | Enables opening long positions. |
| Long | `LongEnableClose` | Enables closing long positions. |
| Long | `LongPeriodRsi` | RSI length used inside the long ColorMETRO indicator. |
| Long | `LongStepSizeFast` | Fast envelope step size for the long module. |
| Long | `LongStepSizeSlow` | Slow envelope step size for the long module. |
| Long | `LongSignalBar` | Historical shift (in closed bars) used when reading indicator values. |
| Long | `LongMagic` | Original MT5 magic number, kept for reference. |
| Long | `LongStopLossTicks` | Stop-loss distance placeholder from the EA (not enforced). |
| Long | `LongTakeProfitTicks` | Take-profit distance placeholder from the EA (not enforced). |
| Long | `LongDeviationTicks` | Allowed slippage placeholder from the EA (not enforced). |
| Long | `LongMarginMode` | MM mode flag retained for compatibility (logic uses raw multipliers). |
| Short | `ShortCandleType` | Candle type used for the short ColorMETRO module. |
| Short | `ShortTotalTrigger` | Number of completed short trades inspected when evaluating MMRec. |
| Short | `ShortLossTrigger` | Loss count that activates the reduced short multiplier. |
| Short | `ShortSmallMm` | Reduced multiplier applied to short trades after a loss streak. |
| Short | `ShortMm` | Default multiplier for short trades. |
| Short | `ShortEnableOpen` | Enables opening short positions. |
| Short | `ShortEnableClose` | Enables closing short positions. |
| Short | `ShortPeriodRsi` | RSI length used inside the short ColorMETRO indicator. |
| Short | `ShortStepSizeFast` | Fast envelope step size for the short module. |
| Short | `ShortStepSizeSlow` | Slow envelope step size for the short module. |
| Short | `ShortSignalBar` | Historical shift (in closed bars) used when reading indicator values. |
| Short | `ShortMagic` | Original MT5 magic number, kept for reference. |
| Short | `ShortStopLossTicks` | Stop-loss distance placeholder from the EA (not enforced). |
| Short | `ShortTakeProfitTicks` | Take-profit distance placeholder from the EA (not enforced). |
| Short | `ShortDeviationTicks` | Allowed slippage placeholder from the EA (not enforced). |
| Short | `ShortMarginMode` | MM mode flag retained for compatibility (logic uses raw multipliers). |

## Implementation Notes
- The strategy relies on `SubscribeCandles(...).BindEx(...)` and avoids direct buffer access, aligning with the conversion guidelines.
- Protective stops from the EA are left as parameters only; users can attach `StartProtection` or custom risk modules if needed.
- Both modules share the same security instance but keep their own candle subscriptions and MMRec counters, matching the duplex layout from MetaTrader.
- All in-code comments are provided in English and the logic refrains from using prohibited API calls such as `GetTrades`.

## Disclaimer
This port reproduces the logical structure of the original EA, but execution quality depends on the connected broker, data feed, and StockSharp configuration. Always validate behaviour on historical and demo data before trading live capital.
