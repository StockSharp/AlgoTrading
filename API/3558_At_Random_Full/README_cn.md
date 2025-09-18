# At Random Full 策略
[English](README.md) | [Русский](README_ru.md)

## 概述
**At Random Full** 是 MetaTrader 5 专家顾问「At random Full」的完整移植版。策略依然通过随机数选择方向，并保留
了所有资金管理开关：方向过滤、加仓数量、最小间距以及时间控制。转换后的实现基于 StockSharp 的高级 API，因而
所有决策都在蜡烛订阅回调中完成，同时通过 `StartProtection` 自动挂载止损和止盈。

## 交易逻辑
1. 每当蜡烛收盘，策略会检查当前是否允许交易（会话过滤、投资组合状态以及 `OnlyOnePosition` 标志）。
2. 伪随机生成器决定买入或卖出。如果 `ReverseSignals` 为真，方向会被翻转，从而复现原始 EA 的反向模式。
3. `Mode` 参数可屏蔽不允许的方向。为了避免同一根 K 线触发多次，策略会记录最近一次多头和空头信号所处的开盘时
   间。
4. 加仓控制与 MQL 版本一致：
   - `MaxPositions` 限制同方向的最多加仓次数；
   - `MinStepPoints` 要求相邻持仓之间至少相隔一定的 MetaTrader 点数，代码会根据价格步长将其转换为实际价格；
   - `CloseOpposite` 会在开新仓之前先关闭相反仓位。
5. 订单通过 `BuyMarket` / `SellMarket` 以归一化后的 `OrderVolume` 执行。

## 仓位与风险管理
- `StartProtection` 根据参数自动下达止损和止盈。当 `TrailingStopPoints` 大于零时，会启用 StockSharp 内置的追踪
  止损。`TrailingActivatePoints` 与 `TrailingStepPoints` 会转换为价格距离并写入日志，实际的追踪逻辑由平台完成。
- 头寸数量会经过归一化处理，遵守证券的 `VolumeStep`、`MinVolume` 与 `MaxVolume` 限制。
- `UseTimeControl` 对应原脚本的时间控制功能，开启后只有在 `[SessionStart, SessionEnd]` 区间内才允许开仓，支持跨
  日区间。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `CandleType` | 用于触发决策流程的蜡烛类型。 | 15 分钟时间框架 |
| `OrderVolume` | 基础下单手数（单位：手）。 | `0.1` |
| `MaxPositions` | 每个方向允许的最大加仓次数（0 表示不限制）。 | `5` |
| `MinStepPoints` | 相邻入场之间的最小点数距离。 | `150` |
| `StopLossPoints` | 止损距离（点）。 | `150` |
| `TakeProfitPoints` | 止盈距离（点）。 | `460` |
| `TrailingActivatePoints` | 触发追踪止损所需的利润阈值（点，记录在日志中）。 | `70` |
| `TrailingStopPoints` | 追踪止损的固定距离，传递给 `StartProtection`。 | `250` |
| `TrailingStepPoints` | 追踪止损的移动步长（同样记录在日志）。 | `50` |
| `OnlyOnePosition` | 若为真，则必须在持仓归零后才会再次入场。 | `false` |
| `CloseOpposite` | 在开仓前先平掉相反方向的仓位。 | `false` |
| `ReverseSignals` | 反转随机信号。 | `false` |
| `UseTimeControl` | 启用交易时间过滤。 | `false` |
| `SessionStart` | 当启用时间过滤时的开始时间（包含）。 | `10:01` |
| `SessionEnd` | 当启用时间过滤时的结束时间（包含）。 | `15:02` |
| `Mode` | 允许的方向（`Both`、`BuyOnly` 或 `SellOnly`）。 | `Both` |
| `RandomSeed` | 随机数种子（0 表示使用环境的 TickCount）。 | `0` |

## 实现细节
- 代码中的注释全部为英文，缩进使用制表符，与仓库规范保持一致。
- 使用 `SubscribeCandles().Bind(...)` 处理蜡烛数据，保证逻辑只在收盘时执行，与原始 EA 的“新柱子”逻辑一致。
- 策略会记录最近一次多头/空头成交价，以便在加仓模式下准确地应用最小步长约束。
- 启动时会在日志中打印止损、止盈以及追踪止损的价格距离，方便检查配置是否正确。

## 使用建议
- 该策略的方向完全随机，更适合测试交易基础设施、验证风险控制或进行学习演示，而不是追求稳定收益。
- 请根据标的的波动与最小价格步长调整 `OrderVolume`、`StopLossPoints` 与 `TakeProfitPoints`。
- 若只希望在特定交易时段运行，可开启 `UseTimeControl` 并设置会话时间。
- 在参数优化或回测需要复现相同随机序列时，可以设置 `RandomSeed` 为固定值。
