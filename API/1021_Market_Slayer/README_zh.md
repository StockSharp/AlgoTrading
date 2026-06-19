# Market Slayer 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用加权移动平均线交叉，并通过高时间框架的SSL趋势确认。短期WMA上穿长期WMA且趋势为多头时开多；相反条件下开空。可以选择启用以点数表示的止盈和止损。

## 细节

- **入场条件**：
  - **多头**：短期WMA上穿长期WMA且高周期SSL为多头。
  - **空头**：短期WMA下破长期WMA且高周期SSL为空头。
- **多空方向**：双向。
- **出场条件**：
  - 趋势过滤器转向相反方向。
  - 可选的止损或止盈。
- **止损**：可选。
- **默认值**：
  - `ShortLength` = 10。
  - `LongLength` = 20。
  - `ConfirmationTrendValue` = 2。
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()。
  - `TrendCandleType` = TimeSpan.FromMinutes(240).TimeFrame()。
  - `TakeProfitEnabled` = false。
  - `TakeProfitValue` = 20。
  - `StopLossEnabled` = false。
  - `StopLossValue` = 50。
- **筛选**：
  - 类型: 趋势跟随
  - 方向: 双向
  - 指标: WMA, SSL
  - 止损: 可选
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
