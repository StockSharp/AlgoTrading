# BTC Seasonality 策略
[English](README.md) | [Русский](README_ru.md)

该策略根据 EST 时间的星期和小时规则开仓和平仓。用户可设置入场日和小时、出场日和小时，以及做多或做空方向。策略在指定的入场时间开仓，并在指定的出场时间平仓。

## 细节

- **入场条件**：
  - 当前 EST 的星期与小时分别等于 `EntryDay` 和 `EntryHour`。
- **多空方向**：可配置。
- **出场条件**：
  - 当前 EST 的星期与小时分别等于 `ExitDay` 和 `ExitHour`。
- **止损**：无。
- **默认值**：
  - `EntryDay` = Saturday
  - `ExitDay` = Monday
  - `EntryHour` = 10
  - `ExitHour` = 10
  - `IsLong` = true
- **筛选器**：
  - 分类：季节性
  - 方向：可配置
  - 指标：无
  - 止损：无
  - 复杂度：低
  - 时间框架：任意
  - 季节性：是
  - 神经网络：否
  - 背离：否
  - 风险等级：低
