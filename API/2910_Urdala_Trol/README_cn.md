# Urdala Trol 对冲网格策略

## 概述
**Urdala Trol Hedging Grid Strategy** 是 MetaTrader 5 智能交易系统 `Urdala_Trol.mq5` 的 StockSharp 高阶 API 版本。策略始终保持双向持仓，并在触发止损后以网格+马丁方式逐步加仓。策略仅依赖 Level1 最优买卖价数据，不使用任何技术指标。

## 交易逻辑
1. **初始对冲（步骤 0）**：当没有持仓时，立即按 *Base Volume* 参数同时买入和卖出一手，建立基础对冲。
2. **亏损方向加仓（步骤 1.2）**：如果仅剩单方向持仓，并且该方向最亏损的仓位距离当前价格至少 `Grid Step` 个点，则按同方向再开一单。新单手数 = 最亏仓位手数 + `Min Lots Multiplier * minVolumeStep`，其中 `minVolumeStep` 来源于品种的 `VolumeStep` 或 `MinVolume`。
3. **止损后的处理（步骤 1.1）**：当仓位被止损（包括跟踪止损）并产生亏损时，如果当前没有距离止损价小于 `Min Nearest` 点的同向仓位，则立即按相同方向重新入场。
4. **盈利止损后的处理（步骤 2.1）**：止损盈利退出时，立刻以放大后的手数在相反方向开仓。
5. **跟踪止损**：价格在入场价上方（或下方）运行超过 `Trailing Stop + Trailing Step` 点后，将止损位上调（或下调），保持 `Trailing Stop` 点的距离。仅当两个参数都大于零时才启用跟踪。

所有以点（pip）表示的距离都会根据标的的 `PriceStep` 转换为绝对价格差。对于三位或五位报价，会额外乘以十，以复现原始 MQL “adjusted point” 的处理方式。

## 参数说明
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `BaseVolume` | 0.1 | 打开初始对冲仓位时使用的基础手数。 |
| `MinLotsMultiplier` | 3 | 加仓时附加的最小手数倍数。 |
| `StopLossPips` | 50 | 止损距离（点）。设为 0 可关闭止损和跟踪功能。 |
| `TrailingStopPips` | 5 | 跟踪止损距离（点），设为 0 则不启用跟踪。 |
| `TrailingStepPips` | 5 | 跟踪止损向前移动前需要额外行进的点数；启用跟踪时必须为正。 |
| `GridStepPips` | 50 | 亏损仓位与当前价格之间的最小距离（点），达到后才加仓。 |
| `MinNearestPips` | 3 | 如果当前已有仓位距离最近止损价小于该值，则跳过再次入场。 |

## 实现细节
- 通过 `SubscribeLevel1()` 订阅行情，使用最佳买卖价驱动全部决策。
- 使用 `RegisterOrder` 注册委托，并在 `OnOwnTradeReceived` 中精确跟踪成交与仓位。 
- 策略内部维护独立的仓位列表，以模拟对冲模式（StockSharp 默认按净头寸管理）。
- 止损和跟踪逻辑由策略自身触发市价单实现，没有单独登记止损委托。

## 使用建议
1. 选择流动性良好的标的，并确保 `PriceStep`、`VolumeStep`、`MinVolume`、`MaxVolume` 等属性设置正确，以便正确换算点值与手数。
2. 启动策略后会立即建立对冲仓位，随后按原版 MQL 逻辑响应止损事件。
3. 根据标的波动调整各项点值参数：增大 `Grid Step` 可降低加仓频率，提高 `Min Lots Multiplier` 会加速手数增长。
4. 谨慎控制风险：马丁结构在连续止损时会迅速放大头寸。

本目录按照要求暂不提供 Python 版本。
