# Two Per Bar 策略
[English](README.md) | [Русский](README_ru.md)

## 概述
原始的 MetaTrader 专家顾问 “Two PerBar” 会在每根新 K 线开始时同时开多头和空头，在下一根 K 线开始时强制平仓整个组合，并可选择按照倍数放大仓位。StockSharp 版本延续了这一节奏：策略只在每根收盘 K 线时触发，显式跟踪两个对冲腿，并通过高层 API 下单，自动遵循品种的价格步长、数量步长以及最小/最大手数限制。

## 交易流程
1. **检测新 K 线**：通过 `SubscribeCandles` 订阅指定的蜡烛序列。收到 `State == CandleStates.Finished` 的蜡烛时表示上一根已收盘，新的周期开始。
2. **检查止盈触发**：每条腿保存自己的入场价与止盈价。如果收盘蜡烛的最高价或最低价触及止盈，则立即用市价单平掉该腿，并从跟踪列表移除。
3. **强制平仓剩余头寸**：未达到止盈的腿会在新周期开始前全部用市价单平掉，对应 MQL 版本在每根新棒开盘时调用 `PositionClose` 的逻辑。
4. **计算下一笔手数**：
   - 如果上一周期仍有腿存活，取其中最大的成交量乘以 `VolumeMultiplier`。
   - 如果两条腿都已出场（例如都触发止盈），则回到 `InitialVolume`。
   - `PrepareVolume` 会把候选手数四舍五入到小数点后两位，贴合品种的 `VolumeStep`，校验 `MinVolume`，若超过 `MaxVolume` 或 `Security.MaxVolume` 则重置为 `InitialVolume`。
5. **更新默认下单量**：计算结果写入 `_lastCycleVolume` 并同步到 `Strategy.Volume`，这样辅助下单方法会复用同样的手数。
6. **开出新的对冲组合**：调用 `BuyMarket(volume)` 开多腿，`SellMarket(volume)` 开空腿。每条腿记录刚收盘蜡烛的价格以及绝对止盈价（`entry ± TakeProfitPoints * pointSize`）。当 `TakeProfitPoints <= 0` 时禁用止盈，只依靠下一根棒的强制平仓退出。

最终形成一个循环“跨式”结构：每根 K 线开始时建立多空双腿，期间监控是否触及止盈，下一根 K 线开始前保证完全平仓。

## 资金与风控
- **类似马丁的加仓**：`VolumeMultiplier` 复制了原策略的倍数规则。只要有腿撑到强制平仓步骤，下一周期就会把最大腿的手数乘以该系数；若两个方向都通过止盈出场，则手数重置为 `InitialVolume`。
- **手数上限**：`MaxVolume` 是硬限制，一旦乘数结果超过它（或超过 `Security.MaxVolume`），手数立即恢复到初始值。
- **交易所合规**：所有手数都会贴合 `VolumeStep`，并在低于 `MinVolume` 时被拒绝。请确保 `InitialVolume` 自身就是可成交的手数。
- **价格步长**：止盈距离通过 `Security.PriceStep`（如未定义则使用 `MinPriceStep`）转换成绝对价格。若品种缺少价格步长，计算结果为 0，相当于停用止盈。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1 分钟 K 线 | 控制交易节奏的主时间框架。 |
| `InitialVolume` | `decimal` | `1` | 当上一周期没有遗留腿时使用的基础手数。 |
| `VolumeMultiplier` | `decimal` | `2` | 对上一周期最大腿手数的放大倍数。 |
| `MaxVolume` | `decimal` | `10` | 当放大手数超过该值时强制恢复到初始手数。 |
| `TakeProfitPoints` | `int` | `50` | 以价格“点”为单位的止盈距离。设为 `0` 时禁用止盈，仅依靠下一根棒的强制平仓。 |

## 实现细节与差异
- 策略在内部 `_legs` 列表中维护多头与空头两条腿，即使连接的是仅支持净头寸的撮合器，也能在逻辑层区分双向仓位。
- 止盈判断依赖已完成蜡烛的最高价/最低价范围，而不是逐笔行情，这与原策略的“按棒处理”一致并让行为更可预测。
- MetaTrader 中的滑点和魔术号参数未保留，订单路由完全交由 StockSharp 的组合管理器处理。
- 下单全部通过 `BuyMarket` / `SellMarket` 等高层方法完成，没有手动往 `Strategy.Indicators` 添加对象，符合仓库的编码规范。

## 使用建议
- 在启动前把 `InitialVolume` 调整到符合品种 `VolumeStep` 的数值，构造函数不会自动调整。
- 若品种的价格步长很小，可适当下调 `TakeProfitPoints`，避免止盈离入场价过远。
- 策略会同时持有多头与空头，请在支持对冲模式的连接或账户上运行。若交易所强制净额结算，`_legs` 仍会记录目标逻辑，但实际成交表现可能不同。
- 将策略添加到图表中可以直观查看蜡烛与成交轨迹（`OnStarted` 中已调用 `DrawCandles` 和 `DrawOwnTrades`）。
