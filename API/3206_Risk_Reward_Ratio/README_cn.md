# Risk Reward Ratio 策略

## 概述
**Risk Reward Ratio Strategy** 是 MetaTrader 智能交易系统 “Risk Reward Ratio” 的 StockSharp 版本。该策略使用多重动量和趋势过滤器，并辅以严格的风险管理模块。入场条件要求快速/慢速随机指标、两条线性加权均线（LWMA）交叉、14 周期 RSI 以及 MACD 趋势方向全部一致。风控部分提供点值止损、按照固定盈亏比的止盈、可选的追踪止损与保本机制，以及紧急平仓开关。

本移植使用 StockSharp 的高层 API：通过 `SubscribeCandles().BindEx(...)` 订阅蜡烛并绑定指标，只处理收盘蜡烛，不直接访问指标缓存，以符合事件驱动模式。

## 交易逻辑
1. **随机指标共振**
   * 快速随机指标 (5, 2, 2) 的 %K 提供动量信号。
   * 慢速随机指标 (21, 10, 4) 的 %D 用于判断大方向。
   * 多头要求快速 %K 高于慢速 %D，空头则相反。
2. **RSI 过滤**
   * RSI(14) 必须高于 50 方可做多，低于 50 方可做空，确保顺势。
3. **LWMA 趋势过滤**
   * 两条 LWMA（长度 6 与 85）需同向：做多时快线在慢线之上，做空时反之。
4. **MACD 趋势确认**
   * MACD(12, 26, 9) 主线必须领先信号线，并且位于正确的象限（多头在零线上方，空头在零线下方）。
5. **动量偏离阈值**
   * Momentum(14) 衡量价格对 100 的偏离。最近三次读数中至少一次需要超过 `MomentumThreshold`。
6. **仓位上限**
   * 净仓位限制在 `MaxPositions * TradeVolume`，与原版 EA 保持一致。

策略使用 `BuyMarket` / `SellMarket` 下达市价单，忽略未完成蜡烛，所有状态保存在策略对象内部。

## 风险管理
* **点值止损** —— 每次入场都会根据 `StopLossPips * PriceStep` 设置保护止损。
* **固定盈亏比止盈** —— 止盈距离等于止损距离乘以 `RewardRatio`。
* **追踪止损** —— 当启用且价格朝有利方向运行超过 `TrailingStopPips` 点时，止损跟随价格移动。
* **保本移动** —— 价格盈利达到 `BreakEvenTriggerPips` 点后，将止损移至入场价并额外留出 `BreakEvenOffsetPips` 缓冲（做空为负方向）。
* **紧急开关** —— `ExitSwitch` 设为 `true` 时，在下一根收盘蜡烛立即平仓并暂停进一步操作。

## 参数
| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `TradeVolume` | `0.1` | 每次交易的量。 |
| `CandleType` | 15 分钟 | 主要使用的蜡烛序列。 |
| `FastMaPeriod` | `6` | 快速 LWMA 周期。 |
| `SlowMaPeriod` | `85` | 慢速 LWMA 周期。 |
| `MomentumThreshold` | `0.3` | 动量指标偏离 100 的最小阈值。 |
| `RewardRatio` | `2` | 止盈与止损的比率。 |
| `StopLossPips` | `20` | 止损距离（点）。 |
| `MaxPositions` | `10` | 最大允许的仓位单位数。 |
| `EnableTrailing` | `true` | 启用追踪止损。 |
| `TrailingStopPips` | `40` | 追踪止损的点数。 |
| `EnableBreakEven` | `true` | 启用保本移动。 |
| `BreakEvenTriggerPips` | `30` | 触发保本的盈利点数。 |
| `BreakEvenOffsetPips` | `30` | 移动到保本后额外偏移的点数。 |
| `ExitSwitch` | `false` | 紧急平仓开关。 |

## 操作步骤
1. 选择交易标的与蜡烛时间框架，设置风控参数。
2. 启动策略，系统会自动订阅蜡烛并计算所有指标。
3. 当所有条件满足时，提交市价单并记录止损/止盈。
4. 每根收盘蜡烛都会重新评估追踪、保本以及紧急开关状态。
5. 平仓可能由止损、止盈、追踪或紧急开关触发。

## 注意事项
* 策略完全基于 StockSharp 自带指标，不直接访问历史缓冲区。
* 需要确保标的拥有有效的 `PriceStep`，否则风险距离不会被设置。
* 出场始终使用市价单，不会修改挂单，与原始 EA 的行为一致。
