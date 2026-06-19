# 财务比率基本面策略
[English](README.md) | [Русский](README_ru.md)

该策略评估公司的季度财务比率，包括流动比率、利息覆盖率、应付周转率和毛利率。当这些指标之一相较上一期改善时，策略做多。

## 细节

- **入场条件**：
  - **多头**：`currentRatio > previousCurrent` 或 `interestCoverage < previousInterest` 或 `payableTurnover > previousPayable` 或 `grossMargin > previousGross`。
- **多空方向**：仅做多。
- **出场条件**：
  - **多头**：`currentRatio < previousCurrent` 或 `interestCoverage > previousInterest` 或 `payableTurnover < previousPayable` 或 `grossMargin < previousGross`。
- **止损**：无。
- **默认值**：
  - `K线类型` = 日线。
- **筛选器**：
  - 类别：基本面
  - 方向：多头
  - 指标：无
  - 止损：无
  - 复杂度：基础
  - 时间框架：长期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
