# Rubberbands Safety Net Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

StockSharp port of the RUBBERBANDS 1.6 MetaTrader expert advisor. The original system keeps a hedged pair of buy and sell tickets, reinjects the closed side after each profit, and activates a safety net when the running loss exceeds predefined cash thresholds. The conversion keeps the alternating cycle but adapts the mechanics to StockSharp's net-position model by averaging in the direction of the current exposure instead of holding independent hedge orders.

## Trading Logic

- **Cycle start:** At the top of each minute, or when `Enter Now` is toggled, the strategy opens a market position using `BaseVolume`. The next cycle alternates direction (buy then sell, then buy again, etc.).
- **Base profit target:** The running unrealized PnL is compared with `TargetProfitPerLot * BaseVolume`. When reached, the position is liquidated and the next cycle flips direction.
- **Session control:** `UseSessionTakeProfit` and `UseSessionStopLoss` watch the cumulative realized plus unrealized profit measured in cash per base lot. Hitting either threshold triggers a full liquidation and resets the counters.
- **Safety mode:** When enabled and the unrealized loss exceeds `SafetyStartPerLot * BaseVolume`, the algorithm enters safety mode and starts averaging in the current direction by sending additional orders of size `SafetyVolume`. Every extra `SafetyStepPerLot` loss per safety lot schedules another averaging order.
- **Safety exits:** While in safety mode the position is flattened once the unrealized gain reaches `SafetyProfitPerLot * |Position|` or when the session level metric crosses `SafetyModeTakeProfitPerLot * BaseVolume`.

## Entry Conditions

### Long entries
- No open exposure and either the minute just rolled over or `Enter Now` is true.
- The strategy currently expects to open a long (cycles alternate).
- Manual stop switch is disabled.

### Short entries
- Same as the long conditions but the next cycle direction is short.

## Exit Management

- **Base target hit:** Close the entire position and flip the cycle direction.
- **Session TP/SL:** Close the position, clear realized profit counters and stay flat until the next minute trigger.
- **Safety profit:** Close the position when the net PnL target is met while safety mode is active.
- **Safety averaging:** Additional safety orders are appended when the unrealized loss grows in increments of `SafetyStepPerLot`.
- **Manual close:** Setting `Close Now` closes the position on the next candle and resets the realized profit accumulator.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `BaseVolume` | Market order size for the primary leg. |
| `TargetProfitPerLot` | Profit target (cash per lot) for the base trade. |
| `UseSessionTakeProfit` / `SessionTakeProfitPerLot` | Enable and configure the session-wide take profit. |
| `UseSessionStopLoss` / `SessionStopLossPerLot` | Enable and configure the session-wide stop loss. |
| `UseSafetyMode` | Toggle the safety averaging logic. |
| `SafetyStartPerLot` | Loss per base lot that activates safety mode. |
| `SafetyVolume` | Volume of each safety averaging order. |
| `SafetyStepPerLot` | Additional loss per safety lot needed to queue another safety order. |
| `SafetyProfitPerLot` | Profit target applied while in safety mode. |
| `SafetyModeTakeProfitPerLot` | Session-level profit target while safety mode is active. |
| `UseInitialState`, `InitialProfitSoFar`, `InitialSafetyMode`, `InitialSafetyToBuy`, `InitialUsedSafetyCount` | State restoration helpers for restarts. |
| `QuiesceNow`, `Enter Now`, `Stop Trading`, `Close Now` | Manual switches mirroring the original EA extern variables. |
| `CandleType` | Time frame of the candle series that drives the loop (default 1 minute). |

## Practical Notes

- StockSharp keeps a single net position per instrument. Instead of holding simultaneous buy and sell tickets, the conversion averages into the existing position when safety mode is active. This preserves the cash-based thresholds while conforming to the netting model.
- The profit and loss thresholds are expressed in account currency per lot, mirroring the MetaTrader extern inputs. Adjust them to match the instrument's tick value.
- Manual switches (`Stop Trading`, `Close Now`, `Enter Now`, `Quiesce`) can be changed on the fly from the user interface to control the strategy without editing the code.
- `StartProtection()` is invoked on start to reuse the standard StockSharp protection framework for risk controls.
- Ensure the instrument metadata (`VolumeStep`, `VolumeMin`, `VolumeMax`) is configured so that the requested volumes pass exchange validation; the helper automatically aligns them to the nearest valid step.
