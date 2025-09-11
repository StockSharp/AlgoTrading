# Reversal Catcher 策略
[English](README.md) | [Русский](README_ru.md)

Reversal Catcher 在价格突破布林带后又回到带内并伴随动量变化时入场。快慢 EMA 用于判断趋势方向，RSI 穿越超买或超卖水平提供信号。目标和止损基于布林带以及前一根蜡烛的极值，可选在指定的收盘时间平仓。

## 细节

- **入场条件**: 价格重新进入布林带并形成更高高/更低低，同时 RSI 穿越极值。
- **多空方向**: 双向
- **出场条件**: 止损、目标或日内收盘平仓
- **止损**: 前一根蜡烛的极值
- **默认值**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 1.5
  - `FastEmaPeriod` = 21
  - `SlowEmaPeriod` = 50
  - `RsiPeriod` = 14
  - `Overbought` = 70
  - `Oversold` = 30
  - `EndOfDay` = 1500
  - `CandleType` = 5 分钟
- **过滤器**:
  - 分类: 反转
  - 方向: 双向
  - 指标: Bollinger Bands, EMA, RSI
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

