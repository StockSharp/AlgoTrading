# Chart Parameters Diagnostics Strategy

## Overview
This strategy is a direct translation of the MetaTrader 4 script `ACB3.MQ4` (folder `MQL/8584`). The original script displayed diagnostic alerts that described the current chart configuration, such as the amount of history loaded, the index of the first and last visible bar and the vertical price range. The StockSharp version keeps the same intent: it subscribes to candles and writes a detailed summary of the "chart geometry" into the strategy log without sending any orders.

Unlike a trading robot, the strategy acts as a monitoring tool. It is useful while preparing a symbol for analysis, verifying that the backtesting window contains the expected amount of data or confirming that the price scale allows enough resolution. Because the diagnostic output is generated from the live candles, the report continuously reflects any changes in the window width, new history or the latest price extremes.

## Data flow and processing steps
1. When the strategy starts it reads the security metadata to obtain the number of price digits and the minimal price increment (quote point). These values are printed immediately, so an operator can confirm that the symbol is configured correctly.
2. The strategy creates a `Highest` and a `Lowest` indicator configured with the `VisibleBars` parameter. Both indicators evaluate the same incoming candle stream and keep track of the highest high and the lowest low inside the synthetic "visible" section of the chart.
3. A candle subscription is opened for the configured `CandleType`. Finished candles trigger the diagnostic routine.
4. For every completed candle the strategy:
   - Counts how many bars have been processed since the start.
   - Calculates the index of the first and the last bar inside the visible window (`VisibleBars` acts as the chart width) and computes the amount of shift that would be needed to fill the window completely.
   - Extracts the price range from the `Highest` and `Lowest` indicators and converts it into quote points to emulate the vertical resolution from MetaTrader.
   - Logs a multi-line report that mirrors the alert sequence of the original MQL script. Every line is in English and includes the latest numeric values formatted with the detected number of digits.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1 minute time frame | The candle series used to perform the diagnostics. Any supported time frame or candle type can be selected. |
| `VisibleBars` | `int` | 100 | Defines how many recent candles emulate the visible width of the chart. This replaces the `WindowBarsPerChart()` metric from MQL. |

Both parameters support optimization, which allows analysts to scan different window widths in backtests. The strategy does not place trades, so optimization mainly alters the context of the logged diagnostics.

## Logging output
The log reproduces the structure of the MetaTrader alerts:

```
Horizontal resolution: 100
Vertical resolution (points): 120
Summary:
Total bars processed: 523
Visible bars: 100
Last visible bar index: 522
First visible bar index: 423
Shift bars: 0
Horizontal:
Price range: 1.23450
Minimum price: 1.09500
Maximum price: 1.23450
Vertical:
Chart parameters updated.
```

Values are continuously refreshed as new candles arrive. The number formatting respects the detected precision of the security. When no price step is supplied by the symbol, the strategy falls back to the classic `10^-digits` quote point calculation used by the original MQL script.

## Usage recommendations
- Attach the strategy to the symbol you want to inspect and choose the same candle type that is used in your charting layout.
- Adjust `VisibleBars` to match the approximate number of bars that are simultaneously visible on your screen or in the backtesting panel.
- Monitor the log in the StockSharp GUI or in the file log. The strategy does not draw anything on the chart, but candle visualization is enabled automatically for convenience.
- Because no orders are sent, risk management and protection modules are not activated.

## Differences from the MQL version
- The MetaTrader script relied on UI functions such as `WindowBarsPerChart()` and `WindowPriceMin()`. In StockSharp the strategy calculates the same values through indicators and counters, which keeps it independent from a particular chart control.
- StockSharp logs replace the original alert pop-ups. This makes the diagnostics friendlier for automated monitoring and preserves a detailed history that can be exported.
- The calculation runs for every finished candle. Therefore the statistics automatically cover newly loaded history or live data, whereas the original script needed to be re-launched to refresh the numbers.

## Notes
- The strategy is deliberately passive: it never sends orders or changes portfolio state.
- The diagnostic output can be parsed by external tools or piped into monitoring dashboards.
- If `VisibleBars` is greater than the number of processed candles, the strategy still reports the current number of candles and indicates the missing amount via the `Shift bars` value, just like the original script.
