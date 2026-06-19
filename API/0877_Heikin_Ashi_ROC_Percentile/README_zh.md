# Heikin Ashi ROC 百分位策略
[Русский](README_ru.md) | [English](README.md)

该策略将K线转换为Heikin Ashi，并用SMA平滑收盘价后计算其变化率（ROC）。最近ROC高低点的百分位区间形成突破带。ROC上穿下方带时做多或反转为多，ROC下穿上方带时做空。

## 详情

- **入场条件**：
  - 多头：ROC 上穿下方百分位线。
  - 空头：ROC 下穿上方百分位线。
- **多头/空头**：双向。
- **出场条件**：反向信号或止损。
- **止损**：百分比止损。
- **默认值**：
  - `RocLength` = 100
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
  - `StartDate` = new DateTimeOffset(2015, 3, 3, 0, 0, 0, TimeSpan.Zero)
- **筛选器**：
  - 分类：突破
  - 方向：双向
  - 指标：Heikin Ashi, RateOfChange, Highest, Lowest
  - 止损：有
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
