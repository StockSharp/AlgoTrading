# Check Execution 策略

## 概述
Check Execution 策略复现了原始 MQL 专家顾问的逻辑，通过反复修改经纪商订单来衡量执行质量。算法可以测试两种场景：在挂单模式下提交 Buy Stop；在市价模式下先买入，再用 Sell Stop 保护多头头寸。每一次修改都会记录当前点差以及交易所确认修改所需的时间，从而帮助评估经纪商的延迟表现。

## 核心流程
1. 使用高阶 `SubscribeLevel1` API 订阅最优买卖报价。
2. 根据选择的模式放置初始测试订单：
   - **Pending**：在当前卖价上方挂出 Buy Stop。
   - **Market**：市价买入，然后在卖价下方挂出保护性的 Sell Stop。
3. 每次收到报价更新时：
   - 利用 `SimpleMovingAverage` 指标更新买卖价差的滚动平均值。
   - 如果需要移动价格且没有等待中的请求，则按新的偏移量重新注册订单。
   - 当订单重新进入 `Active` 状态时，计算执行延迟并送入第二个 `SimpleMovingAverage`，得到毫秒级平均延迟。
4. 重复上述循环直到达到预设的迭代次数。之后策略会取消所有剩余的挂单或止损单，在需要时平掉测试仓位，并在日志中输出点差和延迟统计。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `Volume` | 每次操作使用的交易量。 | `0.01` |
| `Iterations` | 参与平均值计算的修改次数，限制为 1–500。 | `30` |
| `Order Mode` | 工作模式：`Pending` 或 `Market`。 | `Pending` |
| `Pending Offset` | Buy Stop 相对于卖价的价格步长偏移。 | `100` |
| `Stop Offset` | Sell Stop 相对于卖价的价格步长偏移。 | `100` |

## 行为说明
- 交易量会根据合约的 `VolumeStep`、`MinVolume` 和 `MaxVolume` 自动归一化，避免因超出限制而被拒单。
- 价格偏移通过合约的 `PriceStep` 转换为真实价格；当缺少该信息时，默认使用 `0.0001`。
- 只有当交易所把订单状态更新为 `Active` 或 `Done` 时，才会将此次修改计入统计并刷新延迟平均值。
- 达到目标迭代次数后，策略会停止继续修改订单，撤销保护性挂单，平掉测试持仓，并在日志中总结测量结果。

## 与 MQL 版本的差异
- 使用 StockSharp 的 `SimpleMovingAverage` 指标计算点差和延迟平均值，而不是手动维护数组。
- 订单管理依赖 `BuyMarket`、`BuyStop`、`SellStop` 和 `ReRegisterOrder` 等高阶方法，以符合 StockSharp 策略框架。
- 结果通过策略日志输出，而非在图表上绘制文本或图形对象。
