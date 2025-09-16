# Simple Trade Strategy

## Overview
This strategy is a C# conversion of the MetaTrader 5 expert advisor "SimpleTrade (barabashkakvn's edition)". It compares the opening price of the current bar with the opening price three bars ago. If the current open is higher, the strategy goes long; otherwise it goes short. Every position is held for only one completed candle and is secured with a fixed stop-loss distance expressed in pips.

The StockSharp implementation subscribes to the selected candle series through the high-level API and reacts only to finished bars, ensuring that decisions are based on completed price data. Positions are closed at the next bar transition or earlier if the stop level is touched within the bar range.

## Trading Logic
- **Entry**
  - On each completed bar, store its opening price and maintain a rolling history of the last four opens.
  - When there is no open position and at least four opening prices are available, compare the latest open with the one recorded three bars earlier.
  - Enter a long position if the current open is above the open three bars ago; otherwise enter a short position.
- **Exit**
  - Every trade is protected by a stop level calculated as *StopLossPips × pip size* from the entry open price.
  - On the following bar the position is closed regardless of outcome, replicating the original expert advisor that never holds a trade longer than one candle.
  - If the bar's high (for shorts) or low (for longs) penetrates the stop level, the strategy attempts to close the position immediately at market.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `StopLossPips` | 120 | Distance from the entry open price to the protective stop, measured in pips. The code reproduces the MetaTrader behaviour by multiplying the price step by 10 for symbols quoted with 3 or 5 decimals. |
| `TradeVolume` | 1 | Order volume used for market entries. Adjust it to align with the contract size of the traded instrument. |
| `CandleType` | 1 hour time frame | Specifies which candle series the strategy subscribes to. Select the timeframe that corresponds to the chart used in MetaTrader. |

All parameters are exposed as `StrategyParam<T>` objects so they can be optimised or changed through the graphical interface.

## Implementation Notes
- The rolling history of four opening prices is maintained without collections to comply with repository guidelines.
- Stops are not submitted as separate orders; instead the logic checks candle ranges and issues a market exit when the stop level would have been triggered.
- Because StockSharp processes positions asynchronously, the strategy exits an existing trade before evaluating a new entry signal. In live trading, this mirrors the original "close then reopen" sequence while avoiding overlapping orders.
- Pip size is derived from `Security.PriceStep`. For 5-digit or 3-digit symbols the step is multiplied by ten so that a pip matches the MetaTrader definition.

## Usage Tips
- Run the strategy on instruments with consistent tick sizes where pip-based stops are meaningful (for example, major Forex pairs).
- Optimise the `StopLossPips` value per instrument; large values widen the protective buffer, while smaller values make the strategy more sensitive to intrabar noise.
- Ensure the brokerage connection sends candle updates with final states so that the strategy receives the correct open prices.

## Risks and Limitations
- Holding trades for only one bar means the strategy relies heavily on the chosen timeframe. Backtesting different candle durations is essential.
- Using candle extremes to emulate stop execution introduces slippage in volatile markets compared to native stop orders.
- The strategy always stays in the market (either long or short) after the first four bars of data, which may generate frequent trades in sideways markets.
