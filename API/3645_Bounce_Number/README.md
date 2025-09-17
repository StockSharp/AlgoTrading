# Bounce Number Strategy

## Overview
The **Bounce Number Strategy** is a StockSharp port of the MetaTrader indicator `BounceNumber_V0.mq4` / `BounceNumber_V1.mq4`. The original tool was a visual analyzer that counted how many times price touched a symmetric channel before breaking out of it. This C# strategy recreates the bounce counter with the high-level API, stores the results in a distribution table, and reports every completed cycle through the strategy log. The implementation stays faithful to the MetaTrader logic while adapting it to StockSharp's event-driven pipeline.

Unlike the original indicator, the port runs as a strategy component. It subscribes to finished candles, monitors band touches, and tracks how many alternating hits occur before price exits the channel by twice its half-width. The collected statistics can be consumed from the `BounceDistribution` property or from the generated log messages.

## How it works
1. When the strategy starts it validates that the instrument exposes a non-zero `PriceStep`. Point-based inputs rely on this value to convert MetaTrader "points" into decimal price distances.
2. A candle subscription created from `CandleType` feeds the bounce analyzer with completed bars only.
3. The first incoming candle defines the channel center (its close price). A symmetric band whose half-width equals `ChannelPoints * PriceStep` is created around that center.
4. Every new finished candle increments the cycle counter and is evaluated with three rules:
   - **Breakout detection**: if the candle's range crosses `center ± 2 * halfWidth`, the current cycle ends and its bounce count is recorded.
   - **Lower band touch**: if the candle spans the lower band and the previous touch was not also a lower band touch, the bounce counter increases by one and direction switches to "lower".
   - **Upper band touch**: symmetric rule for the upper band.
5. If a cycle lasts more candles than `MaxHistoryCandles` (and the parameter is positive) the channel is forcefully reset, ensuring the histogram is updated even when price drifts sideways forever.
6. On every cycle reset the distribution dictionary is updated and an information log is produced, mirroring the behaviour of the original interface counters.

The strategy does not place any orders by design. It should be hosted alongside other components (dashboards, UI, data exporters) that consume the `BounceDistribution` statistics.

## Parameters
| Name | Type | Default | MetaTrader analogue | Description |
| --- | --- | --- | --- | --- |
| `MaxHistoryCandles` | `int` | `10000` | `maxbar` input | Maximum number of candles allowed inside one cycle before a forced reset. Set to `0` to disable the safety reset. |
| `ChannelPoints` | `int` | `300` | `BPoints` input | Half-width of the bounce channel expressed in price points (`PriceStep` multiples). |
| `CandleType` | `DataType` | `M1` timeframe | `TF` input | Candle series used for the bounce calculations. |

## Differences vs. MetaTrader code
- The histogram is stored as a dictionary instead of on-chart text objects. This makes the information easier to export or visualize in StockSharp dashboards.
- UI-specific inputs from the indicator (colours, fonts, buttons) are removed because they were cosmetic and have no impact on the analytical logic.
- The forced reset by `MaxHistoryCandles` is now optional (`0` disables it) and works on live data streams, whereas MetaTrader processed a finite historical block.
- All informative messages are written in English through `AddInfoLog`, matching the requirement for English-only code comments/logs.

## Usage tips
- Ensure that the selected security defines `PriceStep`; otherwise, the strategy throws an exception on start because point-based offsets cannot be calculated.
- Combine the strategy with custom UI widgets or scripts that read `BounceDistribution` to replicate the MetaTrader grid of counts.
- Use smaller values for `ChannelPoints` when analysing intraday noise and larger values for higher timeframes or volatile instruments.
- To emulate the historical scan from the MQL version, start the strategy with `HistoryBuildMode` enabled in your connector and let it process the requested historical range; the distribution will be populated as soon as the backfilled candles are delivered.
