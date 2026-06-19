# Bitcoin Bullish Percent Index 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用RSI指标来近似比特币看涨百分比指数。当RSI上穿超卖水平时做多，当RSI下穿超买水平时做空。

## 细节

- **入场条件**：
  - **多头**：RSI上穿超卖水平。
  - **空头**：RSI下穿超买水平。
- **多空方向**：双向。
- **出场条件**：反向信号。
- **止损**：无。
- **默认值**：
  - `RSI Period` = 14
  - `Overbought` = 70
  - `Oversold` = 30
- **过滤器**：
  - 分类：振荡器
  - 方向：双向
  - 指标：RSI
  - 止损：无
  - 复杂度：低
  - 时间框架：中期
