# RSI Bollinger Bands
[English](README.md) | [Русский](README_ru.md)

该策略将相对强弱指数（RSI）与布林带结合。 当RSI低于超卖阈值且收盘价低于下轨时开多单。 当RSI高于超买阈值且收盘价高于上轨时开空单。 在相反信号出现时反向开仓。

## 细节

- **入场条件**：RSI低于`RsiOversold`且收盘价低于下轨买入；RSI高于`RsiOverbought`且收盘价高于上轨卖出。
- **多空方向**：双向。
- **出场条件**：相反信号。
- **止损**：无。
- **默认值**：
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `RsiPeriod` = 20
  - `BollingerPeriod` = 20
  - `BollingerWidth` = 2
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
- **筛选器**：
  - 分类：振荡器
  - 方向：双向
  - 指标：RSI，布林带
  - 止损：否
  - 复杂度：基础
  - 时间框架：15分钟
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：中等
