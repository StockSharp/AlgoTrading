# 连续走势交易策略
[English](README.md) | [Русский](README_ru.md)

该策略跟踪连续上涨和下跌的 K 线。当达到设定的连胜或连败数量时，在相反方向开仓，并持有固定数量的 K 线。根据阈值忽略十字星。

## 详情
- **入场条件**: 连胜或连败达到阈值后反向开仓。
- **多空方向**: 可配置 (`TradeDirection`)。
- **退出条件**: 持有 `HoldDuration` 根 K 线后退出。
- **止损**: 无。
- **默认值**:
  - `TradeDirection` = Long
  - `StreakThreshold` = 8
  - `HoldDuration` = 7
  - `DojiThreshold` = 0.01
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 反转
  - 方向: 可配置
  - 指标: Price Action
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
