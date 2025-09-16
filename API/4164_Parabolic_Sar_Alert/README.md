# Parabolic SAR Alert Strategy (4164)

## Overview

This strategy reproduces the MetaTrader expert advisor **pSAR_alert2** inside the StockSharp framework. It monitors the Parabolic SAR indicator on the selected instrument and timeframe. Whenever the SAR value flips from above the closing price to below it (or vice versa), the strategy generates an informational alert. Optionally, it can submit market orders in the direction of the flip to transform the alert into an automated entry.

## Trading Logic

1. Subscribe to the configured candle series and calculate the Parabolic SAR indicator with the supplied acceleration settings.
2. Wait for each candle to finish to emulate the original EA timing.
3. Compare the indicator value with the candle close:
   - Previous SAR above the close and current SAR below the close → **bullish flip**.
   - Previous SAR below the close and current SAR above the close → **bearish flip**.
4. Log a detailed alert for every flip. When auto trading is enabled, flatten any opposite exposure and open a new position in the direction of the signal using market orders.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Candle Type` | Timeframe used to build candles and evaluate the Parabolic SAR indicator. |
| `SAR Step` | Initial acceleration factor passed to the Parabolic SAR. |
| `SAR Max` | Maximum acceleration factor of the Parabolic SAR. |
| `Enable Auto Trading` | When `true`, market orders are sent on each alert; when `false`, only logs are generated. |
| `Trade Volume` | Order size applied when auto trading is enabled. |

## Conversion Notes

- The original MetaTrader script relied on `Sleep` to throttle execution. StockSharp is event-driven, so the strategy reacts to new candles immediately without manual delays.
- Alerts are produced through `AddInfoLog`, keeping the original behavior of pop-up notifications without requiring additional UI components.
- Optional auto trading is provided to integrate the alert logic into automated workflows. Disable the `Enable Auto Trading` parameter to match the exact MetaTrader behavior.
- Python implementation is intentionally omitted as requested.
