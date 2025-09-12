# Post Open Long ATR Stop Loss Take Profit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy enters a long position during market open after a breakout from resistance while price stays near the Bollinger basis. It uses EMA, RSI, ADX and ATR filters and exits via ATR-based stop loss and take profit.

## Details

- **Entry Criteria**:
  - **Long**: Breakout above recent resistance within market open, price near Bollinger middle band, RSI above threshold, ADX above threshold, short-term trend up and no pullback.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - ATR stop loss or take profit reached.
- **Stops**:
  - ATR-based stop loss and take profit.
- **Default Values**:
  - `BB Length` = 14
  - `BB Mult` = 1.5
  - `EMA Length` = 10
  - `EMA Long Length` = 200
  - `RSI Length` = 7
  - `RSI Threshold` = 30
  - `ADX Length` = 7
  - `ADX Threshold` = 10
  - `ATR Length` = 14
  - `ATR SL Mult` = 2.0
  - `ATR TP Mult` = 4.0
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: Bollinger Bands, EMA, RSI, ADX, ATR
  - Stops: ATR
  - Complexity: Intermediate
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
