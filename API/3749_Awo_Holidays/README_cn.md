# Awo Holidays 策略

## 概述

**AwoHolidaysStrategy** 在 StockSharp 中复现了 MetaTrader 上的 “awo Holidays” 指标。策略会读取 CSV 假日日历，识别周末，并在策略日志中发布多日计划，让您及时了解即将到来的掉期、周末或公共假日。日志输出模仿原版 MQL 注释区，依次列出明天、今天以及可配置数量的历史日期，并附上可选的颜色标签说明。

该组件不会发送任何订单，而是作为图表/状态叠加层提醒操作者注意交易中断。可以在任意品种和周期上运行，但推荐使用日线周期，以便与原始工具保持一致。

## 假日日历格式

策略需要一个使用分号分隔的 CSV 文件，并包含以下四列：

| 列名 | 说明 |
| ---- | ---- |
| `Date` | 日期，格式为 `yyyy.MM.dd`（同时兼容 `yyyy-MM-dd` 或 `dd.MM.yyyy`）。 |
| `Country` | 发生假日的国家或市场。 |
| `Symbols` | 受影响的品种列表，使用逗号分隔；留空表示适用于所有品种。 |
| `Holiday` | 假日的名称。 |

示例：

```
2024.12.25;United States;EURUSD,USDJPY;Christmas Day
```

如果当前品种的标识包含 `Symbols` 列中的任意一个标记，策略会将其显示为 `Holiday in Country`。当文件不存在时会写入警告，并仅保留周末识别功能。

## 参数

| 参数 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `History Depth` | 在明天和今天之后要显示的历史日期数量。 | `3` |
| `Clear On Stop` | 策略停止时是否清空缓存的状态文本。 | `true` |
| `Holiday File` | 假日日历 CSV 文件的相对路径或绝对路径。 | `holidays.csv` |
| `Candle Type` | 触发更新的蜡烛序列。推荐使用日线。 | 1 日时间框架 |
| `Workday Label` | 标注工作日的文本标签。 | `LightBlue` |
| `Weekend Label` | 标注周末的文本标签。 | `Blue` |
| `Holiday Label` | 标注假日的文本标签。 | `DarkOrange` |

## 使用步骤

1. 按上述格式准备 CSV 文件，将其放入终端目录，或通过 `Holiday File` 参数提供绝对路径。
2. 将策略附加到目标品种，确认 `Candle Type` 与需要监控的周期一致。
3. 启动策略。收到第一根完成的蜡烛后，日志会出现如下信息：
   ```
   Holiday overview:
   2024-06-15 | Tomorrow | saturday | - | Blue
   2024-06-14 | Today | workday | Flag Day in United States | DarkOrange
   2024-06-13 | Yesterday | workday | - | LightBlue
   2024-06-12 | 2 days ago | workday | - | LightBlue
   2024-06-11 | 3 days ago | workday | - | LightBlue
   ```
4. 关注策略日志即可掌握周末和假日情况。当 CSV 更新时，重新启动策略即可重新加载。

## 说明

- 品种匹配忽略大小写，并允许部分匹配，以保持与原 MQL 版本一致。
- 如果只想查看明天和今天，可将 `History Depth` 设为 0。
- 颜色标签只是描述性文本，可根据个人图表规范自定义。
- 当 CSV 缺失或包含无法解析的日期时，相应行会被跳过。成功加载的条目数量会写入日志，方便排查。
