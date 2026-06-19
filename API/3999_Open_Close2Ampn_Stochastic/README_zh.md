# Open Close2 Ampn 随机指标策略

## 概述
- 该策略为 MetaTrader 4 专家顾问 *open_close2ampnstochastic_strategy* 的移植版本，基于 StockSharp 的高层 API 实现。
- 采用经典的随机振荡指标（周期 9，%K/%D 均为 3）并结合两根 K 线的形态过滤：只有当最新蜡烛延续前一根的方向时才允许发单。
- 默认订阅 1 小时 K 线，可通过 `CandleType` 参数替换为任何可用的时间框架。

## 交易逻辑
1. **入场条件**：策略一次仅持有一个方向的仓位，空仓时评估上一根完整蜡烛。
   - **做多**：随机指标主线高于信号线，且当前蜡烛的开盘价和收盘价都低于上一根蜡烛（价格继续走低，但振荡指标先行反弹）。
   - **做空**：随机指标主线低于信号线，且当前蜡烛的开盘价和收盘价都高于上一根蜡烛（价格继续上行，但振荡指标走弱）。
2. **离场条件**：持仓时使用镜像逻辑：
   - **平多**：主线跌破信号线，同时新蜡烛的开收盘价均高于上一根。
   - **平空**：主线突破信号线，同时新蜡烛的开收盘价均低于上一根。
3. **回撤保护**：复制原脚本的“应急止损”。当浮动亏损的绝对值（已实现 PnL 与按最新蜡烛估算的未实现 PnL 之和）达到 `MaximumRisk × 账户保证金` 时立即平仓。StockSharp 不提供 MT4 的 `AccountMargin` 字段，因此本移植优先使用 `Portfolio.BlockedValue`，若不可用则退回 `Portfolio.CurrentValue`。

## 资金管理
- **BaseVolume** 对应 MQL 中的 `Lots`，在无法获取账户估值时作为固定下单量。
- 若能读取投资组合估值，则按照 `Portfolio.CurrentValue × MaximumRisk / 1000` 计算基础手数，与原始 `AccountFreeMargin` 逻辑保持一致。
- 每出现一笔亏损，下一次下单的数量减少 `亏损次数 / DecreaseFactor`，获利后重置计数。最终数量不低于 `MinimumVolume`（默认 0.1 手）。
- 所有下单量会在发送前对齐交易品种的 `VolumeStep`、`MinVolume`、`MaxVolume` 限制。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `BaseVolume` | decimal | `0.1` | 在无法计算风险头寸时使用的固定下单量。 |
| `MaximumRisk` | decimal | `0.3` | 用于风险头寸和回撤保护的权益比例，设为 `0` 可关闭风险计算。 |
| `DecreaseFactor` | decimal | `100` | 连续亏损时缩减仓位的除数，数值越大缩减越慢。 |
| `MinimumVolume` | decimal | `0.1` | 允许的最小下单量。 |
| `StochasticLength` | int | `9` | 随机指标的计算周期。 |
| `StochasticKLength` | int | `3` | %K 的平滑周期。 |
| `StochasticDLength` | int | `3` | %D 信号线的平滑周期。 |
| `CandleType` | `DataType` | `TimeFrame(1h)` | 用于计算指标与信号的蜡烛数据类型。 |

## 实现说明
- 回撤保护所需的浮动盈亏通过 `Strategy.PositionPrice` 与最新收盘价估算，其目的与 MT4 中的 `AccountProfit` 相同，但实际结果可能因经纪商而异。
- 若连接器既不给出 `BlockedValue` 也不给出 `CurrentValue`，应急平仓功能会保持闲置，但策略仍按照 `BaseVolume` 进行交易。
- `StartProtection()` 在启动时被调用，以启用 StockSharp 的内置保护（例如止损路由、重连守护），对应原脚本中的安全措施。

## 与原版的差异
- 手数的舍入依据交易品种的最小/最大手和步长信息，请确认 `VolumeStep`、`MinVolume` 数据已正确设置，以免与 MT4 结果不一致。
- 原 EA 在每个 tick 上检查 `Volume[0]` 以避免重复触发；移植版本仅处理已完成的蜡烛，更符合 StockSharp 的推荐写法，并能消除重复信号。
- 账户保证金和浮动盈亏均为近似值，如果需要完全复刻经纪商的计算方式，请根据实际情况调整 `MaximumRisk` 或扩展风控模块。
