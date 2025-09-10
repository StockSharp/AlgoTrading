# Scalping EMA RSI MACD Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

30-minute scalping strategy combining fast/slow EMA crossover, trend EMA, RSI and MACD filters with a volume condition. Stop-loss is based on ATR, and take profit uses a fixed risk-to-reward ratio.

## Details

- **Entry Criteria**: Fast EMA crossing slow EMA in trend direction, RSI within bounds, MACD confirmation and high volume.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite stop or target.
- **Stops**: ATR-based stop-loss and risk-reward take profit.
- **Default Values**:
  - `FastEmaLength` = 12
  - `SlowEmaLength` = 26
  - `TrendEmaLength` = 55
  - `RsiLength` = 14
  - `RsiOverbought` = 65
  - `RsiOversold` = 35
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `AtrLength` = 14
  - `AtrMultiplier` = 2.0
  - `RiskReward` = 2.0
  - `VolumeMaLength` = 20
  - `VolumeThreshold` = 1.3
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filters**:
  - Category: Scalping
  - Direction: Both
  - Indicators: EMA, RSI, MACD, ATR, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (30m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
