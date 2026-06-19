# Backbone 篮子策略（StockSharp）

## 概述
**Backbone 篮子策略** 将 MetaTrader 4 的 "Backbone.mq4" 专家顾问迁移到 StockSharp 高层 API。系统先记录买卖价的极值以推断初始方向，然后在后续的每根已完成K线中逐步累积同向仓位。每根K线最多追加一笔市价单，直到达到 `MaxTrades` 限制，或是由止损/止盈订单结束整组仓位。仓位规模通过分数风险模型控制——以账户价值与止损距离计算可承受风险。

## 市场数据流程
- **K线（`CandleType`）**：只有在收到完整的K线后才会评估信号，与原始 EA 仅在 `Bars > PrevBars` 时触发完全一致。
- **盘口快照**：持续跟踪最优买价与卖价，用于复制最初的极值判断以及移动止损的计算。
- **策略内部状态**：StockSharp `Strategy` 基类维护实时仓位、均价和盈亏，这些信息直接驱动保护性订单的更新。

## 交易逻辑
1. **初始校准**：在尚未确定方向时，策略会记录出现过的最高买价与最低卖价。一旦价格距离极值回撤达到 `TrailingStopPoints * PriceStep`，便选定第一组仓位的方向。
2. **下单节奏**：
   - 若上一笔成交为做空（`_lastPositionDirection == -1`）且当前没有持仓，则提交新的**买入市价单**。
   - 若上一笔成交为做多（`_lastPositionDirection == 1`）且尚未达到 `MaxTrades` 上限，则在后续 K 线上继续追加买单。
   - 做空规则完全对称：当上一笔成交为做多且尚未达到上限时追加卖单。
3. **仓位规模**：每次下单都会调用 MQL 中 `Vol()` 的 C# 版本。策略取账户价值（按 CurrentValue → CurrentBalance → BeginBalance 的优先级）乘以 `MaxRisk`，再除以折算成货币单位的止损距离（利用 `PriceStepCost`）。结果对齐至 `VolumeStep`，并受 `MinVolume` 与 `MaxVolume` 约束，低于最小交易量时直接放弃下单。
4. **保护性订单**：成交后立即创建一个覆盖整组仓位的止损与止盈订单，距离以“点”（价格步长）表示，保持与原始 EA 一致。
5. **移动止损**：当 `StopLossPoints` 与 `TrailingStopPoints` 均大于零时，若价格超出入场价超过拖尾距离，则重新注册止损订单以锁定利润。多头使用最优买价作为参考，空头使用最优卖价。
6. **仓位闭合**：一旦止损或止盈触发，内部计数器清零，但 `_lastPositionDirection` 保留，使得下一根K线会在相反方向开启新一组仓位，从而复现原策略的交替行为。

## 资金管理
- 使用与 MQL 相同的分数公式 `1 / (MaxTrades / MaxRisk - openTrades)` 计算风险占比。
- 风险基数依次取 `Portfolio.CurrentValue`、`CurrentBalance`、`BeginBalance`。
- 调整后的仓位若低于 `MinVolume` 会被丢弃，确保遵守交易所最小手数。
- 每次仓位变化都会重新生成止损/止盈订单，使保护范围始终覆盖全部仓位。

## 参数
| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `CandleType` | 15 分钟 | 触发策略评估的 K 线周期。 |
| `MaxRisk` | 0.5 | 单笔交易可使用的账户资金比例，必须为正。 |
| `MaxTrades` | 10 | 同方向最多允许累积的市价单数量。 |
| `TakeProfitPoints` | 170 | 止盈距离（以价格步长表示），设为 0 可关闭止盈。 |
| `StopLossPoints` | 40 | 止损距离（以价格步长表示），也是风险计算和拖尾的基础。 |
| `TrailingStopPoints` | 300 | 移动止损距离（以价格步长表示），设为 0 表示保持静态止损。 |

## 移植说明
- 原 EA 为每笔订单分别调整止损/止盈；StockSharp 版本在净持仓模式下改为为整组仓位维护一个聚合的保护订单。
- 仓位规模依赖 `Security.PriceStepCost`。若连接器未提供该值，则退回到策略的 `Volume` 属性。
- 移动止损仅在新K线完成时更新，保持与 MT4 中“每根K线执行一次”的行为一致。
- `_lastPositionDirection` 保存上一笔成交方向，使得平仓后下一根K线自动在相反方向重新建仓。
- 本目录仅包含 C# 实现，不提供 Python 版本。

## 使用建议
- 选择具有正确 `PriceStep`、`PriceStepCost` 与成交量步长信息的品种，以确保仓位规模合理。
- 回测时需确保有盘口数据源，移动止损需要最优买卖价才能精确工作。
- 若想降低加仓速度，可提高 `MaxTrades` 或降低 `MaxRisk`，从而减小 `Vol()` 计算出的下单量。
