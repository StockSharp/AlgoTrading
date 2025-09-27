# Multi-Factor Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Multi-Factor Strategy combines MACD, RSI, and two moving averages to trade with trend confirmation. Long trades occur when MACD line is above its signal, RSI is below 70, price above the 50-period SMA, and the 50 SMA is above the 200 SMA. Short trades use opposite conditions.

Stops and targets are based on ATR multiples.

## Details

- **Entry Criteria**:
  - **Long**: `MACD > Signal` && `RSI < 70` && `Close > SMA50` && `SMA50 > SMA200`.
  - **Short**: `MACD < Signal` && `RSI > 30` && `Close < SMA50` && `SMA50 < SMA200`.
- **Long/Short**: Both directions.
- **Exit Criteria**: ATR-based stop loss and take profit.
- **Stops**: Yes.
- **Default Values**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `RsiLength` = 14
  - `AtrLength` = 14
  - `StopAtrMultiplier` = 2
  - `ProfitAtrMultiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: MACD, RSI, SMA, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
