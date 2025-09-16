# Color Zerolag Momentum X2 策略
[English](README.md) | [Русский](README_ru.md)

该策略在两个时间框架上使用 Momentum 指标与其零滞后均线的交叉。高时间框架确定趋势方向，低时间框架在 Momentum 向趋势方向穿越其零滞后均线时触发进场。

## 细节

- **入场条件**：Momentum 向趋势方向穿越其零滞后均线
- **多/空**：双向
- **出场条件**：反向穿越或趋势反转
- **止损**：无
- **默认值**：
  - `TrendCandleType` = 6h
  - `TrendMomentumPeriod` = 34
  - `TrendMaLength` = 15
  - `SignalCandleType` = 30m
  - `SignalMomentumPeriod` = 34
  - `SignalMaLength` = 15
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：Momentum，ZeroLagEMA
  - 止损：无
  - 复杂度：中等
  - 时间框架：多时间框架
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
