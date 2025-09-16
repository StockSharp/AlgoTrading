# Exp CandlesticksBW Tm 策略

该策略在 StockSharp 高级 API 上复刻了 MetaTrader 专家顾问 **Exp_CandlesticksBW_Tm**。它使用 Bill Williams 的 Candlesticks BW 指标，通过组合 Awesome Oscillator (AO) 与 Accelerator Oscillator (AC) 的方向来为蜡烛染色，并据此捕捉动能加速或减速。可选的交易时段过滤器可以让策略只在特定时间范围内进出场。

## 工作流程

1. 订阅所选时间框（默认 **H4**），将每根已完成蜡烛传递到 5/34 周期的 Awesome Oscillator。随后利用 5 周期简单移动平均线对 AO 进行平滑，得到 Accelerator 值。
2. 每根蜡烛被划分为六种颜色中的一种：两种多头动能颜色（AO、AC 同时上升）、两种空头动能颜色（AO、AC 同时下降）以及两种中性颜色（AO、AC 方向不一致）。蜡烛实体的方向决定具体使用哪一种色调。
3. 使用环形缓冲区保存最近的颜色索引及其开盘时间。参数 **SignalBar** 指定需要评估的历史蜡烛（默认 1，表示上一根蜡烛），再往前一根蜡烛用于上下文判断。
4. 当较早的蜡烛处于多头动能区，而信号蜡烛离开该区域时触发多头开仓。空头逻辑与之相反。离场信号使用相同的颜色条件来关闭已有仓位。
5. 当启用时间过滤 (**UseTimeFilter**) 时，交易仅允许在 **StartHour:StartMinute** 与 **EndHour:EndMinute** 之间进行。一旦超出窗口，策略会立即平掉全部仓位，避免持仓过夜。
6. 通过 `StartProtection` 注册止损与止盈，参数以价格点为单位，会根据合约的最小价格步长自动换算。

## 交易规则

- **开多**：`SignalBar + 1` 的颜色索引 `< 2`（动能向上），同时 `SignalBar` 的颜色索引 `> 1`。只有在允许做多且当前无多单时才会进场。
- **开空**：`SignalBar + 1` 的颜色索引 `> 3`（动能向下），同时 `SignalBar` 的颜色索引 `< 4`。
- **平多**：`SignalBar + 1` 的颜色索引 `> 3`，并且启用了多头平仓。
- **平空**：`SignalBar + 1` 的颜色索引 `< 2`，并且启用了空头平仓。
- 启用时间过滤后，无论是否出现颜色信号，只要超出交易区间都会立即平仓。

## 参数

| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `CandleType` | AO/AC 使用的时间框。 | `TimeSpan.FromHours(4).TimeFrame()` |
| `Volume` | 开仓手数。 | `1m` |
| `SignalBar` | 要回溯的已完成蜡烛数量（1 代表上一根）。 | `1` |
| `StopLossPoints` | 止损距离（点数，0 表示关闭）。 | `1000m` |
| `TakeProfitPoints` | 止盈距离（点数，0 表示关闭）。 | `2000m` |
| `EnableLongEntries`, `EnableShortEntries` | 是否允许开多/开空。 | `true` |
| `EnableLongExits`, `EnableShortExits` | 是否允许根据信号平多/平空。 | `true` |
| `UseTimeFilter` | 是否启用交易时间过滤。 | `true` |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | 交易窗口（开始时间包含，结束时间在小时相等时不包含）。 | `0/0/23/59` |

## 其他说明

- 止损与止盈会按照标的价格步长自动换算实际价格。
- 信号时间戳基于所分析蜡烛的收盘时间，可避免在同一根 K 线内重复下单。
- 未提供 Python 版本，以保持与原始 MQL 包相同的结构。
