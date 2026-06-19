# EUR/USD 多层线性回归策略
[English](README.md) | [Русский](README_ru.md)

该策略使用多个线性回归层来判断 EUR/USD 的趋势方向。策略计算短期、中期和长期回归，并通过 R² 和斜率阈值验证，然后根据加权结果进行交易。

## 细节

- **入场条件**：
  - 多头：加权斜率 > 0 且可靠性 > 0.5
  - 空头：加权斜率 < 0 且可靠性 > 0.5
- **方向**：双向
- **出场条件**：出现反向信号时反手
- **止损**：日亏损保护
- **默认值**：
  - `ShortLength` = 20
  - `MediumLength` = 50
  - `LongLength` = 100
  - `MinRSquared` = 0.45m
  - `SlopeThreshold` = 0.00005m
  - `WeightShort` = 0.4m
  - `WeightMedium` = 0.35m
  - `WeightLong` = 0.25m
  - `PositionSizePct` = 50m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `MaxDailyLossPct` = 12m
- **筛选**：
  - 类型：趋势
  - 方向：双向
  - 指标：Linear Regression
  - 止损：有
  - 复杂度：高级
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 风险等级：中
