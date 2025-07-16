# ADX DI
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on ADX and Directional Movement indicators

ADX DI focuses on the crossing of +DI and -DI with rising ADX. A bullish cross of +DI over -DI coupled with strong ADX opens longs, while the opposite opens shorts. Positions close on a weakening ADX or opposite cross.

This combination helps avoid trading every DI cross by demanding confirmation from the ADX. The system aims to capture sustainable trends rather than short-term swings.


## Details

- **Entry Criteria**: Signals based on ADX, ATR.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ADX, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
