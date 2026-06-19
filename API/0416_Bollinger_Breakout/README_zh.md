# Bollinger Breakout
[English](README.md) | [Русский](README_ru.md)

Bollinger Breakout 策略旨在捕捉突破布林带后继续发展的行情。
当收盘价高于上轨或低于下轨时，如启用的过滤器全部同意，便在突破方向开仓。

可选过滤器包括 RSI、Aroon 和移动平均线，用于验证动量和趋势。
还可以启用止损以控制风险。平仓发生在价格触及相反带或止损触发时。

该策略适用于趋势性市场，在此环境中带的突破通常会延续。

## 细节
- **数据**: 价格K线。
- **入场条件**:
  - **多头**: 收盘价高于上轨且所有启用过滤器确认。
  - **空头**: 收盘价低于下轨且所有启用过滤器确认。
- **离场条件**: 触及相反带或在 `UseSL` 启用时触发止损。
- **止损**: 可选止损（`UseSL`）。
- **默认参数**:
  - `UseRSI` = True
  - `UseAroon` = False
  - `UseMA` = True
  - `UseSL` = True
- **过滤器**:
  - 类型: 突破
  - 方向: 多空皆可
  - 指标: Bollinger Bands, RSI, Aroon, Moving Average
  - 复杂度: 中等
  - 风险级别: 高
