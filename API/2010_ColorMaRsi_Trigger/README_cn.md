# ColorMaRsi Trigger 策略

该策略是原始 MQL5 专家 `exp_colormarsi-trigger.mq5` 的 StockSharp 版本。它比较快慢 EMA 以及快慢 RSI 的值，组合信号取值 -1、0 或 +1。当当前信号与前一个信号符号不同时开仓。

## 工作原理

- 当信号从正值变为零或负值时，平掉空头并开多头。
- 当信号从负值变为零或正值时，平掉多头并开空头。

## 参数

- **Fast EMA** – 快速指数移动平均线周期。
- **Slow EMA** – 慢速指数移动平均线周期。
- **Fast RSI** – 快速 RSI 周期。
- **Slow RSI** – 慢速 RSI 周期。
- **Candle Type** – 用于计算的蜡烛时间框。

## 指标

- 指数移动平均线（快和慢）
- 相对强弱指数（快和慢）

策略只处理收盘完成的蜡烛，并使用 `BuyMarket` 和 `SellMarket` 进行交易。
