# Ticker Pulse Meter + Fear EKG 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合长短周期寻找超卖并捕捉反弹。
当组合百分比突破上限时买入，在利润目标下穿时卖出。

## 详情

- **入场条件**：百分比突破 `EntryThresholdHigh` 或跌破 `OrangeEntryThreshold`
- **多空**：仅做多
- **出场条件**：跌破 `ProfitTake`
- **止损**：无
- **默认值**：
  - `LookbackShort` = 50
  - `LookbackLong` = 200
  - `ProfitTake` = 95
  - `EntryThresholdHigh` = 20
  - `EntryThresholdLow` = 40
  - `OrangeEntryThreshold` = 95
- **过滤器**：
  - 分类：振荡器
  - 方向：多头
  - 指标：Highest, Lowest
  - 止损：无
  - 复杂度：高级
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中等
