# PulseWave Strategy
[English](README.md) | [Русский](README_ru.md)

该策略结合 VWAP、MACD 金叉/死叉 与 RSI 过滤器。

当价格高于 VWAP、MACD 向上穿越信号线且 RSI 低于超买阈值时买入；当价格跌破 VWAP、MACD 向下穿越信号线且 RSI 高于超卖阈值时平仓。

## 详情
- **入场条件**: 价格高于 VWAP，MACD 向上穿越，RSI 低于超买。
- **多空方向**: 仅做多。
- **退出条件**: 价格低于 VWAP，MACD 向下穿越，RSI 高于超卖。
- **止损**: 无。
- **默认值**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 多头
  - 指标: VWAP, MACD, RSI
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
