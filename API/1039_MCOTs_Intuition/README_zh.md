# MCOTs Intuition 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 RSI 动量与其标准差的关系。当上升动量强但开始减弱时买入，动量反向且减弱时做空。使用固定的获利和止损目标（以 ticks 表示）。

## 细节

- **入场条件**：
  - 多头：momentum > stdDev * multiplier 且 momentum < previousMomentum * exhaustionMultiplier
  - 空头：momentum < -stdDev * multiplier 且 momentum > previousMomentum * exhaustionMultiplier
- **多空方向**：皆可
- **出场条件**：
  - 固定的获利与止损 ticks
- **止损**：是
- **默认值**：
  - `RsiPeriod` = 14
  - `StdDevMultiplier` = 1m
  - `ExhaustionMultiplier` = 1m
  - `ProfitTargetTicks` = 40
  - `StopLossTicks` = 160
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **筛选**：
  - 分类：反转
  - 方向：双向
  - 指标：RSI、StandardDeviation
  - 止损：是
  - 复杂度：基础
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
