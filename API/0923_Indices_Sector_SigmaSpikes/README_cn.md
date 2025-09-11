# 指数板块Sigma尖峰策略
[English](README.md) | [Русский](README_ru.md)

该策略扫描多个板块指数，并根据收益波动率记录Sigma尖峰。

## 详情

- **入场条件**：无，仅用于筛选。
- **多空方向**：无。
- **出场条件**：无。
- **止损**：无。
- **默认值**：
  - `LookbackPeriod` = 20。
  - `ReturnPeriod` = 20。
  - `SigmaThreshold` = 2。
  - `CandleType` = TimeSpan.FromDays(1).TimeFrame()。
- **筛选**：
  - 类别：Indicator
  - 方向：无
  - 指标：StdDev
  - 止损：无
  - 复杂度：Basic
  - 时间框架：Daily
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险水平：Low
