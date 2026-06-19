# 多指标多空策略
[Русский](README_ru.md) | [English](README.md)

该策略结合 RSI、ROC 和可选择的均线来产生多空信号，并使用基于 ATR 的跟踪止损退出。

## 细节

- **入场条件**：
  - 多头：RSI 位于超卖与超买之间，ROC > 0 且价格在均线上方。
  - 空头：确认的空头趋势，ROC < 0 且价格在均线下方。
- **方向**：做多和做空。
- **出场条件**：
  - ATR 跟踪止损或指标停止条件。
- **止损**：ATR 跟踪止损。
- **默认值**：
  - `RsiLength` = 5
  - `RsiOverbought` = 70
  - `RsiOversold` = 44
  - `RocLength` = 4
  - `MaLength` = 24
  - `MaTypeParam` = TEMA
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `BearishMaLength` = 200
  - `BearishTrendDuration` = 5
- **过滤器**：
  - 类别：趋势跟随
  - 方向：多 & 空
  - 指标：RSI, ROC, MA, ATR
  - 止损：是
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
