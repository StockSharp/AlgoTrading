# Envelope Limit Ladder 策略

**Envelope Limit Ladder Strategy** 是将 MetaTrader 专家顾问 `E_2_12_5min.mq4`（ID 7671）移植到 StockSharp 的 C# 实现。该策略在 5 分钟蜡烛上重建原始的 EMA 包络线限价梯形结构，并保留三个分级止盈与跟踪止损的管理方式。

## 策略思路

1. **包络过滤器**：在可配置的 `EnvelopeCandleType` 时间框架上计算移动平均包络（默认 EMA144、偏移 0.05%），得到中线与上下轨。
2. **信号蜡烛**：在 `CandleType` 订阅（默认 5 分钟）上评估信号。当上一根蜡烛的收盘价位于中线与最近一条轨道之间时，在中线位置挂出限价单。
3. **限价梯形**：同时最多放置三张买入限价单与三张卖出限价单：
   - 入场价：对齐后的包络中线。
   - 止损价：包络的对侧轨道。
   - 止盈价：轨道 ± 用户设定的点数偏移（默认 8、13、21 点）。
4. **交易时间窗**：仅当满足 `TradingStartHour < Hour < TradingEndHour` 时才会创建挂单；当小时数达到 `TradingEndHour` 时，所有未成交的限价单都会被取消。
5. **持仓管理**：每张成交的限价单都会立即生成自己的止损与止盈挂单。可选的跟踪模式会在价格突破包络后把止损移动到移动平均线（或保持在对侧轨道）。

## 参数说明

| 参数 | 默认值 | 描述 |
|------|--------|------|
| `CandleType` | 5 分钟 | 用于识别信号的蜡烛类型。 |
| `EnvelopeCandleType` | 5 分钟 | 计算包络的蜡烛类型，可模拟 MT4 的 `EnvTimeFrame`。 |
| `EnvelopePeriod` | 144 | 包络移动平均的周期。 |
| `MaMethod` | EMA | 移动平均方法（`SMA`、`EMA`、`SMMA`、`LWMA`）。 |
| `EnvelopeDeviation` | 0.05 | 包络宽度百分比（0.05 表示 0.05%）。 |
| `TradingStartHour` | 0 | 允许挂单的起始小时（严格大于）。 |
| `TradingEndHour` | 17 | 取消所有挂单的小时（严格小于）。 |
| `FirstTakeProfitPoints` | 8 | 第一阶梯的止盈点数。 |
| `SecondTakeProfitPoints` | 13 | 第二阶梯的止盈点数。 |
| `ThirdTakeProfitPoints` | 21 | 第三阶梯的止盈点数。 |
| `UseOppositeEnvelopeTrailing` | `true` | `true` 表示将止损保持在对侧轨道；`false` 表示突破后移动到中线，对应 MT4 的 `MaElineTSL` 选项。 |
| `OrderVolume` | 0.1 | 每个挂单的成交量，替代 MT4 中的 `LotsOptimized` 资金管理。 |

## 行为细节

- 每个成交的限价单都会拥有独立的止损/止盈订单，其平仓不会影响其余阶梯。
- 跟踪止损只会在价格越过包络并形成浮盈后启动，且只向盈利方向移动。
- 当 `EnvelopeCandleType` 与 `CandleType` 不同时，策略会复用辅助订阅上最新的包络值，从而贴近 MT4 在高周期上调用指标的方式。
- 为保证在 StockSharp 中行为可预测，原始 EA 的动态手数逻辑被固定的 `OrderVolume` 参数取代。

## 使用建议

- 按照原策略的设置选择包络时间框架（例如 5 分钟、1 小时等），以获得接近 MT4 的表现。
- 若希望突破后把止损抬到均线位置，将 `UseOppositeEnvelopeTrailing` 设为 `false`。
- 联合优化包络偏移与各阶梯的止盈距离；梯形间距依赖于当前市场波动性。
