# Ichimoku Retracement Strategy

This strategy is a StockSharp conversion of the MetaTrader expert advisor **"ICHMOKU RETRACEMENT"**. It keeps the original idea o
f trading Ichimoku pullbacks that happen inside a higher timeframe trend while being filtered by long-term momentum and MACD read
ings. The StockSharp implementation focuses on clarity, indicator reuse and risk control through the high-level API.

## Trading Idea

1. **Trend filter** – the strategy looks for a bullish or bearish bias using a pair of Linear Weighted Moving Averages (LWMA). A
 bullish context requires the fast LWMA to be above the slow LWMA, while a bearish context requires the opposite relation.
2. **Ichimoku retracement** – after a trend is detected, the previous candle must touch any of the Ichimoku lines (Tenkan-sen, Ki
jun-sen or the two leading spans). The current candle must open back on the trend side of the touched line, signaling a momentum p
ullback.
3. **Momentum confirmation** – the close-to-close momentum ratio must deviate from its neutral value (100) by at least a configurable
 threshold. The ratio is computed on the same timeframe used for the Ichimoku indicator.
4. **Macro filter** – a monthly MACD (12/26/9) confirms the dominant long-term direction. Long trades require MACD main line above
 the signal line, short trades require the opposite.
5. **Order management** – the strategy keeps at most one net position. Protective stop-loss and take-profit levels are placed in pi
ps and evaluated on every finished candle.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `Signal Candle Type` | Timeframe used for the LWMA, Ichimoku and momentum calculations. | 1 hour candles |
| `Macro Candle Type` | Higher timeframe used for the MACD trend filter. | 30-day candles |
| `Fast LWMA` | Period for the fast linear weighted moving average. | 6 |
| `Slow LWMA` | Period for the slow linear weighted moving average. | 85 |
| `Tenkan Period` | Ichimoku Tenkan-sen period. | 9 |
| `Kijun Period` | Ichimoku Kijun-sen period. | 26 |
| `Span B Period` | Ichimoku Senkou Span B period. | 52 |
| `Momentum Period` | Lookback for the close-to-close momentum ratio. | 14 |
| `Momentum Threshold` | Minimum absolute deviation from 100 required by the momentum ratio. | 0.3 |
| `Take Profit (pips)` | Take-profit distance expressed in pips. | 50 |
| `Stop Loss (pips)` | Stop-loss distance expressed in pips. | 20 |

The base `Volume` parameter controls the size of new orders. When a reversal signal appears, the strategy closes the current posit
ion (if any) and opens a new position in the opposite direction using `Volume + |Position|` contracts.

## Trade Rules

### Long Entries
- Fast LWMA > slow LWMA.
- MACD main line > MACD signal line on the macro timeframe.
- Momentum ratio deviation ≥ threshold.
- Previous candle low touched at least one Ichimoku level and the current candle opened back above that level.
- Net position must be flat or short.

### Short Entries
- Fast LWMA < slow LWMA.
- MACD main line < MACD signal line on the macro timeframe.
- Momentum ratio deviation ≥ threshold.
- Previous candle high touched at least one Ichimoku level and the current candle opened back below that level.
- Net position must be flat or long.

### Exits
- A long position closes when the candle’s low hits the stop-loss or the high reaches the take-profit level.
- A short position closes when the candle’s high hits the stop-loss or the low reaches the take-profit level.

## Differences vs. Original EA

- Money-management ladders, break-even moves and trailing features from the MQL version are not replicated; risk control is limite
d to fixed stop-loss and take-profit levels.
- StockSharp works with a single net position, so the martingale order stack is replaced by one entry per direction.
- Alerts, e-mail and push notifications from the MetaTrader environment are omitted.

## Usage Notes

1. Add the strategy to a StockSharp Designer or Shell project.
2. Select the desired instrument and adjust the `Signal Candle Type` to match your target timeframe.
3. Ensure the `Macro Candle Type` can be synthesized from the available data (the subscription uses `allowBuildFromSmallerTimeFram
e`).
4. Tune stop-loss, take-profit and the momentum threshold according to the instrument’s volatility.

The included comments describe each decision step so the logic can be adapted or extended easily.
