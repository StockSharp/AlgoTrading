# 归一化振荡器蜘蛛图策略

该策略计算多个振荡器（RSI、Stochastic、Correlation、Money Flow Index、Williams %R、Percent Up、Chande Momentum Oscillator 和 Aroon Oscillator）。所有数值被归一化到0-1范围，并求平均生成交易信号。当平均值高于0.6时买入，低于0.4时做空。

## 参数
- **Length** — 振荡器计算周期
- **Candle type** — 使用的K线周期

## 说明
这是 TradingView 脚本“Normalized Oscillators Spider Chart [LuxAlgo]”的简化移植，用于展示在 StockSharp 中的指标使用。
