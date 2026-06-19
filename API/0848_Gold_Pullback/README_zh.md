# Gold Pullback Strategy
[English](README.md) | [Русский](README_ru.md)

Gold Pullback Strategy 是一种趋势策略。当价格在上升趋势中回调至 EMA21 且 MACD 与 TDI 同时看多时做多；当价格在下降趋势中回调至 EMA21 且 MACD 与 TDI 看空时做空。每笔交易的止盈和止损以信号蜡烛为基准，距离相等并加上一个偏移。

## 细节
- **数据**: 价格K线。
- **入场条件**:
  - **多头**: EMA14 高于 EMA60，K线触及 EMA21，MACD 线高于信号线，TDI MA 高于 TDI 信号且 RSI 高于 50。
  - **空头**: EMA14 低于 EMA60，K线触及 EMA21，MACD 线低于信号线，TDI MA 低于 TDI 信号且 RSI 低于 50。
- **离场条件**: 触发等距的止损或止盈。
- **止损**: `Offset` = 0.1 加到 K 线的最低/最高价。
- **默认参数**:
  - `EmaFastLength` = 14
  - `EmaSlowLength` = 60
  - `EmaPullbackLength` = 21
  - `SlOffset` = 0.1
- **过滤器**:
  - 类型: 趋势跟随
  - 方向: 多空皆可
  - 指标: EMA, MACD, RSI, SMA
  - 复杂度: 中等
  - 风险级别: 中等
