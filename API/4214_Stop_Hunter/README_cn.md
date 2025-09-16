# Stop Hunter 策略

## 概览
- 将 MetaTrader 4 的 **Stop Hunter** 专家顾问移植到 StockSharp 高阶策略框架中。
- 专注圆整价位突破：持续寻找右侧 `Zeroes` 个数字为零的价格，并在该圆整价附近布置止损挂单。
- 继续使用“隐藏”止盈/止损的做法，由策略内部监控退出条件，不在交易所端暴露保护单。
- 保留两阶段加减仓机制：首个目标位平掉一半仓位，剩余仓位等待翻倍目标或翻倍止损。

## 数据流与订阅
1. 在 `OnStarted` 中调用 `SubscribeLevel1().Bind(ProcessLevel1)` 订阅 Level1 数据，只需要最优买卖价即可。
2. 每次更新缓存最新的 Bid/Ask，并在 `IsFormedAndOnlineAndAllowTrading()` 通过后触发交易逻辑。
3. 若启用图表，可创建绘图区域并调用 `DrawOwnTrades` 展示策略成交。

## 挂单逻辑
- **圆整价识别**
  - 使用 `Security.PriceStep` 作为 MQL `Point` 的等价物。
  - 计算圆整价步长：`roundStep = PriceStep * 10^Zeroes`。
  - 根据 Bid 求取下一个圆整价：`Math.Ceiling(bid / roundStep) * roundStep`。
  - 若 Ask 已经进入缓冲区，向前移动圆整价，避免挂单距离买卖价过近。
  - 通过 `roundStep` 得到下方圆整价 `LevelS` 并进行同样的安全调整。
- **止损挂单**
  - 当允许做多且没有空头仓位时，在 `LevelB - DistancePoints * PriceStep` 处放置 **BuyStop**。
  - 当允许做空且没有多头仓位时，在 `LevelS + DistancePoints * PriceStep` 处放置 **SellStop**。
  - 一旦新的圆整价出现或价格偏离超过 `roundStep + DistancePoints * 50 * PriceStep`，便取消旧的挂单，与原 EA 的 `Delete*Stop()` 行为保持一致。
  - 通过 `CountActiveSlots()` 控制总槽位数量不超过 `MaxLongPositions + MaxShortPositions`。

## 虚拟止盈/止损
- 记录平均入场价 (`Position.AveragePrice`) 以及当前仓位量。
- 通过 `_takeProfitExtension` 与 `_stopLossExtension` 模拟 EA 中的 `TP2`/`SL2` 变量：
  - 第一阶段：价格朝有利方向移动 `TakeProfitPoints * PriceStep` 时，平掉一半仓位。
  - 平掉一半后，两组距离都会再增加 `TakeProfitPoints` / `StopLossPoints`，进入第二阶段跟踪。
  - 第二阶段：剩余仓位在达到翻倍目标或翻倍止损时整体平仓。
- 退出使用 `SellMarket` 或 `BuyMarket` 下市价单，与原始策略保持一致。
- 开仓后立即撤掉相反方向的挂单，避免形成对冲头寸。

## 资金管理
- 复刻 EA 中的 `Call_MM()`：`volume = 账户价值 / 100000 * RiskPercent`。
- 计算结果被限制在 `MinimumVolume` 与 `MaximumVolume` 之间，并按照合约的交易量步长取整（若步长未知，则根据 `MinimumVolume` 决定保留的小数位数）。
- 部分平仓按当前仓位的一半计算，再次取整以满足交易所要求。

## 实现细节
- 完全依赖 StockSharp 高层方法：`BuyStop`、`SellStop`、`BuyMarket`、`SellMarket` 以及 Level1 绑定，不使用底层 Connector 调用。
- 通过 `ResetState()` 在启动或复位时清理内部状态，确保不会遗留旧的引用。
- `OnOwnTradeReceived` 只有在收到成交确认后才更新 `_secondTrade` 标记，与原 EA 检查 `OrderClose` 成功的逻辑一致。
- `OnOrderChanged` 负责清除被撤单或失败的挂单引用，防止重复使用过期句柄。

## 与 MQL 版本的差异
- StockSharp 采用净持仓模式，无法像 MT4 对冲账户那样同时持有多笔相反订单。默认的 `MaxLongPositions = MaxShortPositions = 1` 保留了 EA 的典型行为。
- 风险计算使用 `Portfolio.CurrentValue`（回退到 `BeginValue`）代替 `AccountFreeMargin`，适合多品种环境。
- 在新仓位建立时重置扩展距离，避免原始代码中可能出现的 `TP2`/`SL2` 残留问题。
- 代码注释统一使用英文，文档按照项目要求提供英文、俄文和中文版本。

## 参数说明
| 参数 | 默认值 | 含义 |
| --- | --- | --- |
| `Zeroes` | 2 | 圆整价末尾必须为零的位数。 |
| `DistancePoints` | 15 | 挂单距离圆整价的点数偏移。 |
| `TakeProfitPoints` | 15 | 隐藏止盈的点数距离（第二阶段也沿用该值）。 |
| `StopLossPoints` | 15 | 隐藏止损的点数距离（第二阶段翻倍）。 |
| `EnableLongOrders` | true | 是否允许放置 BuyStop。 |
| `EnableShortOrders` | true | 是否允许放置 SellStop。 |
| `RiskPercent` | 5 | 参与建仓的资金百分比。 |
| `MinimumVolume` | 0.1 | 计算后允许的最小下单量。 |
| `MaximumVolume` | 30 | 计算后允许的最大下单量。 |
| `MaxLongPositions` | 1 | 多头槽位上限（含挂单 + 仓位）。 |
| `MaxShortPositions` | 1 | 空头槽位上限。 |

## 使用建议
1. 选择与 MQL `Point` 定义兼容的品种，例如大多数外汇货币对的 `Zeroes = 2`。
2. 根据交易所或券商的最小成交量调整 `MinimumVolume`，避免出现“无效手数”错误。
3. 由于止损/止盈在本地执行，请保持策略在线运行，必要时可结合 `StartProtection()` 设置交易所端保护单。
4. 参考俄文与英文文档，为不同团队提供本地化说明。
