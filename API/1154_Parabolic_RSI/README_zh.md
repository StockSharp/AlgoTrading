# Parabolic RSI 策略
[English](README.md) | [Русский](README_ru.md)

该策略将 Parabolic SAR 应用于 RSI 用于趋势反转。当 SAR 相对 RSI 发生翻转时入场，并可使用 RSI 阈值过滤交易。

## 细节

- **入场条件**：
  - 多头：SAR 由上转下并且（可选）`RSI ≥ LongRsiMin`
  - 空头：SAR 由下转上并且（可选）`RSI ≤ ShortRsiMax`
- **多空**：可配置
- **出场条件**：SAR 反向翻转
- **止损**：无
- **默认值**：
  - `RsiLength` = 14
  - `SarStart` = 0.02
  - `SarIncrement` = 0.02
  - `SarMax` = 0.2
  - `LongRsiMin` = 50
  - `ShortRsiMax` = 50
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤器**：
  - 类别：趋势跟随
  - 方向：可配置
  - 指标：Parabolic SAR, RSI
  - 止损：否
  - 复杂度：中等
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
