# Trade EA Template for News 策略

## 概述
Trade EA Template for News 策略是 MetaTrader 4 专家顾问“Trade EA Template for News”的 C# 版本。原始脚本会在重大经济新闻发布前后停止交易，本移植版在 StockSharp 高级 API 中复现这一思路：

- 默认使用 1 小时蜡烛（可配置的 `CandleType`）。
- 只有在账户没有持仓时才会开仓，与原脚本的 `OrdersTotal()<1` 条件一致。
- 根据新闻重要程度，在事件前后禁用开仓操作。
- 自动设置距离成交价 100 个点的止损/止盈（通过合约的 `Security.Step` 计算实际价格距离）。

## 交易逻辑
1. 每当一根蜡烛收盘，策略就根据最新时间更新新闻日程。上一根蜡烛的开盘价会保存下来，用于下一根蜡烛的比较。
2. 若当前时间处于任何禁交易窗口内，策略会取消挂单并禁止开新仓。
3. 在允许交易且没有持仓时：
   - 若当前蜡烛的收盘价高于上一根蜡烛的开盘价，则买入指定的 `Volume` 数量。
   - 若当前蜡烛的收盘价低于上一根蜡烛的开盘价，则卖出（做空）。
4. `TakeProfitPoints` 与 `StopLossPoints` 以“点”为单位，运行时会乘以 `Security.Step` 转换为绝对价格偏移，随后通过 `StartProtection` 下达保护性指令。

## 手工新闻日历
原 EA 会从 investing.com 或 DailyFX 下载新闻数据。为了提高可移植性，本版本要求手工填充参数 `NewsEventsDefinition`。列表中的每个事件使用分号或换行分隔，单个事件使用逗号分隔字段：

```
YYYY-MM-DD HH:MM,CURRENCIES,IMPORTANCE[,TITLE]
```

- `YYYY-MM-DD HH:MM`：UTC 时间。`TimeZoneOffsetHours` 可以整体平移所有事件（例如设置为 `3` 代表 UTC+3）。
- `CURRENCIES`：与事件相关的货币代码或品种名称，如 `USD`、`EUR/USD`。多个代码可通过 `/`、`,`、`;`、`|` 或空格分隔。
- `IMPORTANCE`：重要程度关键字，支持 `Low`、`Medium`、`Mid`、`Midle`、`Moderate`、`High`、`NFP` 以及包含 `Nonfarm` 或 `Non-farm` 的文本。
- `TITLE`：可选描述，将显示在日志中。

示例：

```
2024-03-01 13:30,USD,High,Nonfarm Payrolls;2024-03-01 15:00,USD,Low,Factory Orders
```

### 禁交易窗口
- 通过 `UseLowNews`、`UseMediumNews`、`UseHighNews`、`UseNfpNews` 决定哪些事件会触发过滤器。
- `LowMinutesBefore/After`、`MediumMinutesBefore/After`、`HighMinutesBefore/After`、`NfpMinutesBefore/After` 指定在新闻前后需要冻结的分钟数。
- `OnlySymbolNews` 启用时，只会匹配当前交易品种中的货币（例如 `EURUSD` → `{EUR, USD}`）；关闭后所有事件都会触发暂停。
- 当同一时间有多个事件时，会选取最高重要度的那一个作为当前状态，并在日志中提示原因以及下一次发布时间。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 订阅的蜡烛类型。 | `1h` |
| `UseLowNews` | 是否考虑低重要度新闻。 | `true` |
| `LowMinutesBefore` / `LowMinutesAfter` | 低重要度新闻前/后的禁交易时间（分钟）。 | `15 / 15` |
| `UseMediumNews` | 是否考虑中等重要度新闻。 | `true` |
| `MediumMinutesBefore` / `MediumMinutesAfter` | 中等重要度新闻前/后的禁交易时间。 | `30 / 30` |
| `UseHighNews` | 是否考虑高重要度新闻。 | `true` |
| `HighMinutesBefore` / `HighMinutesAfter` | 高重要度新闻前/后的禁交易时间。 | `60 / 60` |
| `UseNfpNews` | 是否启用非农 (NFP) 事件。 | `true` |
| `NfpMinutesBefore` / `NfpMinutesAfter` | NFP 前/后的禁交易时间。 | `180 / 180` |
| `OnlySymbolNews` | 只针对当前品种中的货币触发过滤。 | `true` |
| `NewsEventsDefinition` | 手工新闻列表。 | 空 |
| `TimeZoneOffsetHours` | 统一的时区偏移（小时）。 | `0` |
| `TakeProfitPoints` | 止盈距离（点）。 | `100` |
| `StopLossPoints` | 止损距离（点）。 | `100` |

`Volume` 继承自 `Strategy`，需要根据账户规模单独设置。

## 与 MQL 版本的差异
- 不再执行网页请求，避免依赖外部服务，策略完全由手工列表驱动。
- 原来绘制在图表上的标签与竖线改为日志信息，例如 “Trading paused due to high news”。
- MQL 版本固定手数为 0.01，这里改为读取 `Volume` 参数。
- 全部基于高级蜡烛订阅接口实现，没有使用低级价格缓冲区。

## 使用建议
1. 在启动策略前准备好 `NewsEventsDefinition`，如需修改请停止并重新启动以重新解析列表。
2. 根据交易时区调整 `TimeZoneOffsetHours` 与各类新闻的前后缓冲时间。
3. 设置好 `Volume`、投资组合和交易品种，然后启动策略。
4. 关注日志输出，确认是否出现“暂停交易”或“下一次新闻”的提示信息。

根据要求未提供 Python 版本。
