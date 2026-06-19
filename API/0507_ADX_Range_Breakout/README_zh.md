# ADX Range Breakout Strategy
[English](README.md) | [Русский](README_ru.md)

该策略在收盘价突破近期最高收盘价且 ADX 低于设定阈值时做多，表明市场处于低波动状态。交易仅在指定的会话内进行，并限制每日最大交易次数。每笔持仓都使用固定价差的止损进行保护。

## 详情

- **入场条件**：在交易会话内 `收盘价 >= 之前的最高收盘价` 且 `ADX < 阈值`
- **多空方向**：仅做多
- **出场条件**：止损或会话结束
- **止损**：有
- **默认值**：
  - `AdxPeriod` = 14
  - `HighestPeriod` = 34
  - `AdxThreshold` = 17.5
  - `StopLoss` = 1000
  - `MaxTradesPerDay` = 3
  - `CandleType` = TimeSpan.FromMinutes(30)
- **筛选**：
  - 类别：突破
  - 方向：多头
  - 指标：ADX
  - 止损：有
  - 复杂度：入门
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
