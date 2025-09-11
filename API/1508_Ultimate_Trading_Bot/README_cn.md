# Ultimate Trading Bot
[English](README.md) | [Русский](README_ru.md)

只做多策略，结合RSI、均线、MACD和随机指标的交叉来确定进出场。

## 详情

- **入场条件**：RSI上穿超卖且价格高于均线，同时MACD和随机指标上穿。
- **多空方向**：仅多头。
- **出场条件**：相反交叉条件。
- **止损**：无显式止损。
- **默认值**：
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MaLength` = 50
  - `StochLength` = 14
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
- **筛选**：
  - 类型：动量
  - 方向：多头
  - 指标：RSI, MA, MACD, Stochastic
  - 止损：无
  - 复杂度：中等
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：中
