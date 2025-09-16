# Custom Chart Monitor Strategy

## Overview
The **Custom Chart Monitor Strategy** replicates the behavior of the MetaTrader 5 expert advisor *Example1_LibCustomChart.mq5*. The original script demonstrates how to work with an auxiliary library that exposes custom chart data. This StockSharp version focuses on monitoring candle updates that arrive from any configured data source, printing the active bar's close price and signalling when a new bar appears.

Unlike conventional trading strategies, this example is intentionally passive. It does not submit orders and instead shows how to integrate external or synthetic chart feeds into a StockSharp strategy. The code is designed to serve as a template for more complex processing of custom candle streams.

## Original MQL Logic
The MQL5 expert advisor performs the following tasks:
1. Loads the `LibCustomChart` library that either connects to a custom chart or falls back to the standard chart if the library is unavailable.
2. Refreshes the chart data on every tick.
3. Reads the close price of the most recent (unfinished) bar.
4. Prints the close value and notifies the user when a new bar is detected by the library.

The StockSharp implementation mimics this flow by relying on candle subscriptions and event handlers instead of direct library calls.

## Strategy Logic in StockSharp
1. A `StrategyParam<DataType>` parameter allows the user to specify which candle series represents the custom chart. By default a one-minute timeframe is used.
2. During startup the strategy subscribes to the requested candle type by calling `SubscribeCandles()` and attaches an event handler through the high-level `Bind` API.
3. Every time a candle update is received, the handler logs the current close price, ensuring that repeated identical updates are ignored.
4. When a candle is finalized (`CandleStates.Finished`), the strategy writes an additional log entry announcing the new bar, replicating the `CustomChartNewBar()` behavior from the MQL example.

This structure provides a clear template for reacting to non-standard candle feeds supplied by indicators, synthetic sources, or external adapters.

## Parameters
| Name | Type | Description |
| ---- | ---- | ----------- |
| `Candle Type` | `DataType` | Defines which candle series will be treated as the custom chart. Choose any timeframe or candle builder supported by your connector. |

## Usage Guidelines
1. Configure the `Security` and `Candle Type` parameters before starting the strategy. The selected candle data source should represent the custom chart you want to monitor.
2. Launch the strategy. It will immediately subscribe to the candle series and start logging the latest close values in the strategy journal.
3. Observe the log messages. Each update prints the close price of the forming bar. When a bar is completed, an additional "New bar detected" message appears with the timestamp of the bar.
4. Extend the handler methods if you need to add calculations, trigger trading decisions, or forward the information to other components.

## Extending the Template
- Replace the logging statements with indicator calculations or data routing to transform the monitored chart into actionable signals.
- Combine the strategy with other indicators or market data feeds if the custom chart should be compared with standard candles.
- Add more parameters for configuring thresholds, message verbosity, or automatic order placement based on the observed candles.

## Notes
- No Python version is provided for this strategy as requested.
- The example does not place trades; it is intended purely for data monitoring and demonstration purposes.
- Ensure that your connector delivers the candle type you configure, otherwise the strategy will remain idle.
