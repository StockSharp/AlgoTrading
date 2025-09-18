# 趋势背离交易者（经典转换）

该策略将 MetaTrader 4 的 **Divergence Trader** 专家顾问迁移到 StockSharp 的高级 API。系统在所选蜡烛价格（默认使用开盘价）上计算两条简单移动平均线，并观察快速均线与慢速均线之间的距离如何随每根柱线变化：

* 当价差向上扩大且背离值处于 *Buy Threshold* 与 *Stay Out Threshold* 之间时，开多仓或平掉现有空单。
* 当价差向下扩大且背离值处于上述区间的相反范围内时，开空仓或平掉现有多单。

策略仅处理已完成的蜡烛，与原始 EA 的逐棒逻辑保持一致。所有交易管理均通过高级方法 `BuyMarket` / `SellMarket` 完成。

## 交易规则

1. 订阅配置的蜡烛类型，并按 *Fast SMA* 与 *Slow SMA* 周期计算两条 SMA。
2. 计算当前价差 (`fast - slow`)，与上一根柱的价差比较得到背离值。
3. 当背离为正，且介于 *Buy Threshold* 与 *Stay Out Threshold* 之间时开多。
4. 当背离为负，且介于 `-Buy Threshold` 与 `-Stay Out Threshold` 之间时开空。
5. 出现相反信号时立即反手。
6. 仅在本地时间 *Start Hour* 到 *Stop Hour* 的时间窗口内允许新开仓（支持跨越午夜）。

## 风险控制

* 可选的 *Take Profit (pips)* 与 *Stop Loss (pips)* 按蜡烛最高/最低价触发。
* *Break-Even Trigger (pips)* 在仓位盈利达到指定点数后，将止损移动到 `入场价 ± Break-Even Buffer`。
* *Trailing Stop (pips)* 在交易盈利后跟踪价格；设为 9999 即与原 EA 一样关闭该功能。
* 当浮动盈亏达到 *Basket Profit* 或低于 `-Basket Loss`（账户货币）时，篮子管理会平掉所有仓位。

## 参数说明

| 参数 | 说明 |
|------|------|
| `Order Volume` | 新开仓时使用的交易量。 |
| `Fast SMA` / `Slow SMA` | 两条简单移动平均线的周期。 |
| `Applied Price` | 用于计算均线的蜡烛价格字段。 |
| `Buy Threshold` | 允许做多的背离下限。 |
| `Stay Out Threshold` | 背离上限，超过该值不再开新仓。 |
| `Take Profit (pips)` / `Stop Loss (pips)` | 以点为单位的固定止盈/止损。 |
| `Trailing Stop (pips)` | 交易盈利后的跟踪距离。 |
| `Break-Even Trigger (pips)` | 触发移动到保本位的盈利点数。 |
| `Break-Even Buffer (pips)` | 移动到保本位时附加的缓冲。 |
| `Basket Profit` / `Basket Loss` | 以账户货币表示的浮动盈亏阈值。 |
| `Start Hour` / `Stop Hour` | 本地交易时间窗口。 |
| `Candle Type` | 订阅与计算所使用的蜡烛类型（时间框架）。 |

## 使用建议

* 将策略附加到目标证券，并选择与原图表一致的时间框架。
* 确认证券的 `PriceStep` / `StepPrice` 设置正确，以便点值计算准确。
* 若要停用追踪止损或保本移动等功能，请保持参数为原始哨兵值（9999）或 0。
