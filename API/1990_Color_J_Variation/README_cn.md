# Color J Variation 策略
[Русский](README_ru.md) | [English](README.md)

该策略基于 JMA 曲线方向的变化，与 MQL 中的 ColorJVariation EA 相对应。策略跟踪 Jurik 移动平均线的斜率，当趋势由下转上或由上转下时入场，并支持绝对止损和止盈。

## 细节

- **入场条件**:
  - 多头：`PrevSlopeDown && JMA turns up`
  - 空头：`PrevSlopeUp && JMA turns down`
- **多/空**：双向
- **离场条件**:
  - 相反的反转信号
- **止损**：通过 `StopLoss` 和 `TakeProfit` 设置绝对价格
- **默认值**:
  - `JmaPeriod` = 12
  - `JmaPhase` = 100
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **筛选**:
  - 分类：趋势反转
  - 方向：双向
  - 指标：Jurik Moving Average
  - 止损：是
  - 复杂度：基础
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中
