# Doji Pattern Alert Strategy

## Overview
Doji Pattern Alert Strategy is a high level StockSharp port of the MetaTrader expert **DojiEN.mq4**. The strategy does not place
orders. Instead it continuously scans the most recent finished candle and raises an alert whenever a classic doji forms. A classic
doji is detected when the candle body is extremely small and both the open and close prices appear near the center of the total
range.

The detection logic mirrors the original expert advisor:

1. Read the open, high, low and close of the last completed bar.
2. Verify that the absolute difference between the open and close is less than a configurable number of price points.
3. Compute the candle midpoint and confirm that both the open and the close remain within a tolerance band around that midpoint.
4. Log an informational message when both conditions are satisfied.

Because the original code added chart objects, the port attaches a candle chart to the strategy window so the operator can
visually confirm the detected doji bars. No manual drawing is required.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| Candle Type | Timeframe of the candles used for the analysis. | 15 minute time frame |
| Middle Tolerance (%) | Maximum distance from the candle midpoint expressed as percentage of the full candle range. | 10 |
| Body Difference (points) | Maximum distance between open and close expressed in price steps. | 3 |

### Parameter notes
- The **Middle Tolerance (%)** value of `10` means the open and close must stay within 10% of the total range measured from the
midpoint. Setting a smaller percentage makes the filter stricter.
- **Body Difference (points)** is multiplied by the security price step. If the security does not define a tick size, the strategy
falls back to a size of one to keep the check operational.

## Alerts and logging
Whenever a doji is confirmed the strategy writes a log entry similar to:

```
Classic doji detected at 2024-05-15T08:45:00.0000000+00:00. Body size: 0.00020, range: 0.00150.
```

Attach any custom listeners (notifications, email, etc.) to the logging pipeline if you need additional delivery methods.

## Trading behavior
This strategy never opens or closes positions. It only monitors incoming candles and emits alerts, faithfully matching the
behavior of the original MetaTrader expert that was limited to chart annotations and pop-up alerts.
