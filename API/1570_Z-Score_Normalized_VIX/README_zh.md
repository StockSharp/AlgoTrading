# Z-Score Normalized VIX Strategy
[English](README.md) | [Русский](README_ru.md)

该策略计算多个VIX指数的z分数并求平均，当组合值低于负阈值时做多。

算法为 VIX、VIX3M、VIX9D 和 VVIX 计算 z-score，并对选中的值求平均，以反映整体波动性情绪。

## 细节

- **入场条件**: 组合 z-score 低于 `-Threshold`。
- **多/空**: 仅做多。
- **离场条件**: 组合 z-score 高于 `-Threshold`。
- **止损**: 无。
- **默认值**:
  - `ZScoreLength` = 6
  - `Threshold` = 1
  - `UseVix` = true
  - `UseVix3m` = true
  - `UseVix9d` = true
  - `UseVvix` = true
  - `CandleType` = TimeSpan.FromDays(1)
- **过滤器**:
  - 类别: 波动率
  - 方向: 多头
  - 指标: Z-Score
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日线
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险级别: 中等
