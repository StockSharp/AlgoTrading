# Account Rules Guard Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram trades EMA(120)/EMA(450) crossovers on finished one-minute candles, and wraps that ordinary entry in an account-level supervisor. P&L change, a formula, two comparisons, a logical OR, a Flag latch and a stored flag together watch the combined money result of the run. The moment that result reaches the loss limit or the profit target, the supervisor closes the position, writes what happened to the log, and blocks every further entry until the run ends.

![schema](schema.svg)

## Strategy Overview

- Finished one-minute candles feed two Indicator blocks, a fast EMA and a slow EMA, and two Crossing blocks read that pair in both directions.
- P&L change reports realized and unrealized money on the same update, and a Formula adds the two into one combined result that is recomputed on every P&L update.
- Two Comparison blocks test the combined result against the Max Loss level and the Profit Target level, and a Logical condition with the OR operator turns either answer into a single rule-tripped signal.
- Flag latches that signal the first time it is true. Its reset input is deliberately left unconnected, so the supervisor is a one-way switch for the rest of the run.
- A Variable of flag type stores the latch state and re-emits it on every candle, which is what a logical AND needs to work with; a Logical condition with the NOT operator turns the stored state into the permission the entry gates read.
- Each entry gate is a logical AND of three things: a crossing in its direction, the supervisor permission, and a position check built from Current position and a Comparison against zero.
- Position modify opens one Volume at market on the open-position condition, so an entry is taken only from flat; the opposite crossing feeds a second Position modify that closes what is open and leaves the diagram flat instead of reversing it.
- When the rule trips, a third Position modify flattens the position, a Variable snapshots the result at that instant, String formatter renders it, and Notification writes it to the log together with a second line describing the platform trading permission.

## Entry and Exit Rules

- **Long entry**: A finished candle on which the fast EMA crosses above the slow EMA, with the supervisor not tripped and the position not long, buys one Volume at market. The open-position condition means the entry is taken only from flat: the same signal arriving while a position is already open is refused rather than added to.
- **Short entry**: A finished candle on which the fast EMA crosses below the slow EMA, with the supervisor not tripped and the position not short, sells one Volume at market. As with the long side, the open-position condition admits the entry only from flat.
- **Exit**: The ordinary exit is the opposite crossing: the closing Position modify block flattens whatever is open, so the diagram returns to flat and waits for a fresh crossing rather than turning the position around. The emergency exit is the supervisor: as soon as the combined realized and unrealized result reaches the Max Loss level or the Profit Target level, the position is closed at market, the latch is set, the amount and the platform permission are written to the log, and no further entry is admitted for the rest of the run.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:01:00 | Time frame of the candle series. Only finished candles drive the moving averages, the re-emitted latch state, the volume pulse and the permission read. |
| Fast EMA Length | 120 | Length of the fast exponential moving average. |
| Slow EMA Length | 450 | Length of the slow exponential moving average. |
| Max Loss | -5000 | Combined realized and unrealized result, in the money of the account, at or below which the supervisor trips. It is written as a negative number and is deliberately wide: a limit set too close stops the diagram before it has traded enough to show anything. |
| Profit Target | 10000 | Combined realized and unrealized result at or above which the supervisor trips. Reaching it ends the run in the same way a loss does: position closed, latch set, no further entries. |
| Volume | 1 | Fixed quantity used by both entry blocks. The two closing blocks take their amount from the open position and ignore this value. |

## Diagram Details

- [P&L change](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) emits realized and unrealized money together, and the [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) `r + u` adds them into the value both [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) blocks judge. The block stays silent until the account has actually moved, so the supervisor cannot trip before the first fill, and the two limit Variables are triggered by the combined result itself so that both sides of each comparison always arrive on the same update.
- The permission is stored, not streamed. [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) emits only at the instant it is set, which a logical AND cannot use, because AND waits for a value on every input and clears them once it fires. The latch state therefore lives in a [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) of flag type whose default is false and whose Trigger input is the candle stream: every candle it re-emits the current state, and a [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) with the NOT operator turns it into the entry permission.
- The OR operator on the tripped signal is a deliberate choice: unlike AND it does not wait for a value on every input, so either limit alone can raise it. It also emits a false answer on every quiet update, which costs nothing downstream: Flag ignores a false trigger, the snapshot Variables ignore it, and [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) refuses to act on it, so no order is ever sent by a negative answer.
- The closing blocks use the close-position condition and need no volume input at all: the amount is taken from the position that is open. The entry blocks keep their own Volume, and [Current position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) compared against zero gives each gate the same not-long and not-short checks the entry rule states.
- [Is trade allowed](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html) reads the platform's own permission on every candle, and it is kept out of the entry gates on purpose: on recorded history it answers "not allowed" for the whole run, so a gate built on it would never open and the diagram would not trade at all. Its answer is captured in a Variable and rendered by [String formatter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) into the second [Notification](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) line, which is where it belongs: it explains the state of the platform at the moment the rule tripped instead of silencing the diagram. Notification is set to the log type, the only type that is delivered while history is being replayed.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
