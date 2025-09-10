# MA PSAR ATR Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The MA PSAR ATR Trend strategy combines a moving-average crossover with a daily Parabolic SAR filter. Trades are taken only when price aligns above or below both averages and the PSAR agrees. An ATR-based stop controls risk.

The method suits traders seeking trend following with dynamic stops. Signals trigger on 5-minute candles by default.

## Details
- **Entry Criteria**:
  - **Long**: Fast MA > Slow MA, Close > Fast MA, Low > Daily PSAR
  - **Short**: Fast MA < Slow MA, Close < Fast MA, High < Daily PSAR
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Trend turns bearish or price falls below ATR stop
  - **Short**: Trend turns bullish or price rises above ATR stop
- **Stops**: Yes, ATR-based.
- **Default Values**:
  - `FastMaPeriod` = 40
  - `SlowMaPeriod` = 160
  - `SarStep` = 0.02m
  - `SarMaxStep` = 0.2m
  - `AtrPeriod` = 14
  - `AtrMultiplierLong` = 2m
  - `AtrMultiplierShort` = 2m
  - `UsePsarFilter` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MA, Parabolic SAR, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
