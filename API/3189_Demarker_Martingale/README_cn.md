# Demarker 马丁格尔策略（StockSharp）

## 概述
**Demarker 马丁格尔策略** 在 StockSharp 高级 API 上复刻了 MetaTrader 专家顾问 “Demarker Martingale”。系统使用中期的 DeMarker 振荡器信号结合更高周期的 MACD 趋势过滤器。开仓后应用马丁格尔式加仓、固定止损/止盈、保本保护以及与原始 EA 相同的移动止损。

## 核心交易流程
1. **数据订阅**：根据参数订阅交易周期（默认 15 分钟 K 线）用于信号判断，同时订阅更高周期（默认月线）用于 MACD 过滤。
2. **DeMarker 触发**：当 DeMarker 值大于中性阈值 `DemarkerThreshold`（默认 0.5）且出现 `Low[2] < High[1]` 的多头重叠结构时，生成做多候选。若 DeMarker 低于阈值且 `Low[1] < High[2]`，则准备做空。
3. **MACD 确认**：更高周期 MACD 必须与方向一致。做多需 MACD 主线高于信号线；做空则要求主线低于信号线。该逻辑复现了原始 EA 使用月线 MACD 的方式。
4. **下单执行**：满足条件时按当前马丁格尔规模市价入场，每次仅持有一个方向的仓位。
5. **持仓管理**：持仓期间在每根收盘 K 线后检查止损、止盈、保本价或移动止损是否触发，一旦满足立即以市价全部平仓。

## 资金管理
- **初始手数**：使用 `InitialVolume` 并按照品种的 `VolumeStep`、`VolumeMin`、`VolumeMax` 做对齐。
- **马丁格尔加码**：亏损后下一笔手数按 `DoubleLotSize` 设置决定是乘以 `MartingaleMultiplier` 还是增加 `LotIncrement`。盈利则恢复到基础手数。连续加码次数受 `MaxMartingaleSteps` 限制。
- **止损/止盈**：距离以 MetaTrader 风格的 “点” 表示。点值根据 `ticksize` 是否为 0.00001 或 0.001 自动放大 10 倍，与原版保持一致。
- **保本保护**：未实现利润达到 `BreakEvenTriggerPips` 后，将止损移动到入场价加上（或减去）`BreakEvenOffsetPips`。
- **移动止损**：利润超过 `TrailingStopPips` 时，内部跟踪价随每根 K 线收盘更新，模仿 EA 的 TrailingStop 行为。

## 参数说明
| 名称 | 说明 |
| --- | --- |
| `CandleType` | 用于生成信号的交易周期。 |
| `MacdCandleType` | 计算 MACD 趋势过滤器的高周期。 |
| `DemarkerPeriod` | DeMarker 指标周期。 |
| `DemarkerThreshold` | 多空判定的中性阈值。 |
| `MacdFast` / `MacdSlow` / `MacdSignal` | MACD 三个 EMA 周期。 |
| `InitialVolume` | 初始下单手数。 |
| `MartingaleMultiplier` | `DoubleLotSize` 为真时的倍增系数。 |
| `LotIncrement` | `DoubleLotSize` 为假时的手数增量。 |
| `DoubleLotSize` | 选择倍增或累加的马丁格尔模式。 |
| `MaxMartingaleSteps` | 连续加码的最大次数。 |
| `StopLossPips` | 止损距离（点）。 |
| `TakeProfitPips` | 止盈距离（点）。 |
| `TrailingStopPips` | 移动止损距离（点）。 |
| `UseBreakEven` | 是否启用保本保护。 |
| `BreakEvenTriggerPips` | 触发保本的利润阈值。 |
| `BreakEvenOffsetPips` | 保本止损相对入场的偏移量。 |

## 转换要点
- 点值计算遵循原 EA 的 `ticksize` 逻辑，确保在 3/5 位报价货币对上的风险距离一致。
- MACD 过滤使用 `MovingAverageConvergenceDivergenceSignal` 指标并处理独立的高周期行情，重建月线趋势判断。
- 马丁格尔管理记录加权平均入场价与实际盈亏，用于判断下一笔手数应加码还是复位。
- 所有保护动作都通过市价单完成，配合 `StartProtection` 避免在受保护模式下直接修改挂单。

## 使用建议
- 绑定的证券需正确填写 `PriceStep`、`VolumeStep`、`VolumeMin`、`VolumeMax` 等属性，以保证点值和手数对齐。
- 可尝试将 `MacdCandleType` 调整为周线等更快周期，以适应不同市场节奏。
- 优化参数时可同时调整 `DemarkerThreshold`、`TrailingStopPips` 与马丁格尔参数，以控制最大回撤。
- 由于马丁格尔会在亏损后放大仓位，建议结合投资组合级的风险限制或交易时段过滤来实盘运行。
