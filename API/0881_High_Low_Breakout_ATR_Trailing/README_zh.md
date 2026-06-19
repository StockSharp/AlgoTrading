# High-Low Breakout ATR Trailing Stop 策略
[English](README.md) | [Русский](README_ru.md)

该策略交易首个30分钟区间的突破。当价格突破初始高点或低点时，开启仓位并使用ATR跟踪止损。所有仓位在设定的盘中时间平仓。

## 细节
- **入场条件**：
  - **多头**：收盘价突破首个30分钟高点
  - **空头**：收盘价跌破首个30分钟低点
- **多空方向**：可配置 (`Direction`)
- **出场条件**：
  - ATR 跟踪止损或对称目标
  - 在 `ExitHour:ExitMinute` 平掉所有仓位
- **止损**：是，基于ATR。
- **默认值**：
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 3.5m
  - `RiskPerTrade` = 2m
  - `AccountSize` = 10000m
  - `SessionStartHour` = 9
  - `SessionStartMinute` = 15
  - `ExitHour` = 15
  - `ExitMinute` = 15
  - `CandleType` = TimeSpan.FromMinutes(30)
- **过滤器**：
  - 类别：突破
  - 方向：可配置
  - 指标：ATR
  - 止损：是
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
