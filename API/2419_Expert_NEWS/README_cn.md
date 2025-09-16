# Expert NEWS 策略

## 概述
Expert NEWS 策略是 MQL5 机器人 “Expert_NEWS” 的直接移植版本。策略会在当前市场价上下同时挂出对称的止损单，并在成交后通过保本、移动止损以及定期刷新挂单等机制管理仓位。实现完全依赖 Level1 行情，默认交易手数为 0.1。

## 交易逻辑
1. **行情订阅**：持续接收最优买卖价，并用最新报价计算挂单价格。
2. **初始止损单**：当不存在多头仓位或买入止损单时，在 `卖价 + EntryOffsetTicks * PriceStep` 挂出买入止损；当不存在空头仓位或卖出止损单时，在 `买价 - EntryOffsetTicks * PriceStep` 挂出卖出止损。
3. **挂单刷新**：每隔 `OrderRefreshSeconds` 秒，如果理想价格偏离当前挂单价格超过 `TrailingStepTicks` 个跳动，则撤单并重新挂出新的止损单。
4. **仓位保护**：成交后，如果 `StopLossTicks` 与 `TakeProfitTicks` 满足 `MinimumStopTicks` 的约束，则分别挂出保护性止损单与止盈单。
5. **保本控制**：启用 `UseBreakEven` 时，一旦盈利足够且新的止损价仍满足最小距离，就把止损移动到 `入场价 ± BreakEvenProfitTicks`。
6. **移动止损**：当浮盈达到 `TrailingStartTicks` 时，止损会按照 `TrailingStopTicks` 的距离跟随价格，且每次至少提升 `TrailingStepTicks` 个跳动。
7. **清理机制**：仓位归零时会撤销所有剩余的保护性委托单。

## 参数
| 参数 | 说明 |
|------|------|
| `StopLossTicks` | 初始保护性止损距离（跳动数）。设为 0 可关闭初始止损单。 |
| `TakeProfitTicks` | 初始止盈距离（跳动数）。设为 0 可关闭止盈单。 |
| `TrailingStopTicks` | 移动止损距离（跳动数）。 |
| `TrailingStartTicks` | 启动移动止损所需的最小浮盈。 |
| `TrailingStepTicks` | 更新移动止损或刷新挂单时的最小改进幅度。 |
| `UseBreakEven` | 启用后，在达到盈利目标时触发保本移动。 |
| `BreakEvenProfitTicks` | 移动至保本位时保留的额外利润缓冲。 |
| `EntryOffsetTicks` | 新挂止损单相对于当前报价的距离。 |
| `OrderRefreshSeconds` | 自动刷新挂单的时间间隔（秒）。 |
| `MinimumStopTicks` | 交易所要求的最小止损距离（跳动数）。小于该距离的止损不会发送。 |

## 仓位管理
- 保护性委托始终与净仓位手数相匹配，部分成交会自动调整止损与止盈订单数量。
- 即使初始止损被禁用，保本与移动止损逻辑仍会在条件满足时创建新的止损单。
- 策略会记录最近一次的止损价格，确保移动止损始终单向收紧。

## 使用说明
- 请确保 `Security.PriceStep` 已正确配置，所有以跳动表示的参数都会乘以该值。
- 默认交易量设置为 `0.1`，用于对齐原始机器人；如需其他手数，可直接修改策略的 `Volume` 属性。
- 如果交易场所限制最小止损距离，应在 `MinimumStopTicks` 中填入对应值；若无要求，可保持为 0。
- 策略不依赖历史 K 线，仅使用实时行情即可运行。
