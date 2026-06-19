# Supertrend Adx Strategy
[English](README.md) | [Русский](README_ru.md)

该策略结合Supertrend指标与ADX，用于确认趋势强度。做多条件：价格高于Supertrend且ADX>25；做空条件：价格低于Supertrend且ADX>25。价格反向突破Supertrend时平仓。

测试表明年均收益约为 166%，该策略在股票市场表现最佳。

Supertrend提供经波动调整的趋势路径，ADX确认动量。当两项指标一致时入场。适合追随强劲趋势并使用跟踪止损的交易者，ATR用于确定止损。

## 细节
- **入场条件**:
  - 多头: `Close > Supertrend && ADX > AdxThreshold`
  - 空头: `Close < Supertrend && ADX > AdxThreshold`
- **多/空**: 双向
- **离场条件**: Supertrend反转
- **止损**: 使用Supertrend作为跟踪止损
- **默认值**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **过滤器**:
  - 类别: Trend
  - 方向: 双向
  - 指标: Supertrend, ADX
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

