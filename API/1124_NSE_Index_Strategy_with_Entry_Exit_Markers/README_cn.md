# NSE 指数进出场标记策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格高于趋势 SMA 且 RSI 向上突破超卖水平时做多，并使用 ATR 设定止损和止盈。

## 细节

- **入场条件**：
  - **多头**：价格高于 SMA 且 RSI 向上穿越超卖水平。
- **多空方向**：仅多头。
- **出场条件**：
  - 价格触及 ATR 止损或止盈时平多。
- **止损**：基于 ATR 的止损和止盈。
- **默认值**：
  - `SmaPeriod` = 200。
  - `RsiPeriod` = 14。
  - `RsiOversold` = 40。
  - `AtrPeriod` = 14。
  - `AtrMultiplier` = 1.5。
  - `CandleType` = TimeSpan.FromDays(1).TimeFrame()。
- **筛选**：
  - 类型: 趋势
  - 方向: 多头
  - 指标: SMA, RSI, ATR
  - 止损: 基于 ATR
  - 复杂度: 基础
  - 时间框架: 日线
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
