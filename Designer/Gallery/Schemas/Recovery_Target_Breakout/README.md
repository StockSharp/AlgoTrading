# Recovery Target Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Entries here are about one thing only: a candle that is far wider than the market has been lately. The exit is about money rather than price — the open result of the position is measured against a target the diagram keeps in a variable, and that target is not a constant. A losing exit multiplies it, a winning exit puts it back to the base figure, so every trade knows what the previous one cost.

![schema](schema.svg)

## Strategy Overview

- Finished fifteen-minute candles feed four converters that pull the high, the low, the open and the close out of each bar.
- A formula subtracts the low from the high to get the range of the bar, while an Average true range indicator measures what the range has been over the last ten bars.
- A second formula multiplies the average true range by the breakout multiplier, and a comparison asks whether this bar's range is above that threshold — this is the whole definition of an abnormally wide bar.
- Direction is one comparison: close above open. A logical Not turns the same signal into the down-bar case, so both entries read the same candle body from opposite sides.
- Both entry gates are a logical And of three terms: the bar is wide, it points the right way, and the position is flat. Position modify then buys or sells at market with the order volume.
- P&L change supplies the open result of the position on every update, and two comparisons measure it against the money target and against the money stop.
- The money target is not a fixed number: a formula multiplies the base target by a recovery factor held in a variable, so the goal moves with the recovery state instead of being typed into the diagram twice.
- The recovery factor is rewritten by exactly two events, each through its own gate, and a Combination joins the two writes into the single input of the variable that stores it.

## Entry and Exit Rules

- **Long entry**: A finished candle whose high-to-low range exceeds the average true range times the breakout multiplier, closing above its own open, taken from a flat position. Position modify buys at market with the order volume.
- **Short entry**: A finished candle whose range exceeds the same threshold but which does not close above its own open, taken from a flat position. Position modify sells at market with the same order volume.
- **Exit**: There is no price stop and no price target in the diagram — the position is closed on money alone. When the open result reaches the current money target, one Position modify set to close the position fires; when it falls to the money stop, a second one fires. Each closing block owns one branch of the recovery latch: the fill of the losing close releases the grown factor, the fill of the winning close releases the figure one, and both writes meet in a Combination that feeds the variable holding the factor.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:15:00 | Time frame of the candles the whole diagram works on. |
| ATR Length | 10 | Number of bars the average true range is measured over; this is the yardstick a wide bar is judged against. |
| Breakout Multiplier | 1.5 | How many times wider than the average a bar has to be before it counts as a breakout. Raise it for rarer and more extreme entries, lower it for more of them. |
| Volume | 1 | Size of every entry order. All money figures below are results of this size, so changing one means re-tuning the others. |
| Target Base | 300 | Money target of a trade taken after a winning one, in the currency the result is counted in. |
| Recovery Multiplier | 2 | What the target is multiplied by after a losing trade. Two means the next trade has to earn back twice the base target; one turns the recovery off and leaves a plain money target. |
| Stop Money | -600 | Open result at which a position is abandoned, written as a negative number. It is a flat figure and is not scaled by the recovery factor. |

## Diagram Details

- The latch is the only loop in the diagram: the factor variable feeds a formula that multiplies it by the recovery multiplier, the result waits in a gate variable, and the gate writes it back into the factor variable when a losing close actually fills.
- Both gates are triggered by the fill of a closing block, not by the comparison that asked for the close. A comparison can repeat its verdict several times while the closing order is still in flight; a fill happens once, so the factor is multiplied once per losing trade.
- The factor variable takes its input without treating it as a trigger, so a write only changes what it stores. It emits on the trigger it is given, which is the P&L update, and that keeps both sides of the target comparison on the same clock.
- The money branch stays silent until the first position is opened, because the open result is only reported once there is something to value. From that point the factor, the base target and the recovery multiplier all release together on every update.
- The candles are subscribed as finished only, so every signal belongs to a bar that is already closed and the orders carry the time of that close rather than the time the bar opened.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
