# Average Pip Movement Tick Seconds Strategy

## Overview
The **AveragePipMovementTickSecondsStrategy** replicates the monitoring logic of the original MQL expert advisor "Average Pip Movement based on Tick & Seconds" using the StockSharp high-level API. The strategy continuously observes bid/ask updates, converts the tick-by-tick changes into pip units, and reports the rolling averages of both price movement and spread. It does **not** place any orders; the implementation is focused on real-time analytics and logging so that traders can evaluate short-term volatility and execution costs.

## Data subscriptions and indicators
- Subscribes to Level1 data for the selected security. Bid and ask prices are processed on every update.
- Uses two `SimpleMovingAverage` indicators:
  - One processes the absolute difference between consecutive bid prices (converted into pips) to compute the average pip movement per tick.
  - The other consumes the instantaneous bid/ask spread (also in pips) to deliver the average spread per tick.
- Indicator results are evaluated only when they are fully formed, mirroring the original buffer-based calculations.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `MaxTicks` | `100` | Number of recent ticks considered for both the pip-movement and spread averages. A value of 100 means the strategy analyses 99 consecutive bid differences and 100 spread measurements. |
| `CheckIntervalSeconds` | `1` | Interval, in seconds, between periodic summary reports. When the elapsed time since the last report exceeds this value, the strategy emits an additional interval summary log. |

Each parameter is marked as optimizable, allowing automated tuning when required.

## Runtime behaviour
1. During `OnStarted` the strategy determines the pip size from `Security.PriceStep` and configures the two moving averages.
2. Every incoming Level1 update:
   - Updates the latest bid/ask values.
   - Feeds the absolute bid movement into the pip-average indicator once a previous bid exists.
   - Feeds the instantaneous spread into the spread-average indicator when both bid and ask are known.
3. When both averages are formed, the strategy logs two types of information:
   - **Tick log** – emitted whenever a new pip-movement value arrives, reporting the current average pip movement per tick together with the average spread per tick.
   - **Interval report** – emitted once the configured number of seconds has elapsed since the previous summary log. This mirrors the periodic `Comment` block of the MQL version.

## Trading rules
The strategy never submits orders. It is intended for diagnostic purposes only and can safely run alongside other strategies to provide additional telemetry.

## Conversion notes
- The circular buffers from MQL were replaced with StockSharp moving-average indicators to avoid manual collections and to keep the logic compatible with the framework.
- Spread values are computed from the best ask minus the best bid and automatically converted into pips using the instrument's price step.
- All comments in the code are written in English, and high-level subscription helpers (`SubscribeLevel1().Bind(...)`) are employed as required by the conversion guidelines.
