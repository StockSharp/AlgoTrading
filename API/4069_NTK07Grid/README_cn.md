# NTK_07 网格策略

## 概述

NTK_07 是一个对称挂单网格策略，最初在 MetaTrader 4 上实现。策略会在行情两侧分别放置买入止损和卖出止损单，通过可配置的间距、止损、止盈与跟踪规则来管理金字塔式加仓。此 StockSharp 版本完整还原原始逻辑，并且将所有参数以强类型形式暴露出来，便于在测试和优化中使用。

策略持续保证以下行为：

* 当没有任何挂单或持仓时，始终维持一对买入止损和卖出止损挂单。
* 当其中一侧被触发后，会立即取消另一侧的挂单，避免出现双向对冲。
* 只要下一档仓位乘以 `Multiplier` 后不超过 `LotLimit`，就会按网格间距继续加仓。
* 当无法继续加仓时，使用跟踪止损以及可选的动态止盈来保护已有仓位。
* 每当仓位变化时，都会重新创建保护性止损/止盈订单，确保所有头寸共享统一的退出价格。

## 交易流程

1. **交易时段过滤。** 周六、周日以及 `StartHour`～`EndHour` 以外的时段不会下单。`EndHour = 24` 表示整日可交易。
2. **资金检查。** 如果绑定了投资组合，则其当前价值必须大于 `MinCapital` 才允许下单。
3. **通道过滤（可选）。** 当 `ChannelPeriod > 0` 时，跟踪最近若干根完成 K 线的最高价和最低价：
   * `UseChannelCenter = false`：只有在价格突破通道上下沿时才布置挂单。
   * `UseChannelCenter = true`：仅当价格回到通道中点附近时才布置挂单。
4. **初始挂单。** 在没有任何挂单的情况下，于当前最优卖价上方 `NetStepPips` 放置买入止损，于最优买价下方 `NetStepPips` 放置卖出止损，基准手数由资金管理模块决定。
5. **同向加仓。** 某侧挂单成交后，取消另一侧的挂单。若当前方向仍允许加仓，则以 `previousVolume × Multiplier` 的手数在 `NetStepPips` 处继续布置下一档挂单，直到超出 `LotLimit` 为止。
6. **止损与止盈。** 每当净持仓变化时，会针对多/空方向分别重新创建保护性止损与止盈，距离由 `StopLossPips` 和 `TakeProfitPips` 决定。
7. **保本逻辑。** 若 `UseBreakEven = true` 且价格相对最近成交价盈利达到 `BreakEvenOffsetPips`，止损会移动到按 `PriceRoundingFactor` 四舍五入后的加权平均开仓价。
8. **跟踪行为。** 当下一档加仓受限时，策略使用最近完成 K 线的最高/最低价按 `TrailingStopPips` 推进止损；如果 `TrailProfit = true`，止盈也会一起向外平移；若启用了 `UseMovingAverageFilter` 且价格反向穿越均线，则跟踪距离减半，模拟原策略在均线附近减缓追踪的行为。

## 资金管理

| 模式 | 说明 |
| ---- | ---- |
| `Fixed` | 始终使用 `InitialLot` 下单，并将单笔手数限制在 `LotLimit` 以内。 |
| `BalanceBased` | 按账户余额计算基准手数：`ceil(balance / 1000 × PercentRisk / 100)`，然后连续除以 `Multiplier` 推算最小网格手数，并用 `LotRoundingFactor` 进行四舍五入。原始 `LotLimit` 充当最大理论手数。 |
| `Progressive` | 保持 `InitialLot` 不变，但按网格层数连续乘以 `Multiplier` 估算最大可能手数。 |

所有订单手数均通过 `LotRoundingFactor` 进行四舍五入（默认 10 ⇒ 0.1 手单位），保本价格使用 `PriceRoundingFactor` 进行价格精度控制（默认 10000 ⇒ 0.0001）。

## 参数说明

| 参数 | 默认值 | 说明 |
| ---- | ------ | ---- |
| `NetStepPips` | 23 | 网格层之间的距离（以点数表示）。 |
| `StopLossPips` | 115 | 每个方向的保护性止损距离，0 表示不设置。 |
| `TakeProfitPips` | 300 | 聚合仓位的止盈距离，0 表示不设置。 |
| `TrailingStopPips` | 75 | 当无法继续加仓时启用的跟踪止损距离。 |
| `Multiplier` | 1.7 | 下一档仓位相对上一档的倍数。 |
| `TrailProfit` | `true` | 启用后，跟踪止损移动时止盈也同步平移。 |
| `ManagementMode` | `Progressive` | 资金管理模式。 |
| `InitialLot` | 1 | 初始挂单手数。 |
| `LotLimit` | 7 | 单笔挂单允许的最大手数。 |
| `MaxTrades` | 4 | 网格层数上限。 |
| `PercentRisk` | 10 | BalanceBased 模式下使用的风险百分比。 |
| `MinCapital` | 5000 | 启动策略所需的最低资金。 |
| `UseBreakEven` | `false` | 是否启用保本止损。 |
| `BreakEvenOffsetPips` | 5 | 达到保本所需的盈利点数。 |
| `UseMovingAverageFilter` | `false` | 是否在跟踪过程中启用均线过滤。 |
| `MovingAverageLength` | 100 | 均线长度。 |
| `MovingAverageShift` | 0 | 均线的偏移量（>0 时使用更早的均线值）。 |
| `StartHour` | 0 | 最早交易小时（0–23）。 |
| `EndHour` | 24 | 最晚交易小时（包含 24 表示全天）。 |
| `ChannelPeriod` | 0 | 通道过滤所使用的回溯 K 线数量，0 表示禁用。 |
| `UseChannelCenter` | `false` | `false` 为突破式下单，`true` 为通道中点挂单。 |
| `LotRoundingFactor` | 10 | 手数四舍五入的分母。 |
| `PriceRoundingFactor` | 10000 | 保本价格四舍五入的分母。 |
| `CandleType` | 15 分钟 | 用于通道与跟踪计算的 K 线类型。 |

## 实现细节

* 订阅盘口数据以获得最优买卖价，当盘口不可用时回退到 K 线收盘价。
* 使用重新注册订单而不是修改已有订单的方式更新止损/止盈，符合高层 API 的安全用法。
* 当 `MovingAverageShift` 超过可用历史时，会退回到最近的均线值，避免空引用并保持与原始 EA 相近的逻辑。
* 所有价格均通过 `Security.ShrinkPrice` 进行规范化，以确保符合合约最小跳动。

## 使用建议

1. 根据经纪商要求设置 `Strategy.Volume`，以便在需要时按账户规模放大交易量。
2. 如果交易品种的最小跳动或合约单位特殊，请相应调整 `LotRoundingFactor` 与 `PriceRoundingFactor`。
3. 默认参数来源于作者在 EURUSD H1（2008-01-01 至 2008-11-01）的测试，请针对不同市场重新优化。
4. 网格策略可能累积较大方向性头寸，请密切关注 `LotLimit` 与 `MaxTrades` 以控制风险。
