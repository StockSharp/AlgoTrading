# 特定日期与时间下单策略

该策略复刻 MetaTrader 专家顾问 **“Expert Advisor specific day and time”** 的逻辑。  
在预设的时间点自动开仓（市价、止损挂单或限价挂单），并可在另一个时间点平仓及清理挂单。  
StockSharp 版本完全保留原始 MQL 机器人中的止盈、止损、移动止损与保本机制。

## 核心逻辑

1. **时间调度**  
   - `OpenTime`：进入市场的时间窗口。  
   - `CloseTime`：退出市场的时间窗口。  
   检查窗口长度为 1 分钟，对应 MT4 中 `TimeToString(..., TIME_MINUTES)` 的比较方法。

2. **下单方式**  
   - `OrderPlacement` 在市价、止损挂单、限价挂单之间切换。  
   - `OpenBuyOrders` / `OpenSellOrders` 分别控制买入或卖出方向。  
   - `OrderDistancePoints` 以点数（pip）定义挂单价格偏移。  
   - `PendingExpireMinutes` 到期后会自动撤销未触发的挂单。

3. **手数管理**  
   - `LotSizing = Manual` 使用固定的 `ManualVolume`。  
   - `LotSizing = Automatic` 根据当前账户权益与合约规模计算手数：  
     `volume = (portfolio / contractSize) * RiskFactor`。  
   结果会对齐到 `Security.VolumeStep`，同时遵守 `MinVolume` / `MaxVolume` 限制。

4. **风控逻辑**  
   - `StopLossPoints`、`TakeProfitPoints` 会使用品种的点值转换为绝对价格。  
   - `TrailingStopEnabled`、`TrailingStepPoints` 与 `BreakEvenEnabled` 共同实现原始 MQL 中的移动止损与保本逻辑，使用买卖价更新作为触发条件。  
   - 当价格触发止损或止盈时，通过市价单立刻平仓，等价于 MT4 中修改止损价位的做法。

5. **收盘阶段**  
   - 如果启用 `CloseOwnOrders` 或 `CloseAllOrders`，策略会在收盘窗口内强制平掉所有仓位。  
   - `DeletePendingOrders` 同时撤销所有剩余挂单。

## 参数

| 名称 | 说明 |
|------|------|
| `OpenTime`、`CloseTime` | 以 UTC 表示的开仓/平仓时间。 |
| `OrderPlacement` | 选择市价、止损挂单或限价挂单。 |
| `OpenBuyOrders`、`OpenSellOrders` | 控制开仓方向。 |
| `TakeProfitPoints`、`StopLossPoints` | 以点数表示的止盈止损（0 表示关闭）。 |
| `TrailingStopEnabled`、`TrailingStepPoints` | 是否启用移动止损以及需要的最小推进点数。 |
| `BreakEvenEnabled`、`BreakEvenAfterPoints` | 达到多少盈利点数后把止损推到保本位。 |
| `OrderDistancePoints` | 挂单价格偏移。 |
| `PendingExpireMinutes` | 挂单有效期（分钟）。 |
| `LotSizing` | 手数为手动或自动计算。 |
| `RiskFactor`、`ManualVolume` | 各模式所需的输入。 |
| `CloseOwnOrders`、`CloseAllOrders`、`DeletePendingOrders` | 控制收盘阶段如何平仓与撤单。 |

## 说明

- 代码位于 `StockSharp.Samples.Strategies` 命名空间，并严格使用制表符缩进以符合仓库规范。  
- 策略订阅 Level1 数据以获取买卖价，从而忠实还原挂单与移动止损的触发条件。  
- MT4 的 `MagicNumber` 在 StockSharp 中不再需要，平台已经按策略自动隔离订单。

## 使用方法

1. 通过 `AlgoTrading.sln` 编译项目，并将策略绑定到指定的品种与账户。  
2. 根据需求设置时间、下单方式以及风险参数。  
3. 在 `OpenTime` 之前启动策略，时间窗口开始时会自动下单。  
4. 如果需要自动平仓，请保持策略运行直到 `CloseTime` 之后。
