# Supertrend Advance Pullback 策略
[English](README.md) | [Русский](README_ru.md)

Supertrend Advance Pullback 结合 Supertrend 的回调或反转入场，并使用 EMA、RSI、MACD 和 CCI 过滤器来优化信号。

## 细节

- **入场条件**: Supertrend 回调或翻转，配合 EMA、RSI、MACD、CCI 过滤
- **多空方向**: 双向
- **出场条件**: 相反信号
- **止损**: 无
- **默认值**:
  - `AtrLength` = 10
  - `Factor` = 3
  - `EmaLength` = 200
  - `UseEmaFilter` = true
  - `UseRsiFilter` = true
  - `RsiLength` = 14
  - `RsiBuyLevel` = 50
  - `RsiSellLevel` = 50
  - `UseMacdFilter` = true
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `UseCciFilter` = true
  - `CciLength` = 20
  - `CciBuyLevel` = 200
  - `CciSellLevel` = -200
  - `Mode` = Pullback
- **过滤器**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: Supertrend, EMA, RSI, MACD, CCI
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
