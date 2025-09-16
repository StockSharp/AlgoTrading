# MACD EMA SAR Bollinger BullBear Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines MACD, EMA crossover, Parabolic SAR, Bollinger Bands, and Bulls/Bears Power indicators. Trades only during active hours.

## Details

- **Entry Criteria**:
  - **Long**: MACD < Signal, last two highs below upper Bollinger Band, EMA3 > EMA34, SAR below price, Bulls Power > 0 and decreasing.
  - **Short**: MACD > Signal, EMA3 < EMA34, SAR above price, Bears Power < 0 and increasing.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - No dedicated exit rules; position closes on opposite signal.
- **Stops**: None.
- **Default Values**:
  - `MACD Fast` = 12
  - `MACD Slow` = 26
  - `MACD Signal` = 9
  - `Fast EMA Period` = 3
  - `Slow EMA Period` = 34
  - `Power Period` = 13
  - `SAR Step` = 0.02
  - `SAR Max` = 0.2
  - `Bollinger Period` = 20
  - `Bollinger Deviation` = 2.0
  - `Candle Type` = 15-minute
  - `Session Start` = 09:00
  - `Session End` = 17:00
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: Multiple
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
