# 零滞后动量OSMA策略
[English](README.md) | [Русский](README_ru.md)

该策略使用五个动量指标构建零滞后的动量OSMA振荡器。
当振荡器两根K线前的值低于三根K线前的值时，被视为上升趋势。
此时如果最近一次的值高于两根K线前的值，则关闭空头仓位并可开多头仓位。
当两根K线前的值高于三根K线前的值时为下降趋势，策略将平掉多头仓位，若最近一次的值低于两根K线前的值，则可以开空头仓位。

## 参数

- `Smoothing1` – 慢速趋势的第一平滑系数。
- `Smoothing2` – OSMA线的第二平滑系数。
- `Factor1-5` – 各动量组件的权重。
- `MomentumPeriod1-5` – 动量指标的周期。
- `CandleType` – 计算所用的K线周期。
- `BuyOpen` – 允许开多头。
- `SellOpen` – 允许开空头。
- `BuyClose` – 允许平多头。
- `SellClose` – 允许平空头。

