# 时间文本对象显示策略

## 概述

本策略直接转换自 MetaTrader 4 脚本 **time_objtxt.mq4**。原始脚本会遍历图表上所有文本对象 (`OBJ_TEXT`)，并将对象携带的时间戳打印到日志中，不包含任何交易指令。

StockSharp 版本保持相同的思路：在启动阶段读取一组可配置的文本对象定义，解析时间戳并写入策略日志。这样就可以在 StockSharp 环境中记录重要事件或备注，并快速查看它们的时间。

## 主要特性

- **不执行交易。** 策略仅提供信息反馈，不会发送任何市价单或限价单。
- **单一配置参数。** `TextObjectDefinitions` 采用分号分隔的格式，每个条目为 `名称@时间`（例如 `My Text@2015.06.30 11:53:24`）。
- **灵活的日期解析。** 支持多种常见的 MetaTrader 日期格式，兼容短横线、点号以及可选的时区偏移。
- **详细日志。** 每个有效条目都会输出 `The time of object <名称> is <时间>`，与原脚本一致。
- **输入校验。** 若条目格式有误，将在日志中给出警告，便于快速修复。

## 参数说明

| 参数 | 描述 | 备注 |
|------|------|------|
| `TextObjectDefinitions` | 需要检查的文本注释列表。每个元素由名称和时间组成，中间使用 `@` 连接。 | 默认值：`My Text@2015.06.30 11:53:24`。会自动忽略两端空格。 |

### 支持的时间格式

解析器接受以下模式（允许空白）：

- `yyyy-MM-dd HH:mm:ss`
- `yyyy-MM-ddTHH:mm:ss`
- `yyyy-MM-dd HH:mm:ss zzz`
- `yyyy-MM-ddTHH:mm:sszzz`
- `yyyy.MM.dd HH:mm:ss`
- `yyyy.MM.ddTHH:mm:ss`
- `yyyy.MM.dd HH:mm:ss zzz`
- `yyyy.MM.ddTHH:mm:sszzz`

如果以上格式都不匹配，将尝试使用当前系统区域进行解析。解析失败时，日志会提示 `Failed to parse time '<值>' for text object '<名称>'.`

## 工作流程

1. 在 `TextObjectDefinitions` 中填写需要检查的注释，多个条目用 `;` 分隔。
2. 启动策略。`OnStarted` 会先写入提示信息，然后逐条处理定义。
3. 打开日志即可查看每个有效文本对象的时间戳。

## 与 MQL 脚本的差异

- StockSharp 无法直接访问 MetaTrader 的图表对象，因此策略改为读取用户提供的定义，而不是自动扫描图表。
- 日志输出使用 StockSharp 的 `LogInfo` 与 `LogWarning` 方法。
- 日期解析支持多种区域格式，以匹配 MetaTrader `TimeToString(..., TIME_DATE | TIME_SECONDS)` 的行为。

## 使用建议

- 使用有意义的名称（例如 `SessionStart`、`NewsRelease`）以便快速识别。
- 建议定期清理过期条目，避免日志信息过多。
- 策略不会发单，因此可以与其他交易策略并行运行，作为信息辅助工具。
