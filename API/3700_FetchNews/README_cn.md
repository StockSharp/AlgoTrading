# Fetch News 策略

## 概述
- 将 MetaTrader 5 专家顾问 *FetchNews*（NewsEA.mq5）移植到 StockSharp 的高级 API。
- 读取用户准备的宏观经济日历，在预定时间段触发提醒或自动挂出买卖止损组合。
- 适合手动筛选新闻的交易者，用于在重大数据公布前自动做好下单准备。
- 通过 Level1 行情获得最新买卖价，并使用 `SetStopLoss`/`SetTakeProfit` 辅助函数设置保护单。

## 运行模式
1. **Alerting**（提醒模式）。
   - 依据 `AlertImportance`（最低重要级别）以及 `OnlySymbolCurrencies`（是否只关注品种相关货币）筛选事件。
   - 在日志中写入 `Upcoming important news event: …` 格式的提示，不会发送任何委托。
2. **Trading**（交易模式）。
   - 同样应用货币过滤条件。
   - 事件名称必须包含 `TradingKeywords`（以 `;`、`,` 或换行分隔）中的任意关键词。
   - 当当前时间落在 `[EventTime - LookBackSeconds, EventTime + LookAheadSeconds]` 范围内时，策略会确认：
     - 没有持仓，且不存在其他挂出的新闻组合单；
     - Level1 行情已提供最新 bid/ask；
     - 品种的最小报价步长已知，从而把“点”转换为实际价格。
   - 满足条件后挂出两个止损委托：
     - 买入止损价 `ask + TakeProfitPoints * PriceStep`；
     - 卖出止损价 `bid - TakeProfitPoints * PriceStep`。
   - 任一方向成交后，立即撤销另一张委托，并调用 `SetStopLoss`、`SetTakeProfit` 设置对应仓位的保护单。
   - `OrderLifetimeSeconds` 到期或者仓位平仓时会撤销剩余的挂单。

## 日历格式
`CalendarEventsDefinition` 以换行或分号分隔，每条记录至少包含四个逗号分隔字段：

```
日期时间, 货币, 重要级别, 事件名称
```

- **日期时间**：使用 `DateTime.TryParse`（不变文化）解析，例如 `2024-06-12 12:30`。
- **货币**：事件对应的货币代码，如 `USD`、`EUR`。
- **重要级别**：`Low`、`Moderate`、`High` 或等价写法（`medium`、`important`）。
- **事件名称**：完整描述，额外的逗号会并入名称。

`TimeZoneOffsetHours` 用于在转换为 UTC 之前修正时区。例如 CSV 以东部夏令时间（UTC-4）记录，则填 `-4`。策略使用 UTC 与 Level1 消息时间比较。

### 示例
```
2024-06-12 12:30,USD,High,Consumer Price Index (YoY)
2024-06-12 14:00,USD,Moderate,FOMC Interest Rate Decision
2024-06-13 08:00,EUR,High,ECB Press Conference
```

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `Mode` | 选择提醒或交易模式。 | `Alerting` |
| `OrderVolume` | 每个挂单的手数。 | `0.1` |
| `TakeProfitPoints` | 入场到止盈的点数。 | `150` |
| `StopLossPoints` | 入场到止损的点数。 | `150` |
| `OrderLifetimeSeconds` | 挂单有效期（秒）。 | `500` |
| `LookBackSeconds` | 事件前多少秒开始关注。 | `50` |
| `LookAheadSeconds` | 事件后多少秒仍然有效。 | `50` |
| `TradingKeywords` | 触发交易模式的关键词列表。 | `cpi;ppi;interest rate decision` |
| `CalendarEventsDefinition` | 日历文本。 | 空 |
| `TimeZoneOffsetHours` | 日历时间的时区修正（小时）。 | `0` |
| `AlertImportance` | 提醒模式的最低重要级别。 | `Moderate` |
| `OnlySymbolCurrencies` | 仅处理与品种代码相关的货币。 | `true` |

## 工作流程
1. `OnStarted` 清空内部状态，提取货币代码、关键词并解析日历，然后订阅 Level1 行情；`StartProtection()` 启用保护单管理。
2. `ProcessLevel1` 保存最新 bid/ask，检查挂单是否过期，并扫描处于激活窗口的事件。
3. 提醒模式仅输出一次日志；交易模式调用 `ProcessTrading`，在满足条件时挂出买卖止损组合。
4. `OnNewMyTrade` 判断成交方向，撤销未成交的对冲委托，并为当前仓位设置止损/止盈。
5. `OnPositionChanged` 在仓位归零时清除挂单到期计时器，使策略能够响应下一条新闻。

## 与 MetaTrader 版本的差异
- MetaTrader 可以直接从经纪商获取日历；在 StockSharp 中需要手动把事件填入 `CalendarEventsDefinition`。
- MT5 允许在挂单中直接指定止损/止盈；此版本改为成交后调用 `SetStopLoss`、`SetTakeProfit`。
- 货币识别基于代码中的三字母片段（如 `EURUSD`、`GBP/JPY`），必要时可关闭 `OnlySymbolCurrencies`。
- 提醒通过 `LogInfo` 记录到日志，而不是弹出窗口。

## 使用建议
- 把日历维护在外部文件，启动前粘贴内容到 `CalendarEventsDefinition`。
- 关键词列表可根据策略需要调整，例如 `cpi;ppi;interest rate decision;nfp`。
- 确保行情源提供 Level1 最优价，否则无法计算挂单价格。
- 先在 StockSharp 模拟器中验证保护单行为，再用于真实交易。
