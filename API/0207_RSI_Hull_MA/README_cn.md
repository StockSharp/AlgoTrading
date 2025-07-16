# RSI Hull MA Strategy
[English](README.md) | [Русский](README_ru.md)

此策略结合RSI与Hull移动平均线。当RSI低于30且HMA上升时做多；当RSI高于70且HMA下降时做空，分别对应超卖和超买状态。

适合在混合市场中寻找机会的交易者。

## 细节
- **入场条件**:
  - 多头: `RSI < 30 && HMA(t) > HMA(t-1)`
  - 空头: `RSI > 70 && HMA(t) < HMA(t-1)`
- **多/空**: 双向
- **离场条件**:
  - 多头: RSI回到中性区域时平仓
  - 空头: RSI回到中性区域时平仓
- **止损**: 是
- **默认值**:
  - `RsiPeriod` = 14
  - `HullPeriod` = 9
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mixed
  - 方向: 双向
  - 指标: RSI Hull MA
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
