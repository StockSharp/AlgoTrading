# Ichimoku Adx Strategy
[English](README.md) | [Русский](README_ru.md)

本策略结合一目均衡表和ADX指标。做多条件：价格在云层上方，且转折线高于基准线，ADX>25；做空条件：价格在云层下方，且转折线低于基准线，ADX>25。价格穿越云层相反方向时平仓。

策略利用云图信号配合ADX过滤强势趋势。当价格突破云层并得到ADX确认时进场。适合偏好结构化趋势形态的交易者，ATR设定的止损帮助控制风险。

## 细节
- **入场条件**:
  - 多头: `Price > Cloud && Tenkan > Kijun && ADX > AdxThreshold`
  - 空头: `Price < Cloud && Tenkan < Kijun && ADX > AdxThreshold`
- **多/空**: 双向
- **离场条件**: 价格反向穿越云层
- **止损**: 使用云图作为跟踪止损
- **默认值**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **过滤器**:
  - 类别: Trend
  - 方向: 双向
  - 指标: Ichimoku Cloud, ADX
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
