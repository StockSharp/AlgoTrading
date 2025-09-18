[English](README.md) | [Русский](README_ru.md)

# Currency Loader 策略

该策略将 MetaTrader 的辅助脚本 **Currency_Loader.mq4** 移植到 StockSharp。
它持续监听多个周期的蜡烛图，定期将完整历史导出到 CSV 文件，并保留原始脚本的日志行为。
策略不会执行任何交易，仅用于数据采集。

## 工作流程

- 启动时根据所选证券的标识创建 `Export_History/<symbol>` 目录。
- 通过高层 `SubscribeCandles` API 订阅启用的周期，并把已完成的蜡烛缓存在内存中，最多 `MaxBarsInFile` 条。
- 周期性计时器 (`FrequencyUpdateSeconds`) 在满足 `BarsMin` 条历史后重写对应的 CSV 文件。
- 每次导出都会覆盖原有文件，从而保持与 MQL 版本一致的快照行为。
- 可选地向 StockSharp 日志 (`AllowInfo`) 和独立的文本日志 (`AllowLogFile`) 输出状态信息。

## 导出格式

每个启用的周期都会在目录中生成 `<symbol>_<TF>.csv` 文件，格式与原脚本一致：

```
"Date" "Time" "Open" "High" "Low" "Close" "Volume"
2023.09.11,14:25,1.07230,1.07310,1.07180,1.07250,123
...
```

- 日期格式为 `yyyy.MM.dd`，时间为 `HH:mm`（分钟粒度，对应 `TIME_MINUTES`）。
- 价格按照证券的最小价格步长自动选择小数位数。
- 成交量四舍五入为整数，保持 MQL 行为。

## 参数

| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `BarsMin` | 执行导出前所需的最少完成蜡烛数。 | 100 |
| `MaxBarsInFile` | 每个周期在缓存中保留的最大蜡烛数量，超出部分会被丢弃。 | 20000 |
| `FrequencyUpdateSeconds` | 触发文件重写的计时器周期（秒）。 | 60 |
| `LoadM1` / `LoadM5` / ... / `LoadMN` | 为相应周期生成 CSV（M1、M5、M15、M30、H1、H4、D1、W1、MN1）。 | `false` |
| `AllowInfo` | 是否向 StockSharp 日志写入信息。 | `true` |
| `AllowLogFile` | 是否在导出目录中追加 `LOGCurrency_Loader_<date>.log`。 | `true` |

## 与 MQL 版本的差异

- 目录结构保持一致，但会移除证券名称中不合法的文件字符。
- 计时器使用 StockSharp 的策略计时器，而不是 `Sleep()` 循环，在回测中更可靠。
- 通过 `SubscribeCandles().Bind(...)` 收集蜡烛数据，无需 `ArrayCopyRates`。
- 日志集成到 StockSharp 框架，同时保留可选的外部日志文件。
- CSV 保留相同的表头、列顺序和数值格式。

## 使用建议

1. 在启动策略前配置目标证券并勾选所需的周期。
2. 确保账号能够获取足够的历史数据，否则无法达到 `BarsMin` 要求。
3. 停止策略会立即执行最后一次导出，并清空缓存以释放内存。
4. 查看 `Export_History/<symbol>` 目录以获取 CSV 文件以及可能存在的 `LOGCurrency_Loader_<date>.log`。
