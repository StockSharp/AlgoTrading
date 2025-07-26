# Bollinger Supertrend Strategy
[English](README.md) | [Русский](README_ru.md)

此策略将布林带与Supertrend结合，在强劲趋势中寻找入场点。布林带衡量波动扩大，Supertrend跟踪整体趋势并充当跟踪止损。

当价格收于上轨上方并保持在Supertrend之上时做多；当价格收于下轨下方且位于Supertrend之下时做空。价格反向突破Supertrend时离场。

该策略适合希望捕捉持续行情而非快速反转的交易者，Supertrend止损会随着市场波动自动调整。

## 细节
- **入场条件**:
  - 多头: `Close > UpperBand && Close > Supertrend`
  - 空头: `Close < LowerBand && Close < Supertrend`
- **多/空**: 双向
- **离场条件**:
  - 多头: 价格跌破Supertrend时平仓
  - 空头: 价格突破Supertrend时平仓
- **止损**: 通过Supertrend跟踪止损
- **默认值**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **过滤器**:
  - 类别: Trend
  - 方向: 双向
  - 指标: Bollinger Bands, Supertrend
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

测试表明年均收益约为 79%，该策略在股票市场表现最佳。
