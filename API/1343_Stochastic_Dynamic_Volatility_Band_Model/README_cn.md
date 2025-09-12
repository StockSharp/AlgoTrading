# Stochastic-Dynamic Volatility Band Model 策略
[English](README.md) | [Русский](README_ru.md)

使用类似布林带的波动率带在价格穿越时交易，并在固定根数的K线后退出。

## 细节

- **入场条件**: 价格上穿下轨做多；下穿上轨做空
- **多空方向**: 双向
- **出场条件**: 持仓达到 `ExitBars` 根K线后平仓
- **止损**: 无
- **默认值**:
  - `Length` = 5
  - `Multiplier` = 1.67
  - `ExitBars` = 7
- **过滤器**:
  - 分类: 波动率
  - 方向: 双向
  - 指标: BollingerBands
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
