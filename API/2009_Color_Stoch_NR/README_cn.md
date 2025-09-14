# Color Stochastic NR Strategy
[English](README.md) | [Русский](README_ru.md)

该策略使用随机指标并提供多种信号模式。每种模式以不同方式解释 %K 和 %D 线以生成买卖信号。

模式：

- **Breakdown** – 当 %K 向上突破 50 时做多，跌破 50 时做空。
- **OscTwist** – 对 %K 方向的改变做出反应。
- **SignalTwist** – 对 %D 方向的改变做出反应。
- **OscDisposition** – 当 %K 上穿 %D 时做多，下穿时做空。
- **SignalBreakdown** – 当 %D 穿越 50 水平时交易。

相反信号会平掉当前仓位并在反方向开仓。风险由固定百分比的止损和止盈控制。

## 详细信息

- **入场条件**：
  - **多头**：取决于所选模式。
  - **空头**：取决于所选模式。
- **Long/Short**：双向。
- **离场条件**：相反信号或保护性止损止盈。
- **止损**：是，`StopLossPercent` 和 `TakeProfitPercent`。
- **默认参数**：
  - `KPeriod` = 5
  - `DPeriod` = 3
  - `Mode` = `OscDisposition`
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 2
  - `CandleType` = 4 小时
- **过滤器**：
  - 分类: 振荡器
- 方向: 双向
  - 指标: Stochastic
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 4H
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
