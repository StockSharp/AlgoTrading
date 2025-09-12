# Global Index Spread RSI Strategy
[English](README.md) | [Русский](README_ru.md)

Global Index Spread RSI Strategy 是一种在 E-mini S&P 500 上进行交易的策略，当该指数与全球股指的价差出现超卖时入场。该价差以百分比计算并输入短周期 RSI。当 RSI 低于超卖阈值时开多，RSI 高于超买阈值时平仓。

## 细节
- **数据**: ES 与全球指数的日线收盘价。
- **入场条件**:
  - **多头**: 价差 RSI 低于 `OversoldThreshold`。
- **离场条件**: 价差 RSI 高于 `OverboughtThreshold`。
- **止损**: 无。
- **默认参数**:
  - `RsiLength` = 2
  - `OversoldThreshold` = 35
  - `OverboughtThreshold` = 78
- **过滤器**:
  - 类型: 均值回归
  - 方向: 多头
  - 指标: RSI
  - 复杂度: 低
  - 风险级别: 中等
