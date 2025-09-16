# Multi Time Frame Trader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy recreates the original MQL "Multi Time Frame Trader" logic with StockSharp high-level APIs. It combines three
polynomial regression channels (M1, M5 and H1) and only trades when the lower time frames test their channel extremes in the
direction suggested by the hourly slope.

The system continuously recomputes the regression channel upper, middle and lower bands on every finished candle. When the hourly
upper band decreases, the bias is bearish; when it rises, the bias is bullish. Entries are triggered once the M5 and M1 candles
reach the corresponding band and the directional filter agrees.

## Core workflow

- **Subscriptions**: the strategy listens to 1-minute, 5-minute and 1-hour candles simultaneously.
- **Regression channel**: each subscription builds a polynomial regression line (degree 1-3) over `Bars` points and offsets it by
  `StdMultiplier` standard deviations to obtain resistance and support bands.
- **Slope estimation**: the channel slope is derived from the difference between the current upper band and the upper band `Bars`
  candles ago, mirroring the `i-Regr` indicator behaviour.
- **Directional filter**: the H1 slope defines whether only shorts (negative slope) or longs (positive slope) are allowed.

## Entry logic

### Short trades

1. Hourly slope is negative.
2. Latest 5-minute candle high touches or breaks the 5-minute regression resistance.
3. Latest 1-minute candle high touches or breaks the 1-minute resistance.
4. No existing short position is open (`Position >= 0`).
5. A market sell order is sent, the stop loss is set half a channel width above the entry and the target equals the M5 midline.

### Long trades

1. Hourly slope is positive.
2. Latest 5-minute candle low touches or breaks the 5-minute regression support.
3. Latest 1-minute candle low touches or breaks the 1-minute support.
4. No existing long position is open (`Position <= 0`).
5. A market buy order is sent, the stop loss is placed half a channel width below the entry and the target equals the M5 midline.

## Exit rules

- Stops and targets are stored internally and evaluated on every finished M1 candle. If the candle range crosses the stored
  stop level, the position is closed immediately.
- If the profit target is reached before the stop, the position is also closed.
- Closing resets the tracked levels so a fresh signal can be evaluated without delay.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Degree` | 1 | Polynomial order of the regression channel (1=linear, 2=parabolic, 3=cubic). |
| `StdMultiplier` | 2.0 | Multiplier for the standard deviation that defines band width. |
| `Bars` | 250 | Number of candles used for regression fitting and slope lookback. |
| `Shift` | 0 | Horizontal shift for the regression evaluation point (clamped between 0 and `Bars - 1`). |
| `UseTrading` | true | Disables all order generation when set to `false`, while the channel continues to update. |

## Additional notes

- The strategy stores stop and target levels locally because StockSharp market orders do not automatically attach SL/TP levels.
- It works on any instrument that supports minute and hourly candles; however, the original logic was designed for forex pairs.
- Adjust `Bars` to match the volatility of the traded instrument. A smaller value reacts faster, a larger value produces smoother
  channels.
- Set `Degree` to 1 for a straight regression channel (closest to the classic linear version), or use higher degrees to emulate
the polynomial modes from the MQL indicator.
