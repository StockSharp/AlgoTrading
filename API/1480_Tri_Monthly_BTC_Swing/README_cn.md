# Tri-Monthly BTC Swing 策略
[English](README.md) | [Русский](README_ru.md)

Tri-Monthly BTC Swing 使用 EMA200、MACD 金叉和 RSI 过滤器。
该策略每 90 天只允许一笔交易。

## 细节

- **入场条件**: 收盘价高于 EMA200，MACD 线高于信号线，RSI 高于阈值，并且距离上次交易至少 90 天
- **多空方向**: 多头
- **出场条件**: MACD 线跌破信号线或 RSI 低于阈值
- **止损**: 无
- **默认值**:
  - `EmaLength` = 200
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiLength` = 14
  - `RsiThreshold` = 50
  - `TradeInterval` = 90 天
  - `CandleType` = 1 天
- **过滤器**:
  - 分类: 趋势跟随
  - 方向: 多头
  - 指标: EMA, MACD, RSI
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日线
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
