# Every Nth Bar Filter Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Two exponential moving averages cross, and one side of the market is bought while the other is sold. The point of this diagram is what the averages are fed. Instead of reading every candle, they read one price out of every fifth candle, and the thinning is done by an N values block wired to count the candle stream against itself. Everything downstream — the averages, the crossing, the entries — lives on that slower clock, so the diagram looks at the market once per sampling window rather than once per bar.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed a converter that pulls the close price out of each candle, and an N values block that turns the same stream into a sampling pulse.
- The N values block takes the candle stream on both of its inputs at once: the trigger arms its countdown and the input counts it down, so it emits a pulse on every fifth finished candle and immediately arms itself again.
- That pulse is the trigger of a Variable that is holding the latest close price. The variable stores every close as it arrives but releases nothing until the pulse comes, so what leaves it is a thinned price series — one value per sampling window.
- Both moving averages read that thinned series instead of the candles, so a fourteen-period average spans seventy candles of market time and a forty-period one spans two hundred.
- A Crossing block watches the fast average against the slow one and speaks only at the moment the two swap places: true when the fast average crosses above, false when it crosses below. A logical NOT turns the downward case into a signal of its own.
- The Position block is measured against a zero variable by two comparisons — not long, and not short — and each result is joined to its crossing signal by a logical AND, so an entry asks for a fresh crossing and a position that is not already on that side.
- Both entry blocks are set to open only, so the diagram carries one position at a time and never adds to it; the order volume comes from a variable that is refreshed on every candle.
- The opposite crossing drives two close blocks, position protection is armed by every fill, and the chart panel draws the candles, both averages, the entry and exit orders, the protective orders and every fill.

## Entry and Exit Rules

- **Long entry**: The fast average crosses above the slow one on a sampling pulse and the position is not long. Position modify buys the order volume at market, opening only, so the entry is taken from a flat account and never stacks on an open trade.
- **Short entry**: The fast average crosses below the slow one on a sampling pulse and the position is not short. The logical NOT turns the downward crossing into a signal, and Position modify sells the order volume at market, opening only.
- **Exit**: There are two ways out. Position protection, armed by every fill, closes the trade at 1.5% profit or on a 1% stop. If neither is reached before the averages swap back, the opposite crossing takes over: it fires the close block for the side that is held, and that block sends the whole position at market. Because the entry blocks open only, the crossing that ends a trade does not open the opposite one — the next entry waits for the next crossing that arrives on a flat account.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the candles the whole diagram works on; the sampling window is counted in these candles. |
| Bars Per Sample | 5 | How many finished candles make one sample. Raise it and the averages see the market more rarely and trade less often; set it to one and the diagram becomes an ordinary crossover taken on every candle. |
| Fast EMA Length | 14 | Length of the fast average, counted in samples rather than in candles: at five candles per sample it covers five times that many candles of market time. |
| Slow EMA Length | 40 | Length of the slow average, in samples. Keep it well clear of the fast length, or the two lines swap places on noise and the crossings stop meaning anything. |
| Order Volume | 1 | Size of each entry order, in instrument units. The close blocks ignore it and send whatever the position holds. |
| Take Profit, % | 1.5 | Take-profit distance, in percent of the fill price. |
| Stop Loss, % | 1 | Stop-loss distance, in percent of the fill price. |

## Diagram Details

- The N values block is used here as a thinner rather than as a delay. Both of its inputs come from the same candle stream, so the countdown restarts the moment it expires and the pulse keeps landing on every fifth finished candle for the whole run.
- The Variable between the pulse and the averages is what makes the resampling real: its input accepts every close price, its output stays silent until the trigger arrives, and so the averages are handed one value per window and never see the candles in between.
- Both averages read the same variable, so they advance in step and the Crossing block can pair their values as they arrive. It reports only the sample on which the order of the two lines changed, which is why entries are impossible in the candles between samples.
- The position tests are written as not long and not short rather than as flat, so a crossing that arrives while the opposite side is still open still reaches the entry block; the opening-only setting is what holds the diagram to a single position, and the close blocks are what release it.
- Every fill, entries and exits alike, is joined by a Combination and sent into position protection, so the protective side always sees the position the account actually holds and stands down as soon as a crossing has closed the trade.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
