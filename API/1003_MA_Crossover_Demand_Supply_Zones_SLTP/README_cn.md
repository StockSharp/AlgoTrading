# MA Crossover Demand Supply Zones SLTP 策略
[English](README.md) | [Русский](README_ru.md)

该策略将长短期简单移动平均线交叉与需求/供应区结合。当交叉发生在最近确认的需求区或供应区附近时入场，并以固定百分比的止损和止盈管理仓位。

## 细节

- **入场条件**：
  - 多头：短期SMA在需求区附近上穿长期SMA。
  - 空头：短期SMA在供应区附近下穿长期SMA。
- **方向**：双向。
- **出场条件**：
  - 价格触及止盈或止损。
- **止损**：按百分比的止损和止盈。
- **默认值**：
  - `ShortMaLength` = 9
  - `LongMaLength` = 21
  - `ZoneLookback` = 50
  - `ZoneStrength` = 2
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 2
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：SMA, Highest, Lowest
  - 止损：是
  - 复杂度：基础
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中
