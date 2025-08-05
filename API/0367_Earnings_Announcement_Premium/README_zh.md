# Earnings Announcement Premium
[English](README.md) | [Русский](README_ru.md)

**Earnings Announcement Premium** 策略在财报发布前几天买入股票，并在财报发布后不久退出。

## 细节
- **入场条件**：在财报前 `DaysBefore` 天买入。
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
  - 指标：日历
  - 止损：无
  - 复杂度：初级
  - 时间框架：日线
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
