# Bollinger Winner Lite
[English](README.md) | [Русский](README_ru.md)

Bollinger Winner Lite 是一个简化的布林带反转系统，关注价格大幅超出带之后的快速回归。
`CandlePercent` 参数限定入场蜡烛需要相对于近期波动足够大，从而过滤微小噪音。
默认只做多，开启 `ShowShort` 后可进行对称的做空。

当价格回到中轨或触及相反带时平仓。策略默认没有固定止损，
完全依赖均值回归。

## 细节
- **数据**: 价格K线。
- **入场条件**:
  - **多头**: 收盘价低于下轨且蜡烛大小 > `CandlePercent`。
  - **空头**: 收盘价高于上轨且蜡烛大小 > `CandlePercent`（需启用 `ShowShort`）。
- **离场条件**: 触及中轨或相反带。
- **止损**: 默认无。
- **默认参数**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `CandlePercent` = 30
  - `ShowShort` = false
- **过滤器**:
  - 类型: 均值回归
  - 方向: 默认仅多头
  - 指标: Bollinger Bands
  - 复杂度: 简单
  - 风险级别: 中等
