# Virtual Profit Close 策略

## 概览

Virtual Profit Close 策略复刻了 MetaTrader 4 指标 `Virtual_Profit_Close.mq4` 的行为。它持续观察所选证券的当前持仓，
当未平仓单达到设定的虚拟盈利目标时立刻市价平仓。与传统止盈不同，目标价位在策略内部计算，不会在市场上留下挂单。
策略还提供可选的移动止损，用于在浮盈扩大时主动跟随。测试模式下可以自动开仓，用于演示策略逻辑。

## 转换说明

- 通过 `SubscribeTrades().Bind(ProcessTrade).Start()` 订阅逐笔行情，等价于原脚本中的 `OnTick` 回调。
- 根据 `Security.PriceStep` 与报价小数位数推导 MetaTrader 的点值（pip），确保点差和盈利计算一致。
- 多头使用买价（Bid），空头使用卖价（Ask）来计算浮盈，与 MQL4 中对 `Bid`/`Ask` 的引用保持一致。
- 移动止损在达到触发阈值后按照设定偏移跟随当前价格，相当于 MQL 中反复调用 `OrderModify` 更新止损。
- 演示模式替代原脚本的 `SendTest` 函数，根据方向、手数和止损配置自动开仓，并通过 `SetStopLoss` 设置防护性止损。

## 参数

| 参数 | 说明 |
|------|------|
| `ProfitPips` | 以 MetaTrader 点（pip）表示的虚拟止盈距离，达到后立即平仓。 |
| `UseTrailingStop` | 是否启用移动止损。 |
| `TrailingOffsetPips` | 移动止损与当前价格之间保持的距离（点）。 |
| `TrailingActivationPips` | 启动移动止损所需的最小盈利（点）。 |
| `EnableDemoMode` | 为测试自动开仓，每次仓位归零后重新进入市场。 |
| `DemoOrderDirection` | 演示订单的方向（买入或卖出）。 |
| `DemoOrderVolume` | 演示模式下提交的手数。 |
| `DemoStopPips` | 演示订单的可选保护性止损距离（点）。 |

## 行为流程

1. 启动时计算点值及对应的盈利、移动止损、演示止损距离。
2. 每个成交 tick 触发 `ProcessTrade` 评估当前仓位：
   - 多头在买价达到目标盈利时平仓。
   - 空头在卖价满足距离要求时平仓。
3. 若启用移动止损并达到触发阈值，止损价会随着有利方向移动，一旦价格回撤穿越止损立即市价退出。
4. 演示模式在仓位清零时自动重新开仓，用于复现原脚本在测试器中的演示功能。

## 使用要求

- 需要逐笔行情数据才能及时响应价格变化。
- 策略仅针对单一品种设计，与原 EA 只监控当前图表品种的假设保持一致。
