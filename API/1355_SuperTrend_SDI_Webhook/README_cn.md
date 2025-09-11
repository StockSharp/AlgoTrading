# SuperTrend SDI Webhook 策略
[Русский](README_ru.md) | [English](README.md)

该策略结合 SuperTrend 和平滑方向指标 (SDI)。当 +DI 大于 -DI 且 SuperTrend 显示上升趋势时开多，当 -DI 大于 +DI 且 SuperTrend 显示下降趋势时开空。策略使用百分比止盈、止损和移动止损。

## 详情

- **入场条件**：
  - 多头：`+DI > -DI 且 SuperTrend 上升`
  - 空头：`-DI > +DI 且 SuperTrend 下降`
- **方向**：双向
- **出场条件**：止盈、止损或移动止损
- **指标**：SuperTrend, AverageDirectionalIndex
- **止损类型**：百分比
- **默认值**：
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 1.8
  - `DiLength` = 3
  - `DiSmooth` = 7
  - `TakeProfitPercent` = 25
  - `StopLossPercent` = 4.8
  - `TrailingPercent` = 1.9
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤器**：
  - 类别：趋势
  - 方向：双向
  - 指标：SuperTrend, SDI
  - 止损：是
  - 复杂度：基础
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
