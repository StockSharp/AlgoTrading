# Statistical Arbitrage Pairs Trading - Long-Side Only
[English](README.md) | [Русский](README_ru.md)

该策略基于两个品种的z分数差执行配对交易。当价差z分数低于设定阈值时开多仓，当价差z分数上穿零时平仓。

## 细节

- **入场条件**：价差z分数低于阈值。
- **多空方向**：仅做多。
- **出场条件**：价差z分数上穿零。
- **止损**：无。
- **默认值**：
  - `ZScoreLength` = 20
  - `ExtremeLevel` = -1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别：均值回归
  - 方向：多头
  - 指标：SMA, StandardDeviation
  - 止损：否
  - 复杂度：初级
  - 周期：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
