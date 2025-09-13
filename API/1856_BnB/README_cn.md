# BnB 策略

该策略来源于 MetaTrader 5 EA “Exp_BnB”。它使用自定义的 BnB（多空力量）指标，在每根 K 线内部评估多头和空头压力，并用指数移动平均进行平滑。

## 工作原理

1. 对每根已完成的 K 线计算 bulls 和 bears 值。
2. 两个序列都使用 EMA 平滑。
3. 当 bulls 线向上穿越 bears 线时：
   - 平掉所有空头仓位；
   - 开立多头仓位。
4. 当 bears 线向上穿越 bulls 线时：
   - 平掉所有多头仓位；
   - 开立空头仓位。
5. 止损和止盈以绝对价格点设置。

## 参数

- `Candle Type` – 计算所用的 K 线周期；
- `EMA Length` – 平滑周期；
- `Stop Loss` – 止损距离；
- `Take Profit` – 止盈距离；
- `Allow Long Entry` – 允许开多；
- `Allow Short Entry` – 允许开空；
- `Allow Long Exit` – 允许平多；
- `Allow Short Exit` – 允许平空。

## 说明

原始指标支持多种平滑方法，本策略使用标准指数移动平均进行近似。
