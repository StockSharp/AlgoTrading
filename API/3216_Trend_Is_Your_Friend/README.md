# Trend Is Your Friend Strategy

## Overview
The Trend Is Your Friend strategy is a multi-timeframe trend-following system inspired by the original MetaTrader expert advisor. It aligns intraday momentum with a higher timeframe MACD filter, while risk is managed via Bollinger Bands exits, classic stop-loss and take-profit targets, an optional break-even lock and trailing stop management.

The strategy works on a configurable base timeframe (default 1 hour) and analyses candle structure for a short-term momentum pattern: a bearish candle followed by a stronger bullish candle for long trades, or the inverse for short trades. These patterns must agree with a moving average trend filter and a monthly MACD signal before a position is opened.

## Entry Logic
1. Calculate a fast EMA and a slow LWMA on the entry timeframe.
2. Track the last two completed candles to form a momentum pattern:
   - **Long setup:** the candle two bars ago is bearish, the previous candle is bullish and larger in magnitude.
   - **Short setup:** the candle two bars ago is bullish, the previous candle is bearish and smaller in magnitude.
3. Confirm the setup with the moving average trend filter (fast MA above slow MA for long trades, below for short trades).
4. Confirm the long-term trend with a MACD signal computed on the higher timeframe (default monthly). The MACD line must be above the signal line for long trades and below for short trades.
5. When all filters align, open a position at market with the configured volume.

## Exit Logic
- **Bollinger Bands exit:** long positions are closed when price closes above the upper band; short positions when price closes below the lower band.
- **Take-profit / stop-loss:** optional fixed distances measured in pips. The implementation converts pips to price distance via the security price step.
- **Break-even:** optional, moves the protective stop to (or beyond) the entry price after a configurable profit threshold has been reached.
- **Trailing stop:** optional, arms after a profit threshold and trails price by a fixed pip distance. The trailing stop shares the same storage with the break-even level.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| Entry Candle | Candle type for entry logic | 1 hour |
| MACD Candle | Higher timeframe used for MACD filter | 30 days |
| Fast MA | Fast EMA length | 8 |
| Slow MA | Slow LWMA length | 20 |
| Bollinger Length | Bollinger Bands period | 20 |
| Bollinger Width | Bollinger Bands standard deviation multiplier | 2.0 |
| Stop Loss (pips) | Protective stop distance | 20 |
| Take Profit (pips) | Profit target distance | 50 |
| Use Break-Even | Enable break-even adjustment | true |
| Break-Even Trigger | Profit (pips) required to move stop | 10 |
| Break-Even Offset | Offset applied to break-even stop | 5 |
| Use Trailing | Enable trailing stop | true |
| Trailing Activation | Profit (pips) required to arm trailing | 40 |
| Trailing Distance | Distance (pips) maintained by trailing stop | 40 |

## Notes
- The strategy stores only the last two completed candles to avoid heavy historical buffers.
- MACD data is subscribed from the configured higher timeframe with aggregation enabled, allowing monthly signals to be built from daily data when necessary.
- Pip-to-price conversion uses the security price step. Instruments with non-standard pip definitions may require parameter tuning.
