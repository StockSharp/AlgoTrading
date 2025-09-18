# 5Mins Envelopes
[Русский](README_ru.md) | [中文](README_cn.md)

The **5Mins Envelopes** strategy reproduces the MetaTrader expert that trades five-minute candles around a linear weighted moving average envelope.
It looks for price spikes that stretch well beyond the bands and then enters in the mean-reversion direction.
A spread filter, static stop-loss, optional take-profit, and trailing stop mirror the original money management.

## Trading Logic
- **Indicator**: Linear Weighted Moving Average (LWMA) calculated on the median price (high+low)/2 with a period of 3.
- **Envelope width**: 0.05% deviation from the LWMA value (upper and lower bands).
- **Signal detection** (evaluated on the previous completed candle and current bid):
  - **Long**: Previous candle low stays more than `DistancePoints` below the lower band **and** the current bid is also beyond that distance.
  - **Short**: Previous candle high stays more than `DistancePoints` above the upper band **and** the current bid is also beyond that distance.
- **Filters**:
  - Only one position at a time (new entries require the current position to be flat).
  - If `MaxSpreadPoints` is greater than zero, the bid/ask spread must stay below this threshold before submitting a new order.

## Risk Management
- **Order volume**: `TradeVolume` parameter controls the market order size.
- **Stop-loss**: `StopLossPoints` converts to absolute price distance using the instrument tick size.
- **Take-profit**: Optional `TakeProfitPoints`; set to zero to disable.
- **Trailing stop**: Optional `TrailingStopPoints`; set to zero to disable.
- **Protection**: The `StartProtection` helper applies all exits with market orders, matching the MetaTrader behaviour.

## Parameters
- `TradeVolume = 1m`
- `DistancePoints = 140`
- `EnvelopePeriod = 3`
- `EnvelopeDeviationPercent = 0.05m`
- `StopLossPoints = 250`
- `TakeProfitPoints = 0`
- `TrailingStopPoints = 120`
- `MaxSpreadPoints = 25`
- `CandleType = TimeFrame(5 minutes)`

## Tags
- Category: Mean Reversion
- Direction: Both
- Indicators: WeightedMovingAverage
- Stops: Yes (fixed + trailing)
- Timeframe: Intraday (M5)
- Complexity: Beginner
- Risk Level: Medium
- Seasonality: No
- Neural Networks: No
- Divergence: No
