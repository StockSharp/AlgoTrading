# ATR GOD 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用 Supertrend 指标作为入场信号，并基于 ATR 设置止损和止盈。

## 详情

- **入场条件**: Supertrend 翻转。
- **多空方向**: 双向。
- **退出条件**: ATR 止损或反向信号。
- **止损**: ATR 止损。
- **默认值**:
  - `Period` = 10
  - `Multiplier` = 3m
  - `RiskMultiplier` = 4.5m
  - `RewardRiskRatio` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: ATR, Supertrend
  - 止损: ATR
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

