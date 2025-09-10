# Bitcoin 1H-15M Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy tracks the high and low of the previous 1-hour candle and enters trades when a 15-minute candle closes outside this range. Risk is managed with a fixed stop-loss buffer and a take-profit derived from a configurable risk-reward ratio.

## Details

- **Entry Criteria**:
  - 15-minute close above previous 1-hour high → long entry.
  - 15-minute close below previous 1-hour low → short entry.
- **Long/Short**: Both
- **Exit Criteria**:
  - Stop loss at fixed buffer distance.
  - Take profit at buffer × risk-reward ratio.
- **Stops**: Stop loss and take profit via protection module.
- **Default Values**:
  - Lower timeframe = 15 minutes.
  - Higher timeframe = 1 hour.
  - Stop loss buffer = 50.
  - Risk-reward ratio = 2.0.
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: SL & TP
  - Complexity: Low
  - Timeframe: Short
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
