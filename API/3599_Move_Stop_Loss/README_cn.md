# 移动止损策略

## 概述
- 将 MetaTrader 4 专家顾问 **MoveStopLoss.mq4 (43793)** 转换为 StockSharp 框架实现。
- 仅管理已经打开的持仓，通过动态移动止损来保护盈利。
- 支持两种跟踪模式：基于 ATR 的自动模式以及固定距离的手动模式。

## 交易逻辑
1. 订阅所选周期的K线，并使用设定的周期计算平均真实波幅（ATR）。
2. 对最近 `AtrLookback` 个 ATR 数值取滚动最大值，对应原始 EA 中对过去 30 根柱子的最大 ATR 搜索。
3. 根据模式确定跟踪距离：
   - 当 `AutoTrail` 为 true 时，使用 `AtrMultiplier`（默认 0.85）乘以 ATR 最大值，得到以价格单位表示的距离。
   - 当 `AutoTrail` 为 false 时，将 `ManualDistance`（以最小价格跳动计）转换为价格单位。
4. 多头仓位：
   - 识别新的进场并重置内部止损订单。
   - 等待价格超过记录的入场价后，再开始移动止损。
   - 始终保持卖出止损距离最新收盘价 `trailDistance`，从而锁定浮动盈利。
5. 空头仓位使用对称的逻辑，将买入止损保持在收盘价上方 `trailDistance` 处。
6. 当没有持仓时，策略会取消挂出的保护性止损并清除内部状态。

## 参数说明
| 参数 | 说明 |
| --- | --- |
| `AutoTrail` | 是否启用基于 ATR 的自动跟踪距离。关闭时使用 `ManualDistance`。 |
| `ManualDistance` | 当 `AutoTrail` 关闭时使用的固定跟踪距离，单位为最小价格跳动。 |
| `AtrMultiplier` | `AutoTrail` 模式下作用于 ATR 最大值的乘数。 |
| `AtrPeriod` | ATR 指标的计算周期，默认与原策略相同为 7。 |
| `AtrLookback` | 计算 ATR 最大值时所包含的历史数据数量，默认 30。 |
| `CandleType` | 用于计算的K线类型（时间框架）。 |

## 实现要点
- 策略本身不会开仓，只负责通过止损订单保护已有净头寸。
- 使用 StockSharp 的高级 API，通过 `SubscribeCandles` 与 `BindEx` 连接 ATR 与最大值指标，无需手动保存历史数据。
- 调用 `StartProtection()` 确保策略能够安全地撤销或重新挂出止损订单。
- 按要求，代码中的注释全部使用英文。
