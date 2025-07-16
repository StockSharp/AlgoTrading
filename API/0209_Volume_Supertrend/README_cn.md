# Volume Supertrend Strategy
[English](README.md) | [Русский](README_ru.md)

本策略利用成交量与Supertrend指标。当成交量大于均值且价格在Supertrend上方时做多；当成交量大于均值且价格在Supertrend下方时做空，表明放量趋势。

适合在趋势市场中寻找机会的交易者。

## 细节
- **入场条件**:
  - 多头: `Volume > Avg(Volume) && Price > Supertrend`
  - 空头: `Volume > Avg(Volume) && Price < Supertrend`
- **多/空**: 双向
- **离场条件**:
  - 多头: Supertrend转向下时平仓
  - 空头: Supertrend转向上时平仓
- **止损**: 是
- **默认值**:
  - `VolumeAvgPeriod` = 20
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Trend
  - 方向: 双向
  - 指标: Volume Supertrend
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
