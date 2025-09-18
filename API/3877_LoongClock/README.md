# Loong Clock Strategy

## Overview
Loong Clock Strategy is a visual-only StockSharp sample converted from the original MetaTrader expert `LoongClock.mq5`. The strategy draws a digital representation of an analog clock directly on the chart. It updates once per second and shows the current hour, minute, and second values positioned around a circular layout. The clock can display local machine time, trading server time, or UTC.

Unlike classic trading strategies, this sample does not send orders or evaluate market conditions. Its purpose is to demonstrate how to use StockSharp chart drawing helpers and the strategy timer to create continuously updated on-chart widgets.

## How it works
1. Subscribes to the configured candle type to ensure the chart area has a valid time/price axis.
2. Stores the most recent candle close price as the vertical anchor point for the clock.
3. Starts a one-second strategy timer. Each tick of the timer:
   - Selects the time source (local/server/UTC) configured by the user.
   - Calculates the position of each clock label using simple trigonometry.
   - Draws the center marker (`@`) plus hour, minute, and second labels at their respective coordinates.
4. All drawings reuse the same anchor time, which keeps the clock stationary on the chart while the labels update in place.

The label positions are computed by converting the clock angle to offsets along the chart's time (horizontal) and price (vertical) axes. Different radii are used for hour, minute, and second labels to reproduce the MQL version's layered look.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Candle series used to anchor the chart area. Default: 1 minute time-frame candles. |
| `TimeSource` | Time base displayed by the clock. Options: `Local`, `Server`, `Utc`. Default: `Local`. |
| `HourTimeRadius` | Horizontal radius for the hour label measured in time. Default: 8 minutes. |
| `MinuteTimeRadius` | Horizontal radius for the minute label measured in time. Default: 10 minutes. |
| `SecondTimeRadius` | Horizontal radius for the second label measured in time. Default: 12 minutes. |
| `HourPriceRadius` | Vertical radius for the hour label measured in price units. Default: 0.20. |
| `MinutePriceRadius` | Vertical radius for the minute label measured in price units. Default: 0.32. |
| `SecondPriceRadius` | Vertical radius for the second label measured in price units. Default: 0.52. |

All radii parameters are exposed so the user can easily resize or reshape the clock for different chart scales.

## Usage
1. Attach the strategy to a security that provides candles for the configured `CandleType`.
2. Ensure the connector supplies either real-time or historical data so that at least one candle closes.
3. Observe the chart to see the clock refresh every second. Switch `TimeSource` to display local machine time, server time, or UTC.
4. Adjust the radius parameters to fine-tune the visual spacing of the hour, minute, and second labels.

## Differences from the MQL version
- Uses StockSharp's strategy timer and chart drawing helpers instead of MetaTrader's timer and chart label APIs.
- Displays formatted two-digit values for hour, minute, and second labels to improve legibility.
- Allows individual configuration of horizontal and vertical radii through strategy parameters.
- Anchors the clock to the most recent candle close price instead of chart pixel coordinates, making it responsive to different instruments and price scales.

This translation preserves the original idea of a continuously updating chart clock while embracing StockSharp's high-level API for visual rendering.
