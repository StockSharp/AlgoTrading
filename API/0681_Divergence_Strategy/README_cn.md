# 背离策略
[English](README.md) | [Русский](README_ru.md)

该策略利用价格与RSI在简单枢轴上的背离。价格创出新高但RSI未确认时做空，价格创新低而RSI上升时做多。

## 详情
- **入场条件**: 价格与RSI背离。
- **多空方向**: 双向（可配置）。
- **退出条件**: RSI反向信号或保护性订单。
- **止损**: 是（止损和止盈）。
- **默认值**:
  - `TradeDirection` = Both
  - `RsiPeriod` = 14
  - `StopLossPercent` = 2m
  - `RiskReward` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: RSI
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 是
  - 风险等级: 中
