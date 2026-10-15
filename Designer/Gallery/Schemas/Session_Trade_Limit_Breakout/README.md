# Session Trade Limit Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A breakout is worth taking only when the market has been quiet, only inside the hours the diagram is allowed to trade, and only once until the right to trade is handed back. The Flag block is what enforces that last part: it lets the first qualifying breakout through as a single pulse and then stays shut, so a run of strong candles produces one entry instead of one order per bar.

![schema](schema.svg)

## Strategy Overview

- Everything downstream runs on finished thirty-minute candles, so no decision is ever made on a bar that is still forming.
- A Highest block over twenty candles marks the top of the recent range, and a Previous value block takes that level from one candle back — the level is therefore fixed before the candle that has to beat it.
- An Average Directional Index of length fourteen is reduced to its own line by a converter, and a comparison keeps entries to a market that is still calm: the ADX line below its limit.
- Working time answers, candle by candle, whether the moment falls inside the trading window, and a logical NOT of the same answer is what marks the end of the session.
- A logical AND collects four answers into one gate: the close is above the breakout level, the ADX line is under the limit, the moment is inside the window, and the position is flat.
- The Flag block turns that gate into a ticket. The first true answer is passed on as a single pulse and triggers a market buy through Position modify with the open-position condition; every later answer is swallowed while the ticket is spent.
- Two things hand the ticket back: a delay block that counts fifteen finished candles after the entry fill, and the first candle that prints outside the trading window.
- The exit is built from the same two readings that allowed the entry — a stretched ADX limit and a level slightly under the breakout level — joined by a logical OR into one closing trigger, with Position protection running underneath as a fixed take profit and stop loss.

## Entry and Exit Rules

- **Long entry**: The candle closes above the highest high of the previous twenty candles, the ADX line is below the calm market limit, the candle belongs to the trading window, and the position is flat. All four arrive on the same candle, the logical AND turns them into one true answer, and the Flag passes the first such answer to Position modify, which buys the order volume at market.
- **Short entry**: There are no short entries. The diagram is deliberately one-sided: the breakout it looks for is an upward one, and a fall back through the give-back level is read as a reason to leave a long rather than as a reason to sell.
- **Exit**: Two readings end the trade, and the logical OR means whichever comes first is enough. The first is a market that stopped being calm: a formula multiplies the calm market limit by the trend multiplier and a comparison checks whether the ADX line has reached that stretched level. The second is a breakout that gave itself back: a second formula takes the breakout level down by the give-back factor and a comparison checks whether the close has fallen under it. Either one triggers a Position modify set to close the position. Underneath both, Position protection watches the entry fill and closes the trade at two percent profit or one percent loss.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:30:00 | Time frame every block in the diagram works on; only finished candles are processed. |
| Breakout Length | 20 | Number of candles the Highest block looks back over to build the breakout level. |
| ADX Length | 14 | Period of the Average Directional Index whose line is used as the calm market filter. |
| Calm Market Limit | 25 | Value the ADX line has to stay under for a breakout to count as one out of a quiet market. |
| Session From | 12:00:00 | Start of the trading window; a candle before it cannot open a position. |
| Session Until | 21:00:00 | End of the trading window; the first candle after it hands the entry ticket back. |
| Cooldown Candles | 15 | Finished candles that have to pass after an entry fill before the entry ticket is handed back. |
| Order Volume | 1 | Order size, in lots. |
| Trend Multiplier | 1.5 | Multiplier applied to the calm market limit to get the ADX level that closes the trade. |
| Give-Back Factor | 0.98 | Fraction of the breakout level that the close has to fall under for the give-back exit. |
| Take Profit, % | 2 | Take-profit distance, in percent of the entry price. |
| Stop Loss, % | 1 | Stop-loss distance, in percent of the entry price. |

## Diagram Details

- The flat-position check inside the entry gate carries real weight. Without it, a breakout arriving while the trade is already open would spend the ticket on an order the open-position condition would refuse, and the diagram would then sit through the whole cooldown for nothing.
- The Flag emits only at the moment it is set, so its output goes straight into the order trigger and never into the logical AND — it is a pulse, not a level, which is why every condition is collected before it rather than beside it.
- The delay that measures the cooldown is armed by the entry fill and fed by the candle series, so the fifteen values it counts are fifteen finished bars.
- The Highest block reads the high of each candle, so the level being broken is the highest high of the last twenty bars, not the highest close, and the breakout is correspondingly stricter.
- Take profit and stop loss sit beside the two exit comparisons rather than instead of them: they are the fallback for a trade that drifts without either reading firing.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
