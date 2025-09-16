# MACD Alert Strategy

## Overview
This strategy is a faithful conversion of the MetaTrader expert advisor *MACD_Alert.mq4*. The original script evaluated the MACD main line calculated from 12/26 exponential moving averages with a 9-period signal line on five-minute bars. Whenever the main line was greater than `0.00060` or lower than `-0.00060` it displayed a platform alert. The StockSharp port keeps the logic intact by subscribing to the requested candle series, computing the MACD indicator with identical parameters, and writing alert messages into the strategy log whenever either threshold is crossed.

Unlike trading-focused bots, the module is purely informational: it does not place orders or manage positions. Instead, it acts as an early warning system for discretionary traders who want to be notified when momentum reaches predefined extremes.

## Conversion highlights
- Uses the high-level `SubscribeCandles` + `Bind` workflow so the MACD indicator receives properly aggregated data without manual buffering.
- Tracks only finished candles to mirror the original behaviour, which evaluated values on the last completed bar.
- Keeps the default parameters (12/26/9 MACD, ±0.00060 thresholds, M5 candles) while exposing them as tunable strategy parameters for optimisation or manual adjustments.
- Logs alerts through `AddInfoLog`, which makes them visible both in the GUI and in any hosted logging pipeline.
- Draws the MACD indicator alongside candles when charting is available so that the operator can visually confirm the threshold breaches.

## Parameters
| Parameter | Type | Description |
| --- | --- | --- |
| `MacdFastPeriod` | `int` | Fast EMA length used by the MACD calculation. Default `12`. |
| `MacdSlowPeriod` | `int` | Slow EMA length used by the MACD calculation. Default `26`. |
| `MacdSignalPeriod` | `int` | Signal smoothing length for the MACD indicator. Default `9`. |
| `UpperThreshold` | `decimal` | MACD value that triggers the bullish alert. Default `0.00060`. |
| `LowerThreshold` | `decimal` | MACD value that triggers the bearish alert. Default `-0.00060`. |
| `EnableAlerts` | `bool` | Enables or disables writing alert messages. |
| `CandleType` | `DataType` | Candle series on which the MACD is calculated. Defaults to five-minute candles. |

## Alert logic
1. `OnStarted` creates a `MovingAverageConvergenceDivergence` indicator configured with the selected periods and binds it to the candle subscription.
2. Every time a candle finishes the strategy reads the MACD main line from the indicator.
3. If the value is greater than or equal to `UpperThreshold`, the strategy writes `"MACD main line {value} exceeded the upper threshold..."` into the log.
4. If the value is less than or equal to `LowerThreshold`, it writes the analogous bearish message.
5. Signal-line and histogram values are still produced by the indicator but are ignored, matching the MetaTrader expert that only evaluated the main line.

Because the alerts are evaluated on completed candles, you receive one message per bar instead of one per tick. This avoids spamming the log while still replicating the intent of the original alerts.

## Usage workflow
1. Select the instrument to monitor and assign the desired `CandleType` (the default 5-minute bars match the MetaTrader script).
2. Adjust the MACD periods or thresholds if you want to react to different momentum levels.
3. Start the strategy. As soon as the MACD indicator is formed, alerts will be written whenever the configured limits are crossed.
4. Combine the log output with your existing notification pipeline (e.g., StockSharp log routing, e-mail forwarders, desktop notifications) if you need audible or visual cues.

## Example alert messages
```
MACD main line 0.00074 exceeded the upper threshold 0.00060 at 2024-05-13 09:25:00.
MACD main line -0.00068 fell below the lower threshold -0.00060 at 2024-05-13 11:40:00.
```
These messages correspond to the same conditions that triggered the MetaTrader `Alert()` calls in the original expert.

## Extending the module
- Couple the alerts with order placement by overriding `ProcessCandle` and adding trading rules, such as entering long when the bullish alert fires.
- Replace the static thresholds with values derived from volatility (e.g., ATR multiples) by updating the parameters from an external controller.
- Feed higher-timeframe candles into `CandleType` when you need macro momentum alerts, or switch to tick bars for faster notifications.
- Store the log output in a database or send it to messaging systems for archival and team-wide dissemination.
