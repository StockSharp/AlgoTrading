# Turtle Trader 策略
[English](README.md) | [Русский](README_ru.md)

Turtle Trader 采用经典的 Turtle 突破系统，使用唐奇安通道和 ATR 资金管理。价格突破近期高点时买入，跌破近期低点时卖出。价格朝有利方向移动时通过加仓来扩大利润。

## 细节

- **入场条件**: `S1` 或 `S2` 期间的最高/最低点突破
- **多空方向**: 双向
- **出场条件**: 反向突破或 ATR 止损
- **止损**: ATR
- **默认值**:
  - `RiskPercent` = 1
  - `AtrPeriod` = 20
  - `StopMultiplier` = 1.5
  - `PyramidProfit` = 0.5
  - `S1Long` = 20
  - `S2Long` = 55
  - `S1LongExit` = 10
  - `S2LongExit` = 20
  - `S1Short` = 15
  - `S2Short` = 55
  - `S1ShortExit` = 7
  - `S2ShortExit` = 20
- **过滤器**:
  - 分类: 趋势跟随
  - 方向: 双向
  - 指标: ATR, Highest, Lowest
  - 止损: ATR
  - 复杂度: 中等
  - 时间框架: 日线
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
