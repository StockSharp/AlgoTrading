# Vortex Indicator MMRec Duplex 策略

## 概述
- 基于 MetaTrader 5 专家顾问 **Exp_VortexIndicator_MMRec_Duplex.mq5**（MQL ID 23180）移植到 StockSharp 高级 API。
- 维护两个互不干扰的 Vortex 指标流：一个负责多头交易，另一个负责空头交易。每个流都可以配置独立的时间周期、指标周期以及信号偏移，从而精细化多空逻辑。
- 完整复现原版 EA 的 MMRec 资金管理恢复机制。策略分别记录多头与空头的最新盈亏结果，当出现指定数量的连续亏损时，临时将下一笔订单的交易量降至保守水平。

## 信号逻辑
1. 对两个订阅分别获取指定类型的蜡烛，并计算 Vortex 指标（`VI+` 与 `VI-`）。
2. **开多：** 如果前一根被评估的蜡烛上 `VI+ ≤ VI-`，而当前关闭的蜡烛上 `VI+ > VI-`，且 `AllowLongEntries` 为真，则认为出现多头交叉信号。
3. **平多：** 当评估蜡烛上 `VI- > VI+` 且 `AllowLongExits` 允许时，立即平掉多头头寸。
4. **开空：** 如果上一根蜡烛上 `VI+ ≥ VI-`，而当前关闭的蜡烛上 `VI+ < VI-`，且 `AllowShortEntries` 为真，则视为出现空头交叉信号。
5. **平空：** 当 `VI+ > VI-` 且 `AllowShortExits` 允许时，关闭空头头寸。
6. 多头与空头各自维护独立的止损和止盈，单位为价格步长。只要触发任意一条保护线，头寸立即被平仓并记入恢复计数器。

## 资金管理恢复
- 原策略会统计最近若干笔交易的盈亏情况来决定下一单使用正常仓位还是缩减仓位。本移植完全遵循该规则。
- 多头队列最多保留 `LongTotalTrigger` 条记录；若其中有不少于 `LongLossTrigger` 条为亏损，下一次多头下单将使用 `LongSmallMoneyManagement`，否则使用 `LongMoneyManagement`。
- 空头对应使用参数 `ShortTotalTrigger`、`ShortLossTrigger`、`ShortSmallMoneyManagement` 与 `ShortMoneyManagement`。
- 当触发参数为零时，相关队列被清空，策略始终使用基础仓位。

## 资金模式
枚举 `MarginModeOption` 决定如何将资金管理参数转换为下单量：
- **FreeMargin (0)：** 将数值视为资本的比例（对应原 EA 的“自由保证金”模式）。
- **Balance (1)：** 与 `FreeMargin` 在本移植中等价，使用当前账户资产。
- **LossFreeMargin (2)：** 根据设定的止损距离按风险比例计算仓位，若止损为零则退化为按价格计算。
- **LossBalance (3)：** 与 `LossFreeMargin` 在本策略中的实现一致。
- **Lot (4)：** 直接把数值视为下单手数。

所有最终得到的仓位都会依据交易品种的 `VolumeStep`、`MinVolume` 与 `MaxVolume` 进行归一化，确保发送合法订单。

## 参数列表
| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `LongCandleType` | H4 | 多头 Vortex 指标使用的蜡烛类型。 |
| `ShortCandleType` | H4 | 空头 Vortex 指标使用的蜡烛类型。 |
| `LongLength` | 14 | 多头 Vortex 指标周期。 |
| `ShortLength` | 14 | 空头 Vortex 指标周期。 |
| `LongSignalBar` | 1 | 参与多头计算的已收盘蜡烛偏移量（0 表示最新已收盘蜡烛）。 |
| `ShortSignalBar` | 1 | 参与空头计算的蜡烛偏移量。 |
| `AllowLongEntries` | true | 允许在出现多头交叉时开多。 |
| `AllowLongExits` | true | 允许在 `VI-` 超过 `VI+` 时平多。 |
| `AllowShortEntries` | true | 允许在出现空头交叉时开空。 |
| `AllowShortExits` | true | 允许在 `VI+` 再次高于 `VI-` 时平空。 |
| `LongTotalTrigger` | 5 | 多头 MMRec 队列最大长度。 |
| `LongLossTrigger` | 3 | 触发多头缩量所需的亏损次数。 |
| `LongMoneyManagement` | 0.1 | 多头基础资金管理值。 |
| `LongSmallMoneyManagement` | 0.01 | 多头在连亏后的降级资金管理值。 |
| `LongMarginMode` | Lot | 多头资金管理模式（见上文）。 |
| `LongStopLossSteps` | 1000 | 多头止损距离，单位为价格步。 |
| `LongTakeProfitSteps` | 2000 | 多头止盈距离，单位为价格步。 |
| `LongSlippageSteps` | 10 | 多头预期滑点（信息用途）。 |
| `ShortTotalTrigger` | 5 | 空头 MMRec 队列最大长度。 |
| `ShortLossTrigger` | 3 | 触发空头缩量所需的亏损次数。 |
| `ShortMoneyManagement` | 0.1 | 空头基础资金管理值。 |
| `ShortSmallMoneyManagement` | 0.01 | 空头在连亏后的降级资金管理值。 |
| `ShortMarginMode` | Lot | 空头资金管理模式。 |
| `ShortStopLossSteps` | 1000 | 空头止损距离（价格步）。 |
| `ShortTakeProfitSteps` | 2000 | 空头止盈距离（价格步）。 |
| `ShortSlippageSteps` | 10 | 空头预期滑点（信息用途）。 |

## 实现说明
- 使用 StockSharp 高级 API，通过 `SubscribeCandles().Bind(...)` 机制驱动指标，仅处理已完成的蜡烛。
- 交易恢复逻辑以队列形式保存多头与空头的盈亏序列，完全对应原始函数 `BuyTradeMMRecounterS` 与 `SellTradeMMRecounterS`。
- 止损和止盈会根据品种的价格步长转换为绝对价格，并在每根蜡烛上检查。
- 下单量统一经过 `VolumeStep`、最小/最大交易量约束的归一化，避免发送不可执行的委托。
- 滑点参数保留下来用于文档说明，实际下单时由撮合系统处理，并不参与量化计算。
