# Tick Save 策略

## 概述

**Tick Save 策略** 是 MetaTrader 4 专家顾问 `TickSave` 的 StockSharp 移植版本。策略会持续监听参数 `Symbol List` 中所有品种的买价（Best Bid），并在价格发生变化时把时间戳和报价写入 CSV 文件。输出结构与原始 EA 相同：每个品种每个月生成一个日志文件，同时可以附加诊断标记。

## 主要特性

- 为列表中的每个品种订阅 Level 1 行情。
- 在买价变化时写入服务器时间与最新买价。
- 自动创建 `<Output Root>/<Server Folder>/<Symbol>_YYYY.MM.csv` 的目录结构。
- 可选写入与原始 EA 一致的诊断标记（`Connection lost` 与 `Expert was stopped`）。
- 适用于 `SecurityProvider` 能够解析的任意品种。

## 参数说明

| 名称 | 说明 |
| ---- | ---- |
| `Symbol List` | 需要记录的品种标识，使用逗号分隔。|
| `Write Warnings` | 启用后，每次写入都会附加诊断标记，并在策略停止时再次写入。|
| `Output Root` | 存放 CSV 文件的根目录，默认位于程序目录下的 `Ticks` 子目录。|
| `Server Folder` | 可选的服务器或环境子目录，留空时会根据投资组合或标的的交易板块自动推断。|

## 输出格式

每条 CSV 记录包含两列：

1. 服务器时间，格式 `yyyy-MM-dd HH:mm:ss`。
2. 按不变文化格式写出的买价。

当 `Write Warnings` 启用时，还会单独写出诊断标记，用于指示连接中断或策略结束，效果与原始 MT4 程序一致。

## 使用步骤

1. 将策略连接到能够提供目标品种的交易连接器。
2. 在 `Symbol List` 中填入连接器可以识别的品种代码。
3. 根据需要调整输出目录或启用诊断标记。
4. 启动策略，即可在生成的 CSV 文件中持续接收买价记录。
