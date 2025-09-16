# 自适应 Renko Duplex 策略

## 概览
**Adaptive Renko Duplex Strategy** 是 `Exp_AdaptiveRenko_Duplex.mq5` 专家顾问在 StockSharp 平台上的移植版本。新版本保留了原策略的核心思想——**为多头和空头分别运行两套自适应 Renko 流**，并通过高层 API 暴露全部逻辑。每个流都会根据近期波动率动态调整砖块高度，从而在价格两侧构建支持/阻力轨道；策略监控这些轨道中出现的趋势反转，并允许对多空参数进行独立配置。

不同于基于虚拟砖块运行的传统 Renko 系统，Duplex 方法监听标准 K 线，并在每根完整 K 线结束后重新计算自适应 Renko 缓冲区。只有在 K 线收盘后才会发出信号，以避免重绘并契合 StockSharp 的事件驱动模型。

## 市场数据与指标
- **K 线订阅**：两个 `DataType` 参数分别指定多头与空头流所使用的 K 线序列，可以选择相同或不同的周期。
- **自适应 Renko 重构**：每个流都内嵌原指标算法。策略在“最小砖块高度（点数）”与 `K × 波动率` 之间取较大值来决定新的砖块尺寸，并维护上/下包络线以及彩色趋势线（上涨时为支撑、下跌时为阻力）。
- **波动率来源**：可在 `AverageTrueRange` 与 `StandardDeviation` 指标之间切换。两者都在各自的 K 线流上运行，并支持自定义回溯长度。

## 交易逻辑
1. **多头检测**
   - 多头流按照设定参数构建自适应砖块。
   - 当延迟 `LongSignalBarOffset` 指定的 K 线上出现 `RenkoTrend.Up`（上升趋势线）时，策略发出市价买单，订单量为 `Volume + |Position|`，以便快速从空头翻转到多头。
   - 如果在同样的延迟窗口内检测到 `RenkoTrend.Down` 且 `LongExitsEnabled` 为真，则立即平掉所有多头仓位。
2. **空头检测**
   - 空头流采用相同的镜像逻辑：出现 `RenkoTrend.Down` 时卖出，`RenkoTrend.Up` 则在 `ShortExitsEnabled` 为真时平空。
3. **信号延迟**：`SignalBarOffset` 控制信号延迟条数，复刻原 EA 中的“信号延后一根 K 线”行为。设为 0 可在最新收盘 K 线上直接响应。
4. **仓位规模**：策略完全依赖 `Strategy.Volume` 属性，因此在启动前务必设置目标手数。

## 风险管理
- **止损/止盈**：距离以“点”为单位设置，并乘以标的 `PriceStep`（若为空则退回到 `MinStep`）后得到绝对价格差。由于 StockSharp 不会自动创建服务器端保护单，所有退出都通过市价单完成。止损逻辑在每次订阅的 K 线收盘时执行。
- **状态跟踪**：策略记录最近一次多头或空头建仓时的价格（基于 K 线收盘价），用以计算与止损/止盈的距离。
- **手工扩展**：如需账户级别的风控，可在外部调用 `StartProtection()` 附加其他保护模块。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `LongCandleType` | 4 小时 | 多头信号使用的 K 线类型。 |
| `ShortCandleType` | 4 小时 | 空头信号使用的 K 线类型。 |
| `LongVolatilityMode` | ATR | 多头砖块使用的波动率指标（ATR 或 StandardDeviation）。 |
| `ShortVolatilityMode` | ATR | 空头砖块使用的波动率指标。 |
| `LongVolatilityPeriod` | 10 | 多头波动率指标的回溯长度。 |
| `ShortVolatilityPeriod` | 10 | 空头波动率指标的回溯长度。 |
| `LongSensitivity` | 1.0 | 多头砖块在波动率基础上的放大倍数。 |
| `ShortSensitivity` | 1.0 | 空头砖块在波动率基础上的放大倍数。 |
| `LongPriceMode` | Close | 多头流使用的价格类型（`HighLow` 或 `Close`）。 |
| `ShortPriceMode` | Close | 空头流使用的价格类型。 |
| `LongMinimumBrickPoints` | 2 | 多头流的最小砖块高度（点数）。 |
| `ShortMinimumBrickPoints` | 2 | 空头流的最小砖块高度。 |
| `LongSignalBarOffset` | 1 | 多头信号确认所需延迟的 K 线数量。 |
| `ShortSignalBarOffset` | 1 | 空头信号确认所需延迟的 K 线数量。 |
| `LongEntriesEnabled` | true | 是否允许多头建仓。 |
| `LongExitsEnabled` | true | 是否允许基于 Renko 信号的多头平仓。 |
| `ShortEntriesEnabled` | true | 是否允许空头建仓。 |
| `ShortExitsEnabled` | true | 是否允许基于 Renko 信号的空头平仓。 |
| `LongStopLossPoints` | 1000 | 多头止损距离（点 × `PriceStep`）。 |
| `LongTakeProfitPoints` | 2000 | 多头止盈距离。 |
| `ShortStopLossPoints` | 1000 | 空头止损距离。 |
| `ShortTakeProfitPoints` | 2000 | 空头止盈距离。 |

> **点值换算**：MetaTrader 中的“点”依赖于报价精度。移植后所有距离都会乘以 `Security.PriceStep`（若不可用则使用 `MinStep`）来得到实际价格增量。请根据标的的最小跳动调整默认值。

## 使用建议
1. **先配置环境**：在启动策略前设置 `Security`、`Portfolio` 与 `Volume`，并确保数据源能够提供所需的全部 K 线周期。
2. **独立调节多空流**：既可以保持对称配置，也可以分别设置不同的周期、波动率源或砖块参数，以实现多空不同的交易风格。
3. **关注日志**：策略在每次进出场时都会通过 `LogInfo` 输出触发的 Renko 水平，便于校验信号是否符合预期。
4. **叠加外部模块**：可结合会话过滤、资产组合风险控制等模块，通过 StockSharp 高层 API 将其与策略组合使用。
5. **回测提示**：在历史回测中，优先选择能够重建目标周期的 K 线生成器，以保持自适应 Renko 的一致性。

## 与原 EA 的差异
- 移除了 MetaTrader 特有功能（魔术号、资金管理模式、滑点控制、消息推送等），仓位规模完全由 `Volume` 决定。
- 原 EA 会同时下发止损/止盈挂单。移植版本改为在 K 线收盘时检查距离并通过市价单退出。
- 所有信号仅在 K 线收盘后评估，避免部分 K 线阶段的重算；对应原 MQL 中的 `IsNewBar` 检查。
- 自适应 Renko 重构算法保持不变，但使用 C# 直接实现，避免额外的指标集合，符合 StockSharp 高层 API 的惯例。

## 推荐拓展
- 搭配更高层级的行情过滤器（如交易时段、波动率门槛）以规避流动性不足的时段。
- 通过 `StartProtection()` 附加跟踪止损或权益保护模块，提升账户层级的风控能力。
- 将生成的支撑/阻力轨道输出到图表或日志中，辅助人工复核策略表现。
