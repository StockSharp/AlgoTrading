# 趋势线交叉提醒策略

## 概述
该策略复刻了原始 MetaTrader 专家顾问的行为：跟踪交易者在图表上绘制的水平线和趋势线，一旦价格在完成的 K 线中穿越了这些参考线，立即发送一次性提醒。策略默认不自动下单，重点在于帮助主观交易者监控关键区域。

## 转换要点
- 只有与 `MonitoringColor` 参数匹配的线条才会被处理，对应原 EA 通过颜色筛选图表对象的做法。
- 当检测到穿越时，会在内部标记该线，避免后续 K 线重复触发。效果等同于在 MetaTrader 中把对象颜色改成 `CrossedColor`。
- 由于 StockSharp 无法直接读取终端中的图形对象，水平线和趋势线通过字符串参数定义：水平线使用 `名称|颜色|价格` 结构，趋势线使用 `名称|颜色|起始时间|起始价格|结束时间|结束价格`，并按照这两个锚点延伸为无限直线。
- “弹窗提醒”“推送通知”“邮件通知”三个开关都会写入详细的日志消息，便于上层系统对接真实的通知渠道。

## 参数
| 参数 | 类型 | 说明 |
| --- | --- | --- |
| `MonitoringColor` | `string` | 需要监控的线条颜色标签（不区分大小写）。 |
| `CrossedColor` | `string` | 提醒消息中用于表示“已触发”的颜色标签。 |
| `HorizontalLevelsInput` | `string` | 水平线定义串，条目之间用分号分隔。格式 `名称|颜色|价格`，若省略颜色则默认使用监控颜色。 |
| `TrendlineDefinitions` | `string` | 趋势线定义串，格式 `名称|颜色|起始时间|起始价格|结束时间|结束价格`，时间必须采用 ISO 8601 格式并匹配交易所时区。 |
| `EnableAlerts` | `bool` | 写入主要提醒信息。 |
| `EnableNotifications` | `bool` | 追加一条模拟移动端推送的日志信息。 |
| `EnableEmails` | `bool` | 追加一条模拟邮件提醒的日志信息。 |
| `CandleType` | `DataType` | 用于监控的 K 线类型。 |

## 定义格式
1. 使用分号分隔多个条目，例如 `Daily|Yellow|1.1020;Weekly|Yellow|1.1180`。
2. 水平线可以省略名称或颜色：
   - `1.1050` → 视为 `Horizontal 1`，价格 1.1050，颜色使用监控颜色。
   - `Resistance|1.1180` → 指定名称，颜色仍为监控颜色。
   - `Breakout|Blue|1.1225` → 指定名称和颜色，只有颜色与 `MonitoringColor` 一致时才会监控。
3. 趋势线必须提供两个锚点的时间和价格，例如 `Channel|Yellow|2024-03-14T08:00:00Z|1.0950|2024-03-14T16:00:00Z|1.1080`。策略会像 MetaTrader 一样允许在锚点之外外推。

## 执行流程
1. `OnStarted` 中解析字符串参数并缓存结构化的线条列表。
2. 订阅到的 K 线在收盘后触发 `ProcessCandle`。 
3. 若当前 K 线开盘价与收盘价分别位于线条两侧，则判定为穿越，写入消息并标记该线已触发。
4. 消息包含穿越方向、理论线价以及实际收盘价，便于人工决策。

## 通知机制
策略通过日志输出实现提醒。若宿主平台接入了实际的推送或邮件服务，可基于这些日志进行转发，实现与原 EA 相同的通知体验。

## 使用步骤
1. 选择标的与周期，设置合适的 `CandleType`。
2. 根据图表中的线条填写 `HorizontalLevelsInput` 与 `TrendlineDefinitions`。
3. 按需打开或关闭三个通知开关。
4. 启动策略，必要时可以在 StockSharp 图表上手动绘制相同的线条以便观察。

## 示例配置
```
MonitoringColor = "Yellow"
CrossedColor = "Green"
HorizontalLevelsInput = "DailyPivot|Yellow|1.1025;WeeklyHigh|Yellow|1.1100"
TrendlineDefinitions = "UpperChannel|Yellow|2024-03-14T08:00:00Z|1.0950|2024-03-14T16:00:00Z|1.1080"
EnableAlerts = true
EnableNotifications = true
EnableEmails = false
CandleType = 15 分钟 K 线
```
该配置监控两个静态水平价位和一条上升趋势线。当收盘价首次跨越任意线条时，会在日志中看到类似 `Price crossed horizontal line 'DailyPivot' upward at ...` 的提示。

## 风险与扩展
- 策略不执行下单操作，如需自动化交易请搭配其他执行模块。
- 要重置提醒，可停止后重新启动策略，或修改定义字符串。内部状态不做持久化，以保持与原 EA 一致。
- 可以在 `ProcessCandle` 中增加时间过滤、波动率限制等附加条件，以满足更复杂的风控需求。

