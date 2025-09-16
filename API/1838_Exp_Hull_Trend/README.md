# Exp Hull Trend Strategy

## Overview

Exp Hull Trend Strategy is based on the Hull Moving Average (HMA) indicator. The algorithm compares an intermediate Hull calculation with a smoothed Hull moving average. When the faster Hull line crosses above the slower smoothed line, the strategy opens a long position. When the fast line crosses below the smoothed line, the strategy opens a short position.

## Strategy Logic

1. Calculate a weighted moving average (WMA) of the closing price with period **Length / 2**.
2. Calculate another WMA of the closing price with period **Length**.
3. Build the intermediate Hull value: `fast = 2 * WMA(Length/2) - WMA(Length)`.
4. Smooth the intermediate value with a WMA of period `sqrt(Length)` to get the final Hull value `slow`.
5. Generate signals:
   - **Long Entry** – when `fast` crosses above `slow`.
   - **Short Entry** – when `fast` crosses below `slow`.
6. Positions are reversed on opposite signals. Protective orders are handled through `StartProtection`.

## Parameters

| Name | Description |
|------|-------------|
| `Hull Length` | Base period for Hull calculation. Determines sensitivity of both WMAs. |
| `Candle Type` | Time frame of candles used for indicator calculations. |

## Notes

- The strategy works on completed candles only.
- Indicator values are bound via the high-level API to avoid manual data collections.
- Volume is taken from the strategy settings; when the signal direction changes, the position is reversed.
