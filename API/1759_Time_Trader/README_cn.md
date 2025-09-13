# Time Trader Strategy
[English](README.md) | [Русский](README_ru.md)

该策略在设定的时间开仓多头和/或空头，并设置固定的止损与止盈。适用于测试基于时间的入场，无需任何指标确认。

## 细节

- **入场条件**：在设定的小时和分钟触发。
- **多/空**：双向（可配置）。
- **出场条件**：止损或止盈。
- **止损**：有。
- **默认值**：
  - `TradeHour` = 0
  - `TradeMinute` = 0
  - `AllowBuy` = true
  - `AllowSell` = true
  - `TakeProfitTicks` = 20
  - `StopLossTicks` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选**：
  - 分类：其它
  - 方向：双向
  - 指标：无
  - 止损：固定
  - 复杂度：基础
  - 时间框架：日内 (1分钟)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
