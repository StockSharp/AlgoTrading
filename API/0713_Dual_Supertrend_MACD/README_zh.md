# Dual Supertrend MACD
[English](README.md) | [Русский](README_ru.md)

**Dual Supertrend MACD** 策略结合两条 Supertrend 与 MACD 筛选。
当价格位于两条 Supertrend 之上且 MACD 柱线为正时做多；
当价格位于两条 Supertrend 之下且 MACD 柱线为负时做空。
任一 Supertrend 反向或 MACD 柱线穿越零轴时平仓。

## 详情
- **数据**: 价格K线。
- **入场条件**:
  - 多头: `Close > Supertrend1 && Close > Supertrend2 && MACD Histogram > 0`
  - 空头: `Close < Supertrend1 && Close < Supertrend2 && MACD Histogram < 0`
- **出场条件**:
  - 多头: `Close < Supertrend1 || Close < Supertrend2 || MACD Histogram < 0`
  - 空头: `Close > Supertrend1 || Close > Supertrend2 || MACD Histogram > 0`
- **止损**: 默认无。
- **默认参数**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `OscillatorMaType` = Exponential
  - `SignalMaType` = Exponential
  - `AtrPeriod1` = 10
  - `Factor1` = 3.0
  - `AtrPeriod2` = 20
  - `Factor2` = 5.0
  - `TradeDirection` = "Both"
- **筛选**:
  - 类别: 趋势跟随
  - 方向: 可配置
  - 指标: Supertrend, MACD
  - 复杂度: 中等
  - 风险级别: 中等
