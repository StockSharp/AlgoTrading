# VWAP Williams R Strategy
[English](README.md) | [Русский](README_ru.md)

本策略关注围绕VWAP的日内回归，结合Williams %R动量指标。当价格远离VWAP且%R进入超买或超卖区域时，预期价格将回到均值。

当%R跌破-80且价格位于VWAP下方时，表明卖压减弱，可能反弹；当%R高于-20且价格在VWAP上方时，意味着买盘耗尽，预计回落。策略按回归VWAP的方向开仓，并等待回归完成。

适合积极的日内交易者寻找频繁的均值回归机会。相对VWAP设置小止损限制风险，同时给价格留有波动空间。

## 细节
- **入场条件**:
  - 多头: `%R < -80 && Price < VWAP`
  - 空头: `%R > -20 && Price > VWAP`
- **多/空**: 双向
- **离场条件**:
  - 多头: 当价格突破VWAP上方平仓
  - 空头: 当价格跌破VWAP下方平仓
- **止损**: 是
- **默认值**:
  - `WilliamsRPeriod` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mixed
  - 方向: 双向
  - 指标: VWAP Williams R
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
