# Scalping 15m EMA MACD RSI ATR
[Русский](README_ru.md) | [中文](README_cn.md)

Scalping strategy combining a 50-period EMA trend filter, MACD histogram momentum and RSI levels. Risk management uses ATR-based stop loss and take profit.

The strategy buys when price is above the EMA, the MACD histogram is positive and RSI sits between 50 and the overbought level. Shorts occur when price is below the EMA, the histogram is negative and RSI is between the oversold level and 50. Stops and targets trail by ATR multiples from the close.

## Details

- **Entry Criteria**: Price relative to EMA, MACD histogram sign, RSI level.
- **Long/Short**: Both directions.
- **Exit Criteria**: ATR-based stop loss or take profit.
- **Stops**: Yes.
- **Default Values**:
  - `EmaPeriod` = 50
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `AtrPeriod` = 14
  - `SlAtrMultiplier` = 1m
  - `TpAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Scalping
  - Direction: Both
  - Indicators: EMA, MACD, RSI, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
