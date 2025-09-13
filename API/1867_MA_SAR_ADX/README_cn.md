# MA SAR ADX 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合移动平均线、Parabolic SAR 和平均方向指数 (ADX)。
当价格高于移动平均线和 SAR 且 +DI 大于 -DI 时做多。
当价格低于移动平均线和 SAR 且 +DI 小于 -DI 时做空。
当价格穿越 SAR 时平仓。

## 细节

- **入场条件**：
  - 多头：`Close > MA && +DI >= -DI && Close > SAR`
  - 空头：`Close < MA && +DI <= -DI && Close < SAR`
- **方向**：多空双向
- **出场条件**：价格穿越 Parabolic SAR
- **止损**：无
- **默认值**：
  - `MaPeriod` = 100
  - `AdxPeriod` = 14
  - `SarStep` = 0.02m
  - `SarMax` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **筛选**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：SMA, Parabolic SAR, ADX
  - 止损：无
  - 复杂度：基础
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
