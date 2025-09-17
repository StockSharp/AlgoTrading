# MARE5.1 策略

## 概述
MARE5.1 策略是 MetaTrader 4 专家顾问 `MARE5_1.mq4` 的 StockSharp 移植版本。原始 EA 在 1 分钟级别运行，通过比较两条在多个历史偏移处的简单移动平均线来判断趋势反转。C# 实现保留了所有核心思想，提供可配置的参数、以 MetaTrader “点”为单位的保护性订单设置，并增加了详细的交易时间过滤器。

## 交易逻辑
1. **行情数据**
   - 仅订阅一个由 `CandleType` 指定的蜡烛序列（默认 1 分钟）。
   - 只在蜡烛完全结束后才处理，避免使用未收盘的数据。
2. **指标体系**
   - 使用两条 `SimpleMovingAverage`：快速均线（周期 `FastPeriod`）和慢速均线（周期 `SlowPeriod`）。
   - 两条均线都向前平移 `MovingAverageShift` 个柱，与 MQL `iMA` 函数中的 `ma_shift` 参数完全对应。
   - 另外计算 `MovingAverageShift + 2` 和 `MovingAverageShift + 5` 的偏移值，以复制 `iMA(..., shift=2/5)` 的调用效果。
3. **信号判定**
   - 均线之间的差值必须至少大于一个价格步长（即 MetaTrader 的 `Point`）。若工具的 `PriceStep` 为零，则只需差值为正。
   - **做空条件：**
     - 前一根蜡烛为阴线（`Close < Open`）。
     - 当前平移后的慢速均线高于快速均线。
     - 向前 2 根和 5 根柱子时，快速均线仍旧高于慢速均线，表明多头趋势正在反转。
   - **做多条件：**
     - 前一根蜡烛为阳线（`Close > Open`）。
     - 当前平移后的快速均线高于慢速均线。
     - 向前 2 根和 5 根柱子时，慢速均线仍旧高于快速均线，证明空头趋势已开始转向。
   - 同一时间只允许持有一笔仓位，对应原 EA 中的 `OrdersTotal() < 1` 判断。
4. **时间过滤**
   - 只有当蜡烛收盘时间的小时数位于 `[TimeOpenHour, TimeCloseHour]` 区间内时才会触发交易。
   - 如果结束小时小于开始小时，则视为跨夜时段（例如 `22` 到 `5`）。

## 风险控制
- 调用 `StartProtection` 将 `StopLossPoints` 与 `TakeProfitPoints`（以 MetaTrader 点为单位）转换为绝对价格偏移，并绑定在后续订单上。
- 原始代码虽然声明了 `TrailingStop`，但并未使用，因此移植版本也未实现追踪止损。
- 下单数量固定为 `TradeVolume`，策略不会加仓或分批减仓。

## 参数列表
| 名称 | 说明 | 默认值 | 备注 |
| --- | --- | --- | --- |
| `TradeVolume` | 市价单的下单手数。 | `7.8` | 最终手数由连接器根据交易所规则自动修正。 |
| `FastPeriod` | 快速简单移动平均的周期。 | `13` | 周期越短，对价格变化越敏感。 |
| `SlowPeriod` | 慢速简单移动平均的周期。 | `55` | 定义大级别趋势背景。 |
| `MovingAverageShift` | 两条均线共同的前移距离。 | `2` | 等价于 MQL `iMA` 的 `ma_shift`。 |
| `StopLossPoints` | 止损距离（MetaTrader 点）。 | `80` | 与 `PriceStep` 相乘后转化为绝对价差。 |
| `TakeProfitPoints` | 止盈距离（MetaTrader 点）。 | `110` | 设为 `0` 可禁用止盈。 |
| `TimeOpenHour` | 允许交易的起始小时（0–23）。 | `8` | 与蜡烛收盘时间对比。 |
| `TimeCloseHour` | 允许交易的结束小时（0–23）。 | `14` | 可以小于 `TimeOpenHour` 以覆盖夜盘。 |
| `CandleType` | 计算所用的蜡烛类型。 | `1 分钟` | 可替换为任意 `TimeFrame()` 值。 |

## 实现细节
- 借助 `Shift` 指标生成延迟序列，无需直接访问指标缓冲区即可复现 MQL 中的历史位移。
- `IsDifferenceSatisfied` 统一处理“差值≥点值”的判断，从而适配不同报价精度的品种。
- 交易时间过滤使用蜡烛的收盘时间，是对 MetaTrader 中 `Hour()` 函数最合理的近似。
- 代码完全基于高层 API（`SubscribeCandles().Bind(...)`），并按照项目要求仅使用英文注释。

## 与 MQL 版本的差异
- 仅在蜡烛收盘后生成信号，避免 MetaTrader 中可能出现的盘中重绘问题。
- 使用 `StartProtection` 统一管理止损/止盈，而不是在每次下单时手动设置。
- 删除了未生效的 `TrailingStop` 参数，防止误导使用者。
- 时间过滤器原生支持跨夜区间，而原 EA 默认假设 `TimeOpen <= TimeClose`。
