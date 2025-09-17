# Simple 2 MA I Strategy

## Overview

Simple 2 MA I is a trend-following strategy that replicates the core logic of the original MetaTrader expert advisor. It uses a pair of linear weighted moving averages (LWMAs) calculated on typical prices to identify the dominant trend. Momentum confirmation and MACD direction filters remove weak signals. The strategy optionally manages risk through automatic stop-loss, take-profit, break-even moves, and candle-based trailing stops.

## Trading Logic

### Long Setup

1. Fast LWMA is above the slow LWMA, confirming an uptrend.
2. The low of the candle two bars ago is below the high of the previous bar, signalling fresh bullish structure.
3. At least one of the last three rate-of-change readings is above the configured momentum threshold.
4. MACD line is above the signal line.
5. Net position volume is less than the `Max Net Volume` limit.

When all conditions are met, the strategy closes short exposure (if any) and buys at market.

### Short Setup

1. Fast LWMA is below the slow LWMA, confirming a downtrend.
2. The low of the previous bar is below the high of the bar two periods ago, indicating bearish structure.
3. At least one of the last three rate-of-change readings is above the momentum threshold (absolute value).
4. MACD line is below the signal line.
5. Net position volume is less than `Max Net Volume`.

When conditions hold, the strategy covers longs (if any) and sells at market.

### Risk Management

* **Stop-loss / take-profit:** Optional fixed distances defined in points relative to entry price.
* **Break-even:** Once price reaches the trigger distance in profit, the stop is moved to entry ± offset.
* **Candle trailing:** After the activation distance is achieved, the stop follows candle extremes padded by a configurable buffer.
* Protective orders are cancelled automatically once the position is closed.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| Candle Type | Timeframe used for indicator calculations. | 15-minute candles |
| Fast LWMA | Period of the fast LWMA. | 6 |
| Slow LWMA | Period of the slow LWMA. | 85 |
| Momentum Length | Lookback period for the rate-of-change indicator. | 14 |
| Momentum Threshold | Minimum absolute rate-of-change value required. | 0.3 |
| MACD Fast | Fast EMA length used in MACD. | 12 |
| MACD Slow | Slow EMA length used in MACD. | 26 |
| MACD Signal | Signal EMA length used in MACD. | 9 |
| Use Stop-Loss | Enable placement of stop-loss orders. | true |
| Stop-Loss (points) | Distance to the stop-loss from entry price. | 20 |
| Use Take-Profit | Enable placement of take-profit orders. | true |
| Take-Profit (points) | Distance to the take-profit from entry price. | 50 |
| Use Break-Even | Enable automatic break-even move. | true |
| Break-Even Trigger | Profit (points) needed before break-even. | 30 |
| Break-Even Offset | Offset (points) added when moving to break-even. | 30 |
| Use Candle Trailing | Enable trailing stops based on candle extremes. | true |
| Trailing Activation | Profit (points) required before trailing activates. | 40 |
| Trailing Padding | Extra distance (points) added to candle extreme. | 10 |
| Max Net Volume | Maximum absolute net volume allowed. | 1 |

## Notes

* All price distances are expressed in security price steps (points). The strategy automatically multiplies parameter values by the security tick size.
* The default timeframe mapping follows the original expert defaults but can be adjusted freely.
* The strategy expects finished candles. Unfinished bars are ignored to stay consistent with the source EA.
