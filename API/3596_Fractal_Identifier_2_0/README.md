# Fractal Identifier 2.0 Strategy

## Overview
The **Fractal Identifier 2.0 Strategy** is a direct port of the MetaTrader 5 script "Fractal Identifier 2.0". The original indicator continuously scanned the built-in fractal buffer and printed the latest confirmed upper fractal in the chart comment. This StockSharp version keeps the behaviour by monitoring the configured candle series and logging the most recent fractal high through the strategy diagnostic stream.

The strategy does **not** place orders. It is intended as an analytical helper that highlights when a new bullish fractal has been confirmed. The high-level StockSharp API is used to subscribe to candles, making the component easy to embed in larger trading workflows or dashboards.

## Operating Principle
1. Subscribe to the primary candle series defined by the `CandleType` parameter.
2. Maintain a rolling buffer with the highs of recently finished candles.
3. Once at least five candles are available, scan the buffer from the newest confirmed centre towards older bars to detect the first fractal that satisfies the classic Bill Williams pattern (the high is greater than the highs of the two preceding and two following candles).
4. Whenever a new fractal is found, emit an informational log line with the detected price level.

This logic reproduces the MQL5 script that repeatedly called `CopyBuffer` on the fractal indicator and printed the newest non-empty value.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `LookbackBars` | Number of recently completed candles that are scanned for a confirmed fractal high. Increasing the value searches deeper into history before reporting a level. | `10` |
| `CandleType` | Candle series used to evaluate fractals. Change this to match the timeframe of interest (e.g., 5-minute candles). | `1 minute time frame` |

## Output
- When a fractal high is detected the strategy calls `AddInfoLog("Most recent fractal high: {price}")`. The log entry mirrors the comment text produced by the original MetaTrader script.
- The included chart area overlays the subscribed candle series, making it easy to visualise when the logs were generated.

## Notes
- Only upper fractals are evaluated, matching the behaviour of the MT5 source.
- A fractal requires two additional completed candles to be confirmed. Therefore, very recent highs may be reported with a short delay while the pattern finalises.
- The component can be embedded into other strategies to trigger alerts, draw levels, or feed rule engines using the logged price.
