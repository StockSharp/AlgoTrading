# Macd Momentum Reversal
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy uses MACD histogram to detect momentum reversals.
Shorts when bullish candle grows but MACD histogram declines.
Buys when bearish candle grows but MACD histogram rises.

## Details

- **Entry Criteria**: Larger candle body with fading MACD momentum.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: MACD
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
