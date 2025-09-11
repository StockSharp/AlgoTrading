# Pure Price Action Breakout with 1:5 RR 策略
[English](README.md) | [Русский](README_ru.md)

Pure Price Action Breakout with 1:5 RR 策略利用两条 EMA 的交叉，并通过 RSI 和成交量确认。止损基于 ATR，止盈为风险的五倍。

## 详情

- **入场条件**:
  - **多头**: 快速 EMA 上穿慢速 EMA，RSI > 50，成交量高于 20 期 SMA。
  - **空头**: 快速 EMA 下穿慢速 EMA，RSI < 50，成交量高于 20 期 SMA。
- **方向**: 双向。
- **出场条件**:
  - 基于 ATR 的止损以及 1:5 风险回报止盈。
- **止损**: 止损 = 1.5 × ATR，止盈 = 5 × 风险。
- **默认参数**:
  - `FastPeriod` = 9
  - `SlowPeriod` = 21
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `VolumePeriod` = 20
  - `StopLossFactor` = 1.5
  - `RiskRewardRatio` = 5
  - `MaxTradesPerDay` = 5
- **过滤器**:
  - 分类: 突破
  - 方向: 双向
  - 指标: EMA, RSI, ATR, 成交量 SMA
  - 止损: ATR 止损, 1:5 止盈
  - 复杂度: 低
  - 时间框架: 5m 或 15m
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
