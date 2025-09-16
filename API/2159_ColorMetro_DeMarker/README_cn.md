# ColorMetro DeMarker 策略

**ColorMetro DeMarker 策略** 是 MQL5 专家顾问 `Exp_ColorMETRO_DeMarker` 的 StockSharp 实现。
该策略使用带有阶梯级别的 DeMarker 指标来生成交易信号。

## 参数
- **DeMarker Period** – DeMarker 指标周期。
- **Fast Step** – 用于构建快速级别（MPlus）的步长。
- **Slow Step** – 用于构建慢速级别（MMinus）的步长。
- **Candle Type** – 用于分析的蜡烛时间框架。
- **Enable Buy Open** – 允许开多头。
- **Enable Sell Open** – 允许开空头。
- **Enable Buy Close** – 允许平多头。
- **Enable Sell Close** – 允许平空头。

## 交易逻辑
1. 将 DeMarker 值缩放到 0–100，并根据快慢步长计算两个动态级别（MPlus 与 MMinus）。
2. 当上一根柱的快速级别高于慢速级别且当前快速级别向下穿越慢速级别时，策略买入并可选择性地平掉空单。
3. 当上一根柱的快速级别低于慢速级别且当前快速级别向上穿越慢速级别时，策略卖出并可选择性地平掉多单。
4. 所有计算仅使用已完成的蜡烛。

该方法可跟随由阶梯 DeMarker 级别指示的趋势变化。
