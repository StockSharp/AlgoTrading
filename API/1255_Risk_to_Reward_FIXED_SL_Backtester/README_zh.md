# Risk to Reward Fixed SL Backtester 策略
[Русский](README_ru.md) | [English](README.md)

当收盘价等于设定值时进入多头。止损基于ATR或一定周期内的最低价，止盈由风险回报比或固定百分比计算。达到目标后可将止损移到保本位。

## 细节

- **入场条件**：收盘价等于 `DealStartValue`
- **多空方向**：多头
- **出场条件**：止盈或止损（可选保本）
- **止损**：ATR 或 最低价，带保本
- **默认值**：
  - `DealStartValue` = 100
  - `UseRiskToReward` = true
  - `RiskToRewardRatio` = 1.5
  - `StopLossType` = Atr
  - `AtrFactor` = 1.4
  - `PivotLookback` = 8
  - `FixedTp` = 0.015
  - `FixedSl` = 0.015
  - `UseBreakEven` = true
  - `BreakEvenRr` = 1.0
  - `BreakEvenPercent` = 0.001
- **过滤器**：
  - 类别: 趋势
  - 方向: 多头
  - 指标: ATR, Lowest
  - 止损: 有
  - 复杂度: 基础
  - 周期: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
