# 高低点策略
[English](README.md) | [Русский](README_ru.md)

基于最高价和最低价区间的策略。

当当前K线中点低于最高价和最低价平均值且归一化距离低于 LowThreshold 时买入。当中点高于平均值且归一化距离超过 HighThreshold 时平仓。

## 详情

- **入场条件**：中点低于平均值且归一化距离低于 LowThreshold。
- **多/空**：仅做多。
- **出场条件**：中点高于平均值且归一化距离高于 HighThreshold。
- **止损**：无。
- **默认值**：
  - `Range` = 100
  - `LowThreshold` = 15m
  - `HighThreshold` = 85m
  - `CandleType` = TimeSpan.FromMinutes(240)
- **筛选器**：
  - 类别：区间
  - 方向：做多
  - 指标：Highest, Lowest
  - 止损：无
  - 复杂度：基础
  - 时间框架：日内 (240m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
