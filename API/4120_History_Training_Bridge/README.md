# History Training Bridge Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a high-level StockSharp port of the MetaTrader script from `MQL/9196/HistTraining/HistTraining/HistoryTrain.mq4`. The original Expert Advisor listens to the `SharedVarsDLLv2.dll` shared-memory bridge and reacts to integer flags set by an external trainer application:

- `GetInt(97) == 1` &rarr; open a buy order with volume `GetFloat(1)` at the current ask.
- `GetInt(98) == 1` &rarr; open a sell order with volume `GetFloat(1)` at the current bid.
- `GetInt(99) == 1` together with `GetInt(20)` &rarr; close the order that has the provided magic number.
- `GetInt(30) == 1` &rarr; close all open orders and reset the shared flags.

The MQL expert stores diagnostic data back into the DLL (last magic number, direction, and price) after every action. The StockSharp implementation reproduces the same workflow using strategy parameters and high-level API calls while respecting the platform differences (netted positions instead of MetaTrader's hedging accounts).

## Command workflow

1. **Level 1 subscription** – the strategy subscribes to `SubscribeLevel1()` to track best bid/ask quotes. These values are stored so that outgoing order diagnostics match the bid/ask that triggered the action.
2. **Timer heartbeat** – a 200&nbsp;ms timer calls `ProcessPendingRequests`. The timer converts parameter toggles into concrete trade requests, mirroring the polling loop from the DLL.
3. **Command execution**
   - `RequestBuy` / `RequestSell`: submit market orders using `CreateMarketOrder` + `RegisterOrder`. If the current position is on the opposite side, the strategy automatically sends the extra volume needed to flatten before flipping, exactly like the MQL script which stacked hedged orders.
   - `RequestCloseSelected`: closes the portion of the position associated with `TargetOrderNumber`. Volumes are tracked per command so that multiple add-on entries can be unwound incrementally.
   - `RequestCloseAll`: flattens the entire position and clears the internal order registry.
4. **Diagnostics** – after every processed command the strategy updates:
   - `LastOrderNumber` – sequential identifier that starts from 0 whenever no positions remain (matches `magn` in the original code).
   - `LastActionCode` – 1 (buy), 2 (sell), 3 (close selected), 4 (close all), 0 when idle.
   - `LastTradePrice` – last seen execution price (updated both from best bid/ask and from `OnNewMyTrade`).

## Parameters

| Parameter | Description |
|-----------|-------------|
| `DefaultVolume` | Equivalent to `GetFloat(1)`; base volume used when external commands do not pass a custom size. Must be strictly positive. |
| `RequestBuy` | Toggle set by the external controller to request a long market order. Automatically resets to `false` once the order is submitted. |
| `RequestSell` | Toggle that submits a short market order. |
| `RequestCloseSelected` | When enabled, closes the portion of the position associated with `TargetOrderNumber`. |
| `RequestCloseAll` | Flattens any open position and clears the internal order registry. |
| `TargetOrderNumber` | Mirrors `GetInt(20)`. Identifies which entry to close when `RequestCloseSelected` is triggered. |
| `LastOrderNumber` | Read-only diagnostic that mirrors `SetInt(10, magn)` from MQL. |
| `LastActionCode` | Read-only diagnostic that mirrors `SetInt(11, direction)` (1 = buy, 2 = sell) and extends it with extra codes for close operations. |
| `LastTradePrice` | Read-only diagnostic mirroring `SetFloat(10, price)` from the original script. |

## Implementation notes

- Uses the StockSharp **high-level API only** (`StartProtection`, `SubscribeLevel1`, `CreateMarketOrder`, `RegisterOrder`). No low-level connector calls or manual collections are required.
- All logic runs on finished data and respects `IsFormedAndOnlineAndAllowTrading()` to avoid premature submissions.
- Order comments follow the pattern `HistoryTraining:Entry:<n>` / `HistoryTraining:Exit:<n>` so that fills can be inspected directly in the trade blotter.
- When switching direction (e.g., short &rarr; long), opposite-side entries are automatically removed from the registry because StockSharp accounts are netted. This matches the DLL behaviour where the counter order would close the previous trade before opening the new one.
- If `DefaultVolume` is invalid (non-positive), the command is skipped and a warning is logged, preventing infinite retry loops.

## Differences compared to the MQL version

- **Netted position model** – MetaTrader can keep multiple hedged tickets simultaneously. StockSharp strategies operate on a net position, so closing a "selected" entry reduces the aggregated exposure by the recorded volume. The README and logs make this behaviour explicit for the operator.
- **Shared variable bridge replaced by parameters** – Instead of `SharedVarsDLLv2.dll`, automation frameworks can toggle `Request*` parameters via the StockSharp UI, scripts, or tests.
- **Real-time diagnostics** – `LastTradePrice` is additionally updated from actual fills (`OnNewMyTrade`) so the UI always shows the executed price even if slippage occurs.
- **Timer-based polling** – replicates the DLL polling frequency without blocking StockSharp's message loop.

## Usage guidelines

1. Configure `DefaultVolume` and attach the strategy to the desired security/portfolio.
2. Connect the strategy to your control layer (GUI buttons, training environment, or automated tests) and toggle the `Request*` parameters to issue commands.
3. Monitor `LastOrderNumber`, `LastActionCode`, and `LastTradePrice` to confirm that each action was executed and acknowledged.
4. For partial exits, set `TargetOrderNumber` to the identifier returned after the entry command and enable `RequestCloseSelected`.
5. Use `RequestCloseAll` for emergency stops or to synchronize the strategy with the external trainer.

The Python version is intentionally omitted as requested. Only the C# implementation is provided in `CS/HistoryTrainingBridgeStrategy.cs`.
