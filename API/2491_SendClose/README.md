# SendClose Strategy

## Overview
SendClose is a fractal-based breakout strategy that recreates the behaviour of the original MT5 expert advisor. The algorithm continuously builds dynamic support and resistance lines by linking alternating fractal pivots and reacts the moment price revisits those projected levels. The StockSharp port keeps the core mechanics intact: trend lines are generated from alternating up/down fractal sequences, breakouts trigger market entries, and separate offset lines are used to force position liquidation.

## Fractal detection workflow
1. **Five-candle window** – the strategy keeps a rolling buffer of the latest five completed candles. As soon as the window is full, it evaluates the middle candle against the two older and two newer neighbours.
2. **Up fractal rule** – the central candle forms an up fractal when its high is greater than the highs of the two newer candles and strictly greater than the highs of the two older candles. This matches the MT5 `iFractals` logic (>= on the newer side, > on the older side).
3. **Down fractal rule** – similarly, the central candle is a down fractal if its low is lower or equal compared with the newer candles and strictly lower than the two older candles.
4. **Fractal queue** – every newly confirmed fractal is pushed into a six-element FIFO queue ordered from most recent to oldest. This queue is later scanned to find the required alternating patterns.

## Trend line construction
* **Sell line** – the algorithm looks for the most recent sequence *up fractal → down fractal → up fractal*. The line is drawn through the first and last up fractals, effectively connecting two swing highs separated by a swing low.
* **Buy line** – symmetrically, it searches for a *down fractal → up fractal → down fractal* chain and connects the surrounding swing lows.
* **Projection** – the stored endpoints (time and price) are used to interpolate or extrapolate the line value for any later timestamp. When the market reaches the projection at the current candle close, a trading decision is taken.
* **Close lines** – two auxiliary levels are calculated by shifting the sell line upward and the buy line downward by `LineOffsetSteps * PriceStep`. They act as forced-exit triggers just like the original Close1/Close2 lines.

## Trading logic
* **Entry conditions**
  * Sell when price touches the sell line and there is no conflicting long exposure. Existing short exposure can be increased until the `MaxPositions` limit is reached.
  * Buy when price touches the buy line and there is no conflicting short exposure. Existing long exposure can be increased up to the same limit.
* **Exit conditions**
  * Price touching any close line immediately closes the open position, emulating the MT5 behaviour where touching Close1/Close2 issues a full exit.
  * Entering signals attempt to flatten opposite positions before placing the new order, mirroring the hedging-to-netting adaptation inside StockSharp.
* **Touch detection** – tick precision from MT5 is approximated with candle data. A level is considered “touched” when it lies between the candle’s high and low prices.

## Parameters
| Name | Description |
|------|-------------|
| `EnableSellLine` | Enables or disables orders based on the upper (sell) fractal line. |
| `EnableBuyLine` | Enables or disables orders based on the lower (buy) fractal line. |
| `EnableCloseSellLine` | Toggles the Close1 level that closes positions when price rises above the sell line plus offset. |
| `EnableCloseBuyLine` | Toggles the Close2 level that closes positions when price falls below the buy line minus offset. |
| `MaxPositions` | Maximum number of lots that may remain open in one direction. Additional entries beyond this cap are ignored. |
| `OrderVolume` | Volume of each market order. The value should match the instrument contract size. |
| `LineOffsetSteps` | Offset, measured in price steps, used when computing Close1/Close2 levels. The default 15 replicates the `15*Point()` shift from MT5. |
| `CandleType` | Candle series used for analysis. Choose a timeframe that matches the chart you plan to trade (e.g., M15, H1). |

## Implementation notes
* The strategy runs on completed candles to respect the original EA, which relied on confirmed MT5 bars before evaluating fractals.
* Tick-level equality with bid/ask is approximated with candle ranges. If higher precision is required, feed tick data instead of candles.
* The `MaxPositions` parameter operates on the net StockSharp position. It is therefore suitable for netting accounts; hedging accounts can still simulate scaling by increasing `MaxPositions`.
* Close lines are evaluated before entries. If both an exit and an entry trigger on the same candle, the exit takes precedence, preventing conflicting orders.

## Usage guidelines
1. Configure the desired symbol and timeframe in your StockSharp terminal and ensure the instrument provides `PriceStep` information. The offset logic relies on it.
2. Adjust `CandleType` to match the timeframe you want to analyse. The default is 30 minutes, which offers a balance between noise and responsiveness.
3. Set `OrderVolume` to the position size you want to send per trade. For futures, use contract counts; for FX CFDs, use lot sizes.
4. Tune `LineOffsetSteps` to align with the instrument’s volatility. Larger offsets require a stronger move to trigger the Close1/Close2 exits.
5. Monitor the number of open lots when increasing `MaxPositions`. The strategy will not exceed this cap but may still pyramid positions in trending markets.

## Differences from the MT5 version
* StockSharp operates with net positions, so the code flattens opposing exposure before opening a new trade instead of maintaining simultaneous buy/sell tickets.
* Chart objects are not drawn automatically. If you need on-chart visualization, connect a chart module and plot the generated line values manually.
* Candle-based touch detection may fire slightly later than MT5 tick checks, especially on fast markets with wide candles.

## Risk management
The strategy places market orders without built-in stop-losses. Always complement it with external risk controls such as equity stops, trading hours filters, or manual supervision. Backtest extensively on the target instrument and timeframe before deploying live.
