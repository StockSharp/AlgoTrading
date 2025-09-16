# Exp MAMA 策略

该策略基于 MESA 自适应移动平均线 (MAMA) 指标进行交易。

指标包含两条线：

- **MAMA** – 自适应移动平均线。
- **FAMA** – 用作信号线的跟随平均线。

交易逻辑：

1. 当 MAMA 向下穿越 FAMA 时，策略平掉空头并开多。
2. 当 MAMA 向上穿越 FAMA 时，策略平掉多头并开空。

## 参数

- `FastLimit` – 自适应系数的上限。
- `SlowLimit` – 自适应系数的下限。
- `CandleType` – 使用的蜡烛图时间框架。
- `BuyOpen` / `SellOpen` – 允许开多或开空。
- `BuyClose` / `SellClose` – 允许平多或平空。

该策略只处理已完成的蜡烛，并使用市价单进行进出场。
