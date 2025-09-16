# Asimmetric Stoch NR 策略
[Русский](README_ru.md) | [English](README.md)

基于非对称随机指标的策略。当 %K 与 %D 交叉时开仓或平仓，并可使用止盈和止损保护。

该方法在计算 %K 时切换周期以适应市场噪音。止损与止盈以绝对价格单位表示。

## 细节

- **入场条件**:
  - 多头：`%K` 自下向上穿越 `%D`
  - 空头：`%K` 自上向下穿越 `%D`
- **多/空**：双向
- **出场条件**:
  - 多头：`%K` 自上向下穿越 `%D`
  - 空头：`%K` 自下向上穿越 `%D`
- **止损**：绝对值 `StopLoss` 和 `TakeProfit`
- **默认值**:
  - `KPeriodShort` = 5
  - `KPeriodLong` = 12
  - `DPeriod` = 7
  - `Slowing` = 3
  - `Overbought` = 80m
  - `Oversold` = 20m
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **筛选**:
  - 类型：振荡器
  - 方向：双向
  - 指标：随机指标
  - 止损：是
  - 复杂度：中等
  - 时间框架：长周期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

