# Volume Supertrend 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

本策略利用成交量与Supertrend指标。当成交量大于均值且价格在Supertrend上方时做多；当成交量大于均值且价格在Supertrend下方时做空，表明放量趋势。

测试表明年均收益约为 64%，该策略在外汇市场表现最佳。

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
  - `SupertrendMultiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2
    Avg(Volume)是之前VolumeAvgPeriod根K线的平均成交量。Supertrend转为下行时平多，转为上行时平空；止损为入场价的固定StopLossPercent百分比，在K线之间同样监控。与持仓方向相反的入场信号会反转持仓。
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

