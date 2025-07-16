# Super Trend 超级趋势
[English](README.md) | [Русский](README_ru.md)

该策略使用基于ATR的Supertrend指标。价格上破该线时看多，下破则看空，当线方向反转时平仓。由于止损随价格移动，能够在动量减弱后锁定利润，减少震荡。

## 详情
- **入场条件**: 基于 ATR、Supertrend 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号
- **止损**: 无
- **默认值**:
  - `Period` = 10
  - `Multiplier` = 3.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: ATR, Supertrend
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
