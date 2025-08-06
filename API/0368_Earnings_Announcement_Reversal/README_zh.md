# Earnings Announcement Reversal
[English](README.md) | [Русский](README_ru.md)

**Earnings Announcement Reversal** 策略在财报公布日做空近期赢家并买入近期输家。

## 细节
- **入场条件**：在财报日，做空最近收益为正的股票，买入收益为负的股票。
- **方向**：双向。
- **出场条件**：信号后调整仓位，没有明确持有规则。
- **止损**：无。
- **默认值**：
  - `LookbackDays = 5`
  - `HoldingDays = 3`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **筛选**：
  - 类别：事件驱动
  - 方向：双向
  - 指标：收益
  - 止损：无
  - 复杂度：中等
  - 时间框架：日线
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
