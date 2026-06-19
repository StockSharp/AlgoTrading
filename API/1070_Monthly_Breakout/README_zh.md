# Monthly Breakout 策略
[English](README.md) | [Русский](README_ru.md)

该策略只在选定的月份交易当前月份的高点或低点突破。可以选择在月高或月低做多或做空，持仓在固定的柱数后平仓。

## 详情

- **入场条件**：根据 `EntryOption` 与所选月份，如收盘价突破当月高点。
- **多空方向**：可配置。
- **出场条件**：持仓达到 `HoldingPeriod` 根K线后平仓。
- **止损**：无。
- **默认值**：
  - `EntryOption` = LongAtHigh
  - `HoldingPeriod` = 5
  - `CandleType` = TimeSpan.FromDays(1)
- **过滤器**：
  - 分类：Breakout
  - 方向：可配置
  - 指标：无
  - 止损：无
  - 复杂度：基础
  - 时间框架：日线
  - 季节性：有
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
