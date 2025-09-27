# MACD RSI EMA BB ATR Day Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Intraday strategy combining MACD signal cross, RSI bounds and EMA trend direction with a Bollinger Bands squeeze filter. Risk management uses ATR-based stop-loss, trailing stop and risk-reward take profit.

## Details

- **Entry Criteria**: MACD crossing signal in trend direction, RSI within thresholds and no BB squeeze.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite stop or target.
- **Stops**: ATR-based stop-loss, trailing stop and risk-reward take profit.
- **Default Values**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `EmaFast` = 9
  - `EmaSlow` = 21
  - `AtrLength` = 14
  - `AtrMultiplier` = 2.0
  - `TrailAtrMultiplier` = 1.5
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `RiskReward` = 2.0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: MACD, RSI, EMA, Bollinger Bands, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
