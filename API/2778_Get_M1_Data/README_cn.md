[English](README.md) | [Русский](README_ru.md)

# 导出一分钟数据的策略

该策略复刻了 MetaTrader 测试中用于导出一分钟数据的工具。
它会订阅完成的 K 线，保存开、高、低、收以及成交量，并在策略停止时写入文件。
无论是回测还是实时连接都可以使用，从而在不依赖 `.hst` 的情况下获得整洁的历史快照。

## 工作流程

1. 启动时根据参数订阅指定的 K 线类型（默认是 1 分钟）。
2. 每根收盘 K 线都会被保存，包括价格和成交量。
3. 停止策略时，根据开关选择导出 CSV 或二进制文件。
4. 写入完成后会清空缓冲区，方便再次运行。

实现完全基于 StockSharp 的高级 API，通过 `SubscribeCandles` 接收数据，
在 `ProcessCandle` 中处理，而文件操作被放到 `OnStopped`，避免阻塞行情线程。

## 参数说明

- **File Name** – 生成文件的基础路径，会自动附加 `.csv` 或 `.bin` 扩展名。
  如果指定的是相对路径，会相对于当前目录解析，并在需要时创建文件夹。
- **Candle Type** – 请求的 K 线类型，默认是一分钟，也可以调整为其他周期。
- **Write CSV** – 开启文本导出，包含表头和逗号分隔的数值，便于 Excel 或数据分析使用。
- **Write Binary** – 生成紧凑的二进制文件。每条记录保存 UTC 时间（`DateTime.ToBinary()` 形式）
  以及价格和成交量，前面附带一个简单的 `(version = 1, count = N)` 头部。

## 输出格式

### CSV 文件

采用 UTF-8（无 BOM）编码，列结构如下：

| 列名 | 描述 |
| --- | --- |
| Time | 开盘时间，格式 `yyyy-MM-dd HH:mm:ss`，保留原始时区。 |
| Open | 开盘价，使用 InvariantCulture 格式化。 |
| High | 最高价，使用 InvariantCulture 格式化。 |
| Low | 最低价，使用 InvariantCulture 格式化。 |
| Close | 收盘价，使用 InvariantCulture 格式化。 |
| Volume | 该周期的总成交量或 tick 成交量。 |

### 二进制文件

文件头由两个 `int` 组成（版本号、记录数），随后是每根 K 线的数据：

1. `long` – `DateTime.UtcDateTime.ToBinary()` 结果。
2. `decimal` – 开盘价。
3. `decimal` – 最高价。
4. `decimal` – 最低价。
5. `decimal` – 收盘价。
6. `decimal` – 成交量（优先 `TotalVolume`，缺失时使用 `Volume`）。

格式设计尽可能简单，方便在其他语言或脚本中解析。

## 使用建议

- 选择好标的和时间范围后运行策略，停止时即可触发导出。
- 如果同时关闭 `WriteCsv` 和 `WriteBinary`，策略会记录提示并跳过写文件。
- 当数据源没有 `TotalVolume` 时，会退回到 `Volume` 字段，以保持与原始 MQL 脚本一致的行为。
- 想生成多个文件时，可设置不同的 `FileName` 并多次运行。
- 策略不会发送订单，可与其他分析策略并行运行。

