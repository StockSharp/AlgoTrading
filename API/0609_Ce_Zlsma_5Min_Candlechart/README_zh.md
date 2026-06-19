# CE ZLSMA 5MIN Candlechart 策略
[English](README.md) | [Русский](README_ru.md)

基于 Zero Lag LSMA 的趋势跟随策略，使用 Heikin Ashi 蜡烛并结合 Chandelier Exit 过滤。当前方向转多且收盘价高于 ZLSMA 时做多。

## 细节

- **入场条件**:
  - 多头: 方向转向上，Heikin Ashi 收盘价高于 ZLSMA 且高于开盘价
- **多/空**: 多头
- **出场条件**:
  - 多头: 收盘价跌破 ZLSMA
- **止损**: 无
- **默认值**:
  - `ZlsmaLength` = 50
  - `AtrPeriod` = 1
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤器**:
  - 分类: 趋势跟随
  - 方向: 多头
  - 指标: ZLSMA, ATR, Heikin Ashi
  - 止损: 否
  - 复杂度: 中等
  - 时间框架: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
