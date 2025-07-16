# Volatility Breakout Strategy
[English](README.md) | [Русский](README_ru.md)

该策略在价格脱离平均区间时捕捉强势走势。通过平均真实波幅(ATR)衡量价格与简单移动平均线的距离，并据此设定随波动变化的突破阈值。

当收盘价高于SMA超过`Multiplier*ATR`时买入；当收盘价低于SMA相同距离时卖出。持仓直到出现反向突破或触发止损。

此方法适合在日内动量爆发中获利的交易者。ATR阈值有助于过滤噪声，仅在显著波动时进场。

## 细节
- **入场条件**:
  - 多头: `Close > SMA + Multiplier*ATR`
  - 空头: `Close < SMA - Multiplier*ATR`
- **多/空**: 双向
- **离场条件**:
  - 多头: 反向突破或止损触发
  - 空头: 反向突破或止损触发
- **止损**: `Multiplier*ATR`
- **默认值**:
  - `Period` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Breakout
  - 方向: 双向
  - 指标: SMA, ATR
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
