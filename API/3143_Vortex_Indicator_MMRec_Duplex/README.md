# Vortex Indicator MMRec Duplex Strategy

## Overview
- Converted from the MetaTrader 5 expert **Exp_VortexIndicator_MMRec_Duplex.mq5** (MQL ID 23180).
- Maintains two independent Vortex indicator streams: one dedicated to long trades and one to short trades. Each stream has its own timeframe, length, and bar shift so that bullish and bearish logic can be tuned separately.
- Replicates the "MMRec" money-management recovery module from the original EA. The strategy tracks the latest trade results per direction and temporarily switches to a reduced order size after a configurable number of losses.

## Signal Logic
1. Subscribe to the configured candle type for each stream and calculate the Vortex indicator (`VI+` and `VI-`).
2. **Long entries:** when the previous bar had `VI+` below or equal to `VI-` and the current bar closes with `VI+` above `VI-` (bullish crossover). Entries are allowed only if `AllowLongEntries` is enabled.
3. **Long exits:** when `VI-` rises above `VI+` on the evaluated bar, provided that `AllowLongExits` is enabled.
4. **Short entries:** when the previous bar had `VI+` above or equal to `VI-` and the current bar closes with `VI+` below `VI-` (bearish crossover), controlled by `AllowShortEntries`.
5. **Short exits:** when `VI+` climbs back above `VI-` on the evaluated bar, controlled by `AllowShortExits`.
6. Each direction keeps its own stop-loss and take-profit levels measured in price steps. Hitting either level immediately closes the position and registers the result for the recovery counters.

## Money-Management Recovery
- The original EA inspects a sliding window of past trades to decide whether the next order should use the normal or reduced volume. This port mirrors the same behaviour.
- For long trades the queue stores up to `LongTotalTrigger` most recent PnL results. If at least `LongLossTrigger` of them are losing trades, the next long entry uses `LongSmallMoneyManagement`; otherwise it uses `LongMoneyManagement`.
- Short trades repeat the same logic with `ShortTotalTrigger`, `ShortLossTrigger`, `ShortSmallMoneyManagement`, and `ShortMoneyManagement`.
- When the trigger values are zero the queues are cleared and the base volume is always used.

## Margin Modes
`MarginModeOption` describes how the money-management value is turned into an executable volume:
- **FreeMargin (0):** treat the value as a fraction of capital (approximation of the original "free margin" mode).
- **Balance (1):** identical to `FreeMargin` in this port; uses the current portfolio value.
- **LossFreeMargin (2):** risk a capital fraction using the configured stop-loss distance. Falls back to price-based sizing if the stop distance is zero.
- **LossBalance (3):** same as `LossFreeMargin` in this implementation.
- **Lot (4):** interpret the value directly as order volume.

All calculated sizes are normalised using the instrument's volume step as well as minimum and maximum volume constraints.

## Parameters
| Parameter | Default | Description |
| --- | --- | --- |
| `LongCandleType` | H4 | Timeframe used to feed the long-side Vortex indicator. |
| `ShortCandleType` | H4 | Timeframe used to feed the short-side Vortex indicator. |
| `LongLength` | 14 | Period of the Vortex indicator for long signals. |
| `ShortLength` | 14 | Period of the Vortex indicator for short signals. |
| `LongSignalBar` | 1 | Closed-bar offset evaluated for long crossovers (0 = latest closed bar). |
| `ShortSignalBar` | 1 | Closed-bar offset evaluated for short crossovers. |
| `AllowLongEntries` | true | Enable long entries when the bullish crossover appears. |
| `AllowLongExits` | true | Enable closing long positions when `VI-` dominates `VI+`. |
| `AllowShortEntries` | true | Enable short entries when the bearish crossover appears. |
| `AllowShortExits` | true | Enable closing short positions when `VI+` dominates `VI-`. |
| `LongTotalTrigger` | 5 | Number of recent long trades inspected by the recovery counter. |
| `LongLossTrigger` | 3 | Losing long trades required before switching to the reduced long volume. |
| `LongMoneyManagement` | 0.1 | Base money-management value for long trades. |
| `LongSmallMoneyManagement` | 0.01 | Reduced money-management value after a long losing streak. |
| `LongMarginMode` | Lot | Interpretation of the long money-management value (see modes above). |
| `LongStopLossSteps` | 1000 | Protective distance below the long entry expressed in price steps. |
| `LongTakeProfitSteps` | 2000 | Take-profit distance above the long entry expressed in price steps. |
| `LongSlippageSteps` | 10 | Informational slippage allowance for long orders (not used for sizing). |
| `ShortTotalTrigger` | 5 | Number of recent short trades inspected by the recovery counter. |
| `ShortLossTrigger` | 3 | Losing short trades required before switching to the reduced short volume. |
| `ShortMoneyManagement` | 0.1 | Base money-management value for short trades. |
| `ShortSmallMoneyManagement` | 0.01 | Reduced money-management value after a short losing streak. |
| `ShortMarginMode` | Lot | Interpretation of the short money-management value. |
| `ShortStopLossSteps` | 1000 | Protective distance above the short entry expressed in price steps. |
| `ShortTakeProfitSteps` | 2000 | Take-profit distance below the short entry expressed in price steps. |
| `ShortSlippageSteps` | 10 | Informational slippage allowance for short orders. |

## Implementation Notes
- Built entirely on the StockSharp high-level API. Candle subscriptions drive the Vortex indicators through `Bind`, which delivers finished bars before any decision is made.
- The trade-recovery logic stores per-direction profit series in queues and mirrors the MetaTrader `BuyTradeMMRecounterS` / `SellTradeMMRecounterS` functions.
- Stop-loss and take-profit levels are recalculated in price units (instrument price step × configured steps) and enforced on every incoming candle.
- Order volumes are normalised via the security's `VolumeStep`, `MinVolume`, and `MaxVolume` constraints to avoid invalid submissions.
- Slippage parameters are preserved for documentation purposes but are not used directly by the StockSharp order handlers.
