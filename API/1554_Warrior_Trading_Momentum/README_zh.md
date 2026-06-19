# Warrior Trading Momentum Strategy
[English](README.md) | [Русский](README_ru.md)

受Warrior Trading启发的动量策略，结合跳空、VWAP与"红转绿"形态。

## 细节

- **入场条件**：Gap-and-go、红转绿或带成交量的VWAP反弹。
- **多空方向**：仅做多。
- **出场条件**：基于ATR的止损、止盈和追踪。
- **止损**：是。
- **默认值**：
  - `GapThreshold` = 2m
  - `GapVolumeMultiplier` = 2m
  - `VwapDistance` = 0.5m
  - `MinRedCandles` = 3
  - `RiskRewardRatio` = 2m
  - `TrailingStopTrigger` = 1m
  - `MaxDailyTrades` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选**：
  - 分类：动量
  - 方向：多头
  - 指标：VWAP, RSI, EMA, ATR, Volume
  - 止损：是
  - 复杂度：高级
  - 时间框架：日内(1m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：高
