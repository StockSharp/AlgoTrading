# ZScore Reversal Strategy
[English](README.md) | [Русский](README_ru.md)

该策略通过计算价格相对于移动平均线的标准差距离(Z分数)来衡量偏离程度，捕捉可能向均值回归的极端状态。

当Z分数低于负阈值时做多，表明市场超卖；当Z分数高于正阈值时做空。Z分数回到0附近时平仓。

适合喜欢客观入场标准的均值回归交易者。百分比止损在等待回归期间限制损失。

## 细节
- **入场条件**:
  - 多头: `ZScore < -Threshold`
  - 空头: `ZScore > Threshold`
- **多/空**: 双向
- **离场条件**:
  - 多头: 当ZScore上穿0时平仓
  - 空头: 当ZScore下穿0时平仓
- **止损**: 百分比止损
- **默认值**:
  - `LookbackPeriod` = 20
  - `ZScoreThreshold` = 2.0m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(10)
- **过滤器**:
  - 类别: Mean Reversion
  - 方向: 双向
  - 指标: Z-Score
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
