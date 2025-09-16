# MACD Pattern Trader Strategy

## Overview

The **MACD Pattern Trader** is a direct port of the MetaTrader 5 expert advisor *MacdPatternTraderAll*. The strategy merges six independent setups derived from the main line of the Moving Average Convergence Divergence (MACD) indicator. Each setup observes specific turning patterns inside the MACD sequence and opens a position with dynamically computed stop-loss and take-profit levels. The StockSharp implementation keeps the original risk handling features such as staged position reduction and the optional slow martingale lot progression.

The strategy operates on a single symbol and a configurable candle type. Every finished candle is evaluated and the active patterns may generate new entries. At most one net position is maintained at a time; partial exits reduce the volume but the strategy does not hold simultaneous long and short trades.

## Trading Logic

All six patterns inspect three previously finished MACD values (`macd[1]`, `macd[2]`, `macd[3]` in MetaTrader terms). The StockSharp version reproduces the same sequences using `MovingAverageConvergenceDivergence` indicators that provide the main line for each pattern.

* **Pattern 1 (A):** waits for the MACD to spike above the upper threshold and then roll over below the previous peak. A short trade is opened when the pullback is confirmed. The inverse behaviour around the lower threshold triggers a long trade.
* **Pattern 2 (B):** monitors sign flips around the zero line. When the MACD crosses back through zero while the magnitude continues to increase the strategy enters in the direction of the crossing.
* **Pattern 3 (C):** tracks double and triple tops/bottoms. Two successive corrections that fail to make a new extreme arm the pattern and the third failure opens the trade. This reproduces the nested `S3`, `stops3`, `stops13` and `sstops` flags from the original EA.
* **Pattern 4 (D):** captures single peaked reversals. The strategy stores the first peak above (or below) the threshold and fires when the following peak is smaller.
* **Pattern 5 (E):** implements the "reset" logic where a sharp movement above/below a reset level arms the pattern and a quick return through the trigger level opens a trade in the opposite direction.
* **Pattern 6 (F):** is a bar-counting pattern. It counts how many consecutive bars stay beyond the threshold and allows an entry once the counter exceeds the configured value without overshooting the maximum allowed bars.

Each pattern uses the helper methods `TryOpenLong` or `TryOpenShort` that compute:

* **Stop-loss:** highest high / lowest low of the last `StopLossBars` bars plus an additional offset in price steps.
* **Take-profit:** iterative search through blocks of historical highs or lows using the same algorithm as the original `TakeProfit` function.

The strategy opens market orders at the close of the finished candle and records the computed stop and target internally. On subsequent candles the price action is checked against these levels to trigger exits.

## Position Management

* The base trade volume is defined by `Initial Volume`. After every losing trade the lot size can be doubled when `Slow Martingale` is enabled. Profitable trades reset the lot size back to the base value.
* When a long position accumulates more than five price steps of profit and the previous candle close is above EMA(21), one third of the position is closed. If the move extends further and the previous high breaks above the average of SMA(98) and EMA(365), another half of the remaining position is closed. The short logic mirrors these rules to the downside.
* Stops and targets are recalculated at entry. When either level is violated the remaining position is closed immediately.

## Parameters

| Parameter | Description |
|-----------|-------------|
| **Candle Type** | Working timeframe and candle type. |
| **Time Filter** / **Start** / **Stop** | Optional trading session filter. |
| **Initial Volume** | Base position size. |
| **Min Volume** | Minimal tradable volume step. |
| **Slow Martingale** | Enables the slow martingale lot progression. |
| **Pattern N Enable** | Toggles a specific pattern. |
| **Pattern N Stop Bars / Take Bars / Offset** | Stop-loss lookback, take-profit lookback and stop buffer in price steps. |
| **Pattern N Fast EMA / Slow EMA** | MACD periods for the pattern. |
| **Pattern thresholds** | Upper/lower MACD trigger values exactly matching the original EA inputs. |
| **Pattern 6 Counters** | Number of bars required, maximum and minimum allowed counts. |
| **EMA/SMA Periods** | Periods for the management averages used by the staged exit rules. |

All numerical defaults match the original expert advisor so the StockSharp strategy behaves identically when the same market data is used.

## Usage Notes

1. Add the strategy to a `Strategy` container, configure the desired instrument and set the candle type.
2. Enable or disable individual patterns to focus on specific MACD setups.
3. Adjust the time filter to limit trading hours if required by the underlying market.
4. Review the stop and take-profit lookback windows; they should be adapted to the instrument volatility and timeframe.
5. Monitor the strategy log for messages such as `PatternX: buy` or `PatternX: sell` that document every executed signal and the calculated protective levels.

## Differences from the MetaTrader 5 Version

* The StockSharp port places market orders and monitors stop/target levels internally because protective orders are not attached automatically as on MetaTrader hedging accounts.
* Volume rounding uses the instrument `VolumeStep` when available; the minimal step falls back to `0.01` lots.
* Partial exits and the slow martingale cycle are recalculated using the same thresholds but executed on finished candles.

The overall behaviour and the parameter set remain faithful to the MetaTrader 5 strategy, providing a familiar workflow for traders migrating to StockSharp.
