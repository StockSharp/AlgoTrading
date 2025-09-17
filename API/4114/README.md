# Indicator Parameters Demo Strategy

## Overview

`IndicatorParametersDemoStrategy` is a direct adaptation of the MetaTrader sample *IndicatorParameters_Demo.mq5* for the StockSharp high level strategy API. The original Expert Advisor listened for chart events and printed a detailed description of every indicator added to or removed from the chart. This C# version reproduces that behaviour inside the strategy context by tracking all indicators bound to the strategy and dumping a formatted parameter snapshot into the log when indicators are added, removed, or refreshed.

Unlike most strategies, this sample does not place any orders. It is intended as a diagnostic and educational tool that shows how to introspect indicators programmatically: enumerate their configurable properties, observe their runtime values, and understand when values become available. The strategy is therefore safe to run on any account because no trading instructions are sent to the broker.

## Key Features

- Instantiates three commonly used indicators (simple moving average, exponential moving average, and relative strength index) to demonstrate parameter extraction.
- Automatically records a metadata line whenever an indicator is added (`+ added`) or removed (`- deleted`) from the internal tracker.
- Builds a structured report that contains the indicator type plus the name, type and current value of each readable parameter.
- Provides a public helper method `RefreshIndicatorSnapshots` that can be invoked from the UI to regenerate the parameter report on demand.
- Optional logging of indicator values on every finished candle via the `LogIndicatorValues` parameter. When disabled, only structural events are reported.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Candle type (time frame or custom data source) that feeds all tracked indicators. Configure this to match the data stream you want to inspect. | 1-minute time frame |
| `SmaLength` | Lookback length of the demo simple moving average. The value is logged as part of the indicator snapshot so you can observe how the property travels through the reporting pipeline. | 20 |
| `EmaLength` | Lookback length of the demo exponential moving average. | 50 |
| `RsiLength` | Period of the relative strength index used to illustrate oscillators. | 14 |
| `LogIndicatorValues` | When `true`, the strategy writes `[timestamp] indicator value: X` messages after every finished candle for each tracked indicator. When `false`, only parameter changes are reported, which keeps the log compact. | `false` |

All parameters are built with `StrategyParam<T>` to support optimisation, UI binding, and validation. Feel free to adapt the demo by inserting additional indicators or exposing extra parameters – the tracking subsystem will automatically include them in the report as long as you call `TrackIndicator` for the new instance.

## Logging Behaviour

1. **Startup** – during `OnStarted` the strategy creates the indicators, calls `TrackIndicator`, and produces entries like `+ added: name=SMA(20), type=SimpleMovingAverage`. Immediately afterwards a detailed snapshot is logged, for example:

   ```
   SMA(20) parameter snapshot:
       Length (Int32) = 20
       IsFormed (Boolean) = False
       LastValue (Decimal?) = null
   ```

   The exact contents depend on the indicator class – the helper inspects all readable public properties whose type is primitive, `decimal`, `string`, `TimeSpan`, `DateTime`, `DateTimeOffset`, `DataType`, or any enum.

2. **Runtime updates** – if `LogIndicatorValues` is enabled, the strategy records the value delivered by the subscription after every finished candle, tagged with the candle close time in ISO 8601 format. This makes it easy to correlate parameter changes with market data.

3. **Manual refresh** – calling `RefreshIndicatorSnapshots()` (for example from a button bound in the UI) iterates over every tracked indicator and re-emits the parameter report. This mirrors the original MetaTrader script that could be triggered by chart events.

4. **Reset** – the override of `OnReseted` clears the tracker and logs a `- deleted` entry for each indicator, demonstrating how to react to removal events.

## Usage Steps

1. Assign a security and portfolio to the strategy as usual.
2. Optionally adjust the candle type and indicator lengths in the parameter grid.
3. Decide whether value-level logging is required and toggle `LogIndicatorValues` accordingly.
4. Start the strategy. Observe the log to see the automatically generated indicator descriptions.
5. (Optional) Call `RefreshIndicatorSnapshots` at any time to produce a fresh dump of the current indicator state.

Because the strategy does not submit orders, no trading-specific preparation is required. It is perfectly suitable for both live and simulated connections when you only need to inspect indicator configuration.

## Differences from the MQL Version

- The original script reacted to `CHARTEVENT_CHART_CHANGE`. StockSharp strategies do not receive chart-level events, so indicator additions are handled explicitly through the `TrackIndicator` helper that should be called whenever an indicator is created.
- Indicator handles are not exposed in the same way as in MetaTrader. Instead of raw handles and parameter arrays, this port uses reflection to inspect strongly typed properties of each `IIndicator` implementation.
- Logging is integrated with the standard strategy logging subsystem (`AddInfoLog`) instead of `Print` statements.

## Extending the Demo

To analyse additional indicators, simply create them in `OnStarted`, bind them to the candle subscription, and pass them to `TrackIndicator`. The tracker stores a human-friendly alias for each indicator, so choose descriptive names (for example, include the length or source). The helper methods will automatically pick up the new indicators when you refresh snapshots or when the strategy resets.

If you need to monitor parameter changes at runtime (for example after modifying a property interactively), call `RefreshIndicatorSnapshots` to get an updated report without restarting the strategy.

## Safety Notes

- No market or limit orders are sent under any circumstance.
- The strategy depends on the high level candle subscription mechanism. Ensure the selected `CandleType` is supported by your data connection.
- Extensive logging may impact performance if `LogIndicatorValues` is enabled on very small time frames. In such cases prefer manual refreshes over per-candle logging.
