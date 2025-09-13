# EMA SAR Power Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This intraday strategy combines fast and slow exponential moving averages with Parabolic SAR and Bulls/Bears Power indicators. It trades only during active market hours and requires sufficient free margin before entering any position.

The system goes short when the fast EMA is below the slow EMA, Parabolic SAR sits above the candle high, and Bears Power is rising while remaining negative. It goes long when the fast EMA is above the slow EMA, Parabolic SAR is below the candle low, and Bulls Power is falling but still positive. Each trade places a wide stop-loss and a closer take-profit.

**Dynamic Margin Filter**

Before trading, the strategy checks the portfolio's free margin. Depending on its value, the required minimum margin increases stepwise: 600 → 1000 → 1300 → 1500 → 1800 → 2000 → 2500. Trading is skipped whenever the free margin falls below the current threshold.

## Details

- **Entry Criteria**:
  - **Short**: `EMA3 < EMA34` && `SAR > High` && `BearsPower < 0` && `BearsPower > BearsPower[1]`.
  - **Long**: `EMA3 > EMA34` && `SAR < Low` && `BullsPower > 0` && `BullsPower < BullsPower[1]`.
- **Long/Short**: Both sides.
- **Stop/Target**: Stop-loss at 2000 points, take-profit at 400 points.
- **Time Filter**: Trades only between 09:00 and 16:59 broker time.
- **Indicators**:
  - Exponential Moving Averages (3, 34) on median price.
  - Parabolic SAR (0.02 step, 0.2 maximum).
  - Bulls Power (13) and Bears Power (13).
- **Default Volume**: 30 contracts.
- **Timeframe**: 15-minute candles.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Moderate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High
