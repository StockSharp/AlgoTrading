# EMA MACD RSI 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合 EMA 趋势过滤、MACD 交叉和 RSI 水平。

当快速 EMA 高于慢速 EMA、MACD 向上穿越信号线且 RSI 位于 RsiBuyLevel 与 70 之间时买入。当快速 EMA 低于慢速 EMA、MACD 向下穿越信号线且 RSI 位于 30 与 RsiSellLevel 之间时卖出。

## 详情
- **入场条件**: EMA 趋势过滤、MACD 交叉、RSI 水平。
- **多空方向**: 双向。
- **退出条件**: 反向信号。
- **止损**: 否。
- **默认值**:
  - `FastEmaLength` = 50
  - `SlowEmaLength` = 200
  - `RsiLength` = 14
  - `RsiBuyLevel` = 45m
  - `RsiSellLevel` = 55m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: EMA, MACD, RSI
  - 止损: 否
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
