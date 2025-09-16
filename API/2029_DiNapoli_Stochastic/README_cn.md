# DiNapoli 随机指标策略

该策略基于 **DiNapoli 随机指标**，对 %K 与 %D 线的交叉做出反应。

## 策略逻辑

1. 订阅所选时间框的K线。
2. 使用带有平滑周期的标准随机指标计算 DiNapoli Stochastic 值。
3. 当上一根K线中 %K 高于 %D 时平仓空头仓位。
4. 当上一根K线中 %K 低于 %D 时平仓多头仓位。
5. 当 %K 向下穿越 %D 且允许做多时开多仓。
6. 当 %K 向上穿越 %D 且允许做空时开空仓。

## 参数

- `FastK` – %K 的基础周期。
- `SlowK` – %K 的平滑周期。
- `SlowD` – %D 的平滑周期。
- `BuyOpen` – 是否允许开多仓。
- `SellOpen` – 是否允许开空仓。
- `BuyClose` – 是否允许平多仓。
- `SellClose` – 是否允许平空仓。
- `CandleType` – 计算所用的K线时间框。

## 说明

策略使用 StockSharp 的高级 API，仅处理已完成的K线。指标值通过 `BindEx` 获取，不访问历史值。
