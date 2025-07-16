# Parabolic SAR CCI Strategy
[English](README.md) | [Русский](README_ru.md)

该策略结合抛物线SAR和CCI指标。当价格高于SAR且CCI < -100时做多；当价格低于SAR且CCI > 100时做空，对应超卖与超买的趋势。

适合在混合市场中寻找机会的交易者。

## 细节
- **入场条件**:
  - 多头: `Price > SAR && CCI < -100`
  - 空头: `Price < SAR && CCI > 100`
- **多/空**: 双向
- **离场条件**:
  - 多头: 价格跌破SAR时平仓
  - 空头: 价格升破SAR时平仓
- **止损**: 否
- **默认值**:
  - `SarAccelerationFactor` = 0.02m
  - `SarMaxAccelerationFactor` = 0.2m
  - `CciPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mixed
  - 方向: 双向
  - 指标: Parabolic SAR CCI
  - 止损: 否
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
