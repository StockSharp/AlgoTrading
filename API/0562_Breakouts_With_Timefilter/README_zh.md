# Breakouts With Timefilter 策略
[English](README.md) | [Русский](README_ru.md)

该策略在指定交易时段内，当价格突破最近的高点或低点时入场。可选的移动平均过滤器用于确认方向。止损可基于ATR、蜡烛极值或固定点数，并具有可配置的风险回报比。

## 细节

- **入场条件**：
  - **多头**：收盘价 > `Length` 周期最高价并处于时间窗口内；可选收盘价 > MA。
  - **空头**：收盘价 < `Length` 周期最低价并处于时间窗口内；可选收盘价 < MA。
- **方向**：双向
- **止损**：ATR、蜡烛极值或固定点数并设定目标比率
- **默认参数**：
  - `Length` = 5
  - `MaLength` = 99
  - `UseMaFilter` = false
  - `UseTimeFilter` = true (14:30–15:00)
  - `SlType` = Atr
  - `SlLength` = 0
  - `AtrLength` = 14
  - `AtrMultiplier` = 0.5
  - `PointsStop` = 50
  - `RiskReward` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
