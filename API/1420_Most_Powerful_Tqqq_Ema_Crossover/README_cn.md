# Most Powerful TQQQ EMA Crossover 策略
[English](README.md) | [Русский](README_ru.md)

当快速 EMA 上穿慢速 EMA 时做多。止盈和止损按入场价的倍数设置。

## 细节

- **入场条件**: 快速 EMA 上穿慢速 EMA
- **多空方向**: 仅做多
- **出场条件**: 价格触及止盈或止损
- **止损**: 有（固定倍数）
- **默认值**:
  - `FastLength` = 20
  - `SlowLength` = 50
  - `TakeProfitMultiplier` = 1.3
  - `StopLossMultiplier` = 0.95
- **过滤器**:
  - 分类: 趋势
  - 方向: 多头
  - 指标: EMA
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
