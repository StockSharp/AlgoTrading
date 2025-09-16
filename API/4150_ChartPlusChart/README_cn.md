# Chart Plus Chart 策略

## 概述
Chart Plus Chart 是从 MetaTrader 脚本 *Chart1.mq4* 和 *Chart2.mq4* 转换而来的工具型策略。原始代码依赖名为
`SharedVarsDLLv2` 的 DLL，在每个行情到达时把四个核心指标写入共享内存：最新收盘价、当前未完成订单数量、账户余额以及
首个持仓的浮动盈亏。其他脚本可以读取这些共享槽位，在自定义面板上汇总显示这些信息。

StockSharp 版本完全保留了这种数据共享思路，但不再依赖外部二进制文件。策略公开了一个静态的
`ChartPlusChartSharedStorage` 辅助类，用线程安全的方式模拟 DLL 接口。`ChartPlusChartStrategy` 本身并不会发出
交易指令，而是监听指定的证券和组合，把最新的统计数据写入共享存储，供其他模块读取。

## 发布的指标
1. 根据参数 `CandleType` 订阅蜡烛序列，只在蜡烛收盘（`Finished`）后使用收盘价，避免未完成数据产生噪声。
2. 统计策略当前挂着的活跃订单数，已成交、撤单或失败的订单会被忽略，对应 MetaTrader 中的 `OrdersTotal()` 逻辑。
3. 读取最近的账户权益值，优先使用 `Portfolio.CurrentValue`，如果不可用则退回到 `Portfolio.BeginValue`。
4. 如果策略存在净持仓，根据最新价格与平均成交价计算浮动盈亏。
5. 将四个指标写入共享存储：
   - `SetFloat(baseIndex, closePrice)`
   - `SetInt(baseIndex, activeOrders)`
   - `SetFloat(baseIndex + 1, accountValue)`
   - `SetFloat(baseIndex + 2, floatingPnL)`
6. 每当策略出现自己的成交（`OnNewMyTrade`）时也会立即刷新共享槽位，确保盘中成交立刻被反映出来。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1 分钟周期 | 发布数据前要处理的主时间框架。 |
| `ChannelIndex` | `int` | `10` | 在 `ChartPlusChartSharedStorage` 中写入数据的基准索引。 |

## 与 MetaTrader 原版的差异
- 不再需要外部 DLL，所有共享内存的行为都由进程内的 `ChartPlusChartSharedStorage` 字典完成。
- MetaTrader 会在每个 tick 上刷新数据；StockSharp 版本在蜡烛收盘及自身成交时发布数据，既避免了中途价格波动造成的噪声，又能及时反映成交。
- 原脚本通过 `OrderSelect` + `OrderProfit()` 读取第一个订单的盈亏。StockSharp 版本根据净头寸计算浮动盈亏，更符合平台的净额模式。
- 策略停止时会清理对应的共享槽位，提醒其他组件该数据已经不再维护。

## 使用建议
- 在需要监控的账户上运行 `ChartPlusChartStrategy`，并让发布端和消费端配置相同的 `ChannelIndex`，确保读写的是同一组槽位。
- 其他可视化面板或配套策略可以通过 `ChartPlusChartSharedStorage.TryGetFloat` / `TryGetInt` 读取最新的共享数据。
- 策略不会主动下单，因此可以安全地与主交易系统并行运行，把它当作轻量级的账户遥测发布器。
