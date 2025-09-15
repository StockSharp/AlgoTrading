# Bezier ReOpen Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Bezier ReOpen Strategy** applies a custom Bezier curve indicator to follow trend direction.
When the indicator turns upward and the latest value is above the previous one, the strategy can open a long position.
When the indicator turns downward, it can open a short position. Existing positions are closed when the indicator changes direction.
After entering, additional positions are re-opened each time price advances by a user-defined step, allowing scaling into the trend.

This implementation is based on the original MetaTrader Expert Advisor `Exp_Bezier_ReOpen.mq5` (ID 16883).

## Details

- **Indicator**: Bezier curve constructed from the last `BPeriod` prices and parameter `T` defining curve tension.
- **Entry**:
  - **Long**: indicator slope turns up and current value is above previous value.
  - **Short**: indicator slope turns down and current value is below previous value.
- **Exit**:
  - **Long**: indicator slope turns down.
  - **Short**: indicator slope turns up.
- **Re-entry**: after initial entry, an extra order is sent each time price moves `PriceStep` away from the last entry price, up to `PosTotal` orders.
- **Stops**: optional stop-loss and take-profit defined in absolute price units.

## Parameters

- `CandleType` – candle timeframe used for calculations. Default: 4-hour.
- `BPeriod` – number of bars for Bezier calculation. Default: 8.
- `T` – Bezier curve tension (0..1). Default: 0.5.
- `PriceType` – price source for the indicator (close, open, high, low, median, typical, weighted). Default: weighted.
- `PriceStep` – price distance to send additional orders. Default: 300.
- `PosTotal` – maximum number of positions in scaling sequence. Default: 10.
- `BuyPosOpen` – allow opening long positions. Default: true.
- `SellPosOpen` – allow opening short positions. Default: true.
- `BuyPosClose` – allow closing longs on opposite signal. Default: true.
- `SellPosClose` – allow closing shorts on opposite signal. Default: true.
- `StopLoss` – stop-loss in price units. Default: 1000.
- `TakeProfit` – take-profit in price units. Default: 2000.

## Filter Tags
- Category: Trend following
- Direction: Both
- Indicators: Custom
- Stops: Optional
- Complexity: Medium
- Timeframe: Medium-term
- Risk level: Moderate
