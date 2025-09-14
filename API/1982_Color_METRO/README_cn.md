# ColorMETRO 策略

该策略基于 ColorMETRO 指标，该指标在 RSI 周围构建快慢阶梯线。
当快线向上穿越慢线时开多仓，快线向下穿越慢线时开空仓。相反仓位在相同信号下平仓。

## 参数
- **Candle Type** – 计算所用的蜡烛类型。
- **RSI Period** – RSI 周期。
- **Fast Step** – 快线步长。
- **Slow Step** – 慢线步长。
- **Stop Loss** – 以点数表示的止损距离。
- **Take Profit** – 以点数表示的止盈距离。
- **Allow Buy** – 允许开多。
- **Allow Sell** – 允许开空。
- **Close Long** – 允许平多。
- **Close Short** – 允许平空。

策略使用 `StartProtection` 管理止损和止盈。
