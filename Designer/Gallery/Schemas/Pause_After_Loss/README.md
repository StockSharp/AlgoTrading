# Pause After Loss Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A plain momentum diagram carries a rule that decides when it is allowed to trade at all. The P&L change block reports the realized result of the account, so every closed trade can be read as a win or a loss without measuring prices. Losses that follow one another are counted, and when the count reaches its limit a Flag block is set and holds trading shut for a fixed number of candles. Entries resume only after the countdown ends and the flag is cleared.

![schema](schema.svg)

## Strategy Overview

- Finished hourly candles feed a one-period Rate of Change, which is the percentage move of the closing price against the previous close, so one indicator carries both the entry threshold and the exit threshold.
- Two comparisons read that percentage against two variables: an upper threshold for an upward move and a lower, negative one for a downward move.
- The Position block is compared with zero three times — equal, greater and less — which gives a flat test for entries and a long and a short test for exits.
- An entry is a logical AND of three signals: momentum in the wanted direction, a flat position, and no pause running. Both entry blocks are set to open only, so the diagram holds one position at a time and never adds to it.
- An exit is a logical AND of momentum in the opposite direction and a position on that side; it triggers a Position modify block set to close, which works the volume out from what is held.
- The P&L change block reports the realized result. A previous-value block keeps the figure that stood before the latest change, and two comparisons say whether the result went down or up, which is a losing or a winning closed trade.
- A loss makes the stored streak pass through a formula that adds one and writes the sum back into the same variable; a win writes zero over it. A comparison of the new count against the limit is the signal that starts a pause.
- That signal sets a Flag and arms an N values block counting finished candles. While the flag is set, a stored state variable read on every candle reports "paused", a logical NOT turns it into "allowed again", and that is the third input of both entry gates.

## Entry and Exit Rules

- **Long entry**: The Rate of Change of the finished candle is above the long threshold, the position is flat, and no pause is running. Position modify buys the order volume at market, opening only.
- **Short entry**: The Rate of Change of the finished candle is below the short threshold, the position is flat, and no pause is running. Position modify sells the order volume at market, opening only.
- **Exit**: A position is given up as soon as momentum turns against it: a long is closed when the Rate of Change falls below the short threshold, a short when it rises above the long threshold. The block is set to close position, so the order volume comes from the position itself and the diagram never flips from one side to the other in a single order — the opposite side can only be opened by a later candle, from flat. Exits are never held back by the pause; only entries are.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 01:00:00 | Time frame of the candles the whole diagram works on. The pause is counted in these candles as well, so a longer candle makes the pause longer in clock time. |
| Rate Of Change Length | 1 | How many candles back the Rate of Change measures. At one it is the percentage move from the previous close, which is what both thresholds are written against; a longer length turns it into a broader momentum measure and the thresholds have to be widened with it. |
| Long Threshold, % | 0.3 | Percentage move that opens a long and closes a short. Raising it makes both rarer and the diagram more selective. |
| Short Threshold, % | -0.3 | Percentage move that opens a short and closes a long, written as a negative number. It does not have to mirror the long threshold; asymmetric values bias the diagram towards one side. |
| Order Volume | 1 | Size of each entry order, in instrument units. Exits take their volume from the position, so this value is not repeated there. |
| Consecutive Losses | 3 | How many closed trades in a row have to lose before trading is suspended. A winning trade puts the count back to zero, so this counts a run and not a total; at one, every losing trade starts a pause. |
| Pause Candles | 8 | How many finished candles a pause lasts. Entries are refused for the whole count, after which the flag is cleared and the loss count is put back to zero. |

## Diagram Details

- Nothing on the trading side feeds the pause: the streak counter and the flag are driven only by the account result, so the diagram has no loop and the pause can only ever take permission away, never grant it.
- The Flag block emits only at the moment it is first set, and the N values block ignores a trigger while it is already counting, so a further loss signal during a running pause neither restarts nor extends the countdown.
- The countdown is measured in finished candles of the trading time frame, not in account events, so a quiet stretch and a busy one produce a pause of the same length.
- The entry gates read the pause from a stored variable rather than from the flag itself. The flag reports a moment; the variable holds a state — written true when the flag is set, written false when the countdown ends, and emitted on every candle, so both gates always have a fresh value to combine with the other two signals.
- The chart panel draws the candles, the Rate of Change, the realized result, the entry and exit orders and every fill, so a stretch where signals were reached but no order followed is easy to recognise as a pause.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
