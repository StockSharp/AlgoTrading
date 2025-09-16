# EM VOL Strategy

This strategy trades breakouts around pivot-based support and resistance levels.
It calculates yesterday's high and low plus an ATR buffer to form entry triggers.
Trades are only placed when the ADX indicator signals a low volatility environment.

## Logic

1. Compute the previous bar's pivot and add/subtract ATR to obtain resistance and support.
2. If ADX is below the threshold and price closes above resistance, enter a long position.
3. If price closes below support, enter a short position.
4. After entry, protective stop and take-profit orders are placed.
5. A trailing stop can tighten the protective stop once profit reaches the specified level.

## Parameters

- `TakeProfit` — take-profit distance in price steps.
- `StopLoss` — stop-loss distance in price steps.
- `AtrPeriod` — ATR indicator period.
- `AdxPeriod` — ADX indicator period.
- `AdxThreshold` — maximum ADX value to allow trading.
- `TrailStart` — profit required before trailing stop begins.
- `TrailStep` — distance of the trailing stop.
- `CandleType` — timeframe used for calculations.

## Indicators Used

- Average True Range
- Average Directional Index
