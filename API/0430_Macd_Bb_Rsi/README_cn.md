# MACD + Bollinger Bands + RSI 策略
[English](README.md) | [Русский](README_ru.md)

该组合策略在 MACD 指示的趋势中寻找超出布林带的回调。当 MACD 为正但价格收于下轨之下且 RSI 处于超卖时买入，期待趋势延续；做空条件与此相反。

## 详情

- **入场条件**:
  - **多头**: `MACD > 0` 且 `Close < LowerBand` 且 `RSI < 30`
  - **空头**: `MACD < 0` 且 `Close > UpperBand` 且 `RSI > 70`
- **多空方向**: 双向
- **退出条件**: 反向信号
- **止损**: 无
- **默认值**:
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `RSILength` = 14
- **过滤器**:
  - 类型: 趋势跟随
  - 方向: 双向
  - 指标: MACD, 布林带, RSI
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 是
  - 风险等级: 中等
