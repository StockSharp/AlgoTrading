# Color XMACD Candle 策略

本策略在 StockSharp 中实现了 “ColorXMACDCandle” 专家顾问。它使用 MACD 指标，根据柱状图或信号线颜色的变化来产生交易信号。

## 思路

策略分析 MACD 某个组成部分的斜率：

- **Histogram 模式** – 新柱比上一柱更高表示多头动能增强；下降表示空头动能增强。
- **SignalLine 模式** – 使用 MACD 信号线的斜率。向上斜率是买入信号，向下斜率是卖出信号。

当所选组件转向上且之前不是上升时，策略可以关闭空头仓位并开多。当组件转向下且之前不是下降时，策略可以关闭多头仓位并开空。每个动作都可以通过参数单独启用或禁用。

## 参数

- `Mode` – 信号来源：`Histogram` 或 `SignalLine`。
- `FastPeriod` – MACD 快速 EMA 周期。
- `SlowPeriod` – MACD 慢速 EMA 周期。
- `SignalPeriod` – MACD 信号线平滑周期。
- `EnableBuyOpen` – 允许开多。
- `EnableSellOpen` – 允许开空。
- `EnableBuyClose` – 允许平多。
- `EnableSellClose` – 允许平空。
- `CandleType` – 计算使用的K线类型。

## 交易规则

1. 订阅选定的K线并计算 MACD。
2. 根据模式跟踪柱状图或信号线的斜率。
3. 当斜率转向上时，若允许则平空，并可开多。
4. 当斜率转向下时，若允许则平多，并可开空。

策略未实现止损和止盈功能，如需风险管理可在外部添加。
