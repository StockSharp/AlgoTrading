# Supertrade RVI Long-Only 策略
[English](README.md) | [Русский](README_ru.md)

当 RVI 指标上穿 20 时开多仓。止损和止盈由风险百分比与收益比率设定。

## 细节

- **入场条件**: RVI 上穿阈值
- **多空方向**: 多头
- **出场条件**: 止损或止盈
- **止损**: 有
- **默认值**:
  - `RviLength` = 10
  - `EmaLength` = 14
  - `RviThreshold` = 20
  - `RiskPercent` = 1
  - `RewardRatio` = 3
- **过滤器**:
  - 分类: 动量
  - 方向: 多头
  - 指标: StdDev, EMA
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

