# Earnings Announcements With Buybacks
[English](README.md) | [Русский](README_ru.md)

**Earnings Announcements With Buybacks** 策略在公司财报前几天若存在股票回购计划则买入，并在财报后不久卖出。

## 细节
- **入场条件**：若公司有回购，在财报前 `DaysBefore` 天买入。
- **方向**：仅多头。
- **出场条件**：在财报后 `DaysAfter` 天卖出。
- **止损**：无。
- **默认值**：
  - `DaysBefore = 5`
  - `DaysAfter = 1`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **筛选**：
  - 类别：事件驱动
  - 方向：多头
  - 指标：回购 + 日历
  - 止损：无
  - 复杂度：中等
  - 时间框架：日线
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
